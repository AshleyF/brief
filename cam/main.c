#include <stdio.h>
#include <string.h>
#include <unistd.h>
#include "camera.h"
#include "server.h"

void process(int conn, unsigned char* frame, int length) {
    printf("Accept request, conn is %d\n", conn);
    unsigned char buf[1024];
    sprintf(buf, "HTTP/1.1 200 OK\r\n");
    sprintf(buf + strlen(buf), "Content-Type: image/jpeg\r\n");
    // sprintf(buf + strlen(buf), "Content-Length: %i\r\n", length);
    sprintf(buf + strlen(buf), "\r\n");
	int written = write(conn, buf, strlen(buf));
    written = write(conn, frame, length);
}

int main(int argc, char** argv)
{
	printf("V4L2 Camera Test\n");

	if (!camInit("/dev/video0"))
	{
		printf("Failed to initialize camera.\n");
		return 1;
	}

	int fd = serverInit(80);
	unsigned char* frame;
	int length = 0;
    while(1)
	{
        int conn = serverAccept(fd);

		for (int i = 0; i < 8; i++) // driver seems to queue 8 frames
		{
			if (!camFrame(&frame, &length))
			{
				printf("Failed to get camera frame.\n");
				return 1;
			}
		}
		printf("Got frame! (len=%i)\n", length);

		process(conn, frame, length);
		serverClose(conn);
    }

    return 0;
}
