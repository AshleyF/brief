#include <stdio.h>
#include <fcntl.h>
#include <sys/ioctl.h>
#include <sys/mman.h>
#include <linux/videodev2.h> // apt-get install libv4l-dev

int main(int argc, char** argv)
{
	printf("Once upon a time... V4L2 Camera Test\n");

	// open device
	int fd;
	fd = open("/dev/video0", O_RDWR);
	if (fd == -1)
	{
		perror("Open video device failed.");
		return 1;
	}
	
	// query capabilities
	struct v4l2_capability caps = {0};
	if (-1 == ioctl(fd, VIDIOC_QUERYCAP, &caps))
	{
		perror("Query capabilities failed.");
		return 1;
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
		return 1;
	}

	// request buffers
	const int BUF_COUNT = 3;
	struct v4l2_requestbuffers req = {0};
	req.count = BUF_COUNT;
	req.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
	req.memory = V4L2_MEMORY_MMAP;
	if (ioctl(fd, VIDIOC_REQBUFS, &req))
	{
		perror("Request buffers failed.");
		return 1;
	}
	if (req.count != BUF_COUNT)
	{
		perror("Request buffers returned incorrect number of buffers");
		return 1;
	}

	// query buffer
	struct v4l2_buffer info[BUF_COUNT];
	unsigned char* buffer[BUF_COUNT];
	for (int i = 0; i < BUF_COUNT; i++)
	{
		info[i].type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
		info[i].memory = V4L2_MEMORY_MMAP;
		info[i].index = i;
		if (-1 == ioctl(fd, VIDIOC_QUERYBUF, &info[i]))
		{
			perror("Query buffer failed");
			return 1;
		}
		buffer[i] = mmap(NULL, info[i].length, PROT_READ | PROT_WRITE, MAP_SHARED, fd, info[i].m.offset);
		if (-1 == ioctl(fd, VIDIOC_QBUF, &info[i]))
		{
			perror("Enqueue buffer failed.");
			return 1;
		}
	}

	// capture
	struct v4l2_buffer buf = {0};
	buf.type = V4L2_BUF_TYPE_VIDEO_CAPTURE;
	if (-1 == ioctl(fd, VIDIOC_STREAMON, &buf.type))
	{
		perror("Stream on failed");
		return 1;
	}
	while (1)
	{
		if (-1 == ioctl(fd, VIDIOC_DQBUF, &buf))
		{
			perror("Dequeue buffer failed.");
			return 1;
		}

		unsigned char* bytes = buffer[buf.index];
		unsigned int i = 0;
		while (1)
		{
			// decode enough to find length (https://en.wikipedia.org/wiki/JPEG)
			if (bytes[i] != 0xFF)
			{
				perror("JPEG decode failed (expected 0xFF)\n");
				return 1;
			}
			int length = 0;
			switch (bytes[i + 1])
			{
				case 0xD8: // start of image (SOI)
					i += 2;
					break;
				case 0xDA: // start of scan (SOS)
					i += 2 + bytes[i + 2] * 256 + bytes[i + 3];
					while (1)
					{
						if (bytes[i] == 0xFF)
						{
							switch (bytes[i + 1])
							{
								case 0xD0:
								case 0xD1:
								case 0xD2:
								case 0xD3:
								case 0xD4:
								case 0xD5:
								case 0xD6:
								case 0xD7: // restart
								case 0x00: // escaped 0xFF
								case 0xFF: // padding
									i++;
									continue;
								default:
									break;
							}
							break;
						}
						i++;
					}
					break;
				case 0xDC: // number of lines
				case 0xDD: // restart interval
					i += 6;
					break;
				case 0xDF: // expand reference image
					i += 5;
					break;
				case 0xD9: // end of image (EOI)
					length = i + 2;
					break;
				case 0x01: // temporary in arthmetic coding
					i += 2;
					break;
				default: // variable size
					i += 2 + bytes[i + 2] * 256 + bytes[i + 3];
					break;
			}
			if (length)
			{
				printf("Got frame! %i (%i)\n", length, buf.index);
				break;
			}
		}
		if (-1 == ioctl(fd, VIDIOC_QBUF, &buf))
		{
			perror("Enqueue buffer failed.");
			return 1;
		}
	}
}
