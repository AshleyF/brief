#include <stdio.h>
#include <fcntl.h>
#include <unistd.h>
#include <string.h>
#include <sys/ioctl.h>
#include <sys/mman.h>
#include <linux/videodev2.h> // apt-get install libv4l-dev

#include "camera.h"

int fd;
unsigned char* buffer[BUF_COUNT];
struct v4l2_buffer buf = {0};

int camInit(const char* device)
{
	buf.index = -1; // signals non-dequeued initially

	// open device
	fd = open(device, O_RDWR);
	if (fd == -1)
	{
		perror("Open video device failed.");
		return 0;
	}
	
	// query capabilities
	struct v4l2_capability caps = {0};
	if (-1 == ioctl(fd, VIDIOC_QUERYCAP, &caps))
	{
		perror("Query capabilities failed.");
		return 0;
	}

	// set format
	struct v4l2_format fmt = {0};
	fmt.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
	fmt.fmt.pix.width = 1280;
	fmt.fmt.pix.height = 720;
	fmt.fmt.pix.pixelformat = V4L2_PIX_FMT_MJPEG;
	fmt.fmt.pix.field = V4L2_FIELD_NONE;
	if (-1 == ioctl(fd, VIDIOC_S_FMT, &fmt))
	{
		perror("Set pixel format failed.");
		return 0;
	}

	// request buffers
	struct v4l2_requestbuffers req = {0};
	req.count = BUF_COUNT;
	req.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
	req.memory = V4L2_MEMORY_MMAP;
	if (ioctl(fd, VIDIOC_REQBUFS, &req))
	{
		perror("Request buffers failed.");
		return 0;
	}
	if (req.count != BUF_COUNT)
	{
		perror("Request buffers returned incorrect number of buffers");
		return 0;
	}

	// query buffer
	struct v4l2_buffer info[BUF_COUNT];
	for (int i = 0; i < BUF_COUNT; i++)
	{
		info[i].type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
		info[i].memory = V4L2_MEMORY_MMAP;
		info[i].index = i;
		if (-1 == ioctl(fd, VIDIOC_QUERYBUF, &info[i]))
		{
			perror("Query buffer failed");
			return 0;
		}
		buffer[i] = mmap(NULL, info[i].length, PROT_READ | PROT_WRITE, MAP_SHARED, fd, info[i].m.offset);
		if (-1 == ioctl(fd, VIDIOC_QBUF, &info[i]))
		{
			perror("Enqueue buffer failed.");
			return 0;
		}
	}

	// capture
	buf.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
	if (-1 == ioctl(fd, VIDIOC_STREAMON, &buf.type))
	{
		perror("Stream on failed");
		return 0;
	}

	return 1;
}

int camFrame(unsigned char** frame, int* length)
{
	// reenque buffer
	if (buf.index != -1)
	{
		if (-1 == ioctl(fd, VIDIOC_QBUF, &buf))
		{
			perror("Enqueue buffer failed.");
			return 0;
		}
	}

	// dequeue buffer
	if (-1 == ioctl(fd, VIDIOC_DQBUF, &buf))
	{
		perror("Dequeue buffer failed.");
		return 0;
	}

	*frame = buffer[buf.index];
	*length = buf.bytesused;
	return 1;
}
