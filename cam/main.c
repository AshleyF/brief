#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include "camera.h"
#include "server.h"

const int MAXLINE = 1024;

int len = 0;
unsigned char* next;

unsigned char* readLine(int fd, unsigned char* buf)
{
	if (len <= 0)
	{
		// fill buffer
		len = read(fd, buf, MAXLINE);
		if (len == 0)
		{
			perror("Unable to read from socket.");
			exit(-1);
		}
		next = buf;
	}

	// find terminator
	for (int i = 0; i < len; i++)
	{
		if (next[i] == '\r')
		{
			// found - replace with '\0' and update `next`/`len`
			next[i] = '\0';
			unsigned char* line = next;
			i += 2;
			next += i;
			len -= i;
			return line;
		}
	}

	if (len >= MAXLINE)
	{
		perror("HTTP header exceeds buffer or is underminated.");
		exit(-1);
	}

	// not found - shift remaining data and fill remaining buffer
	for (int i = 0; i < len; i++)
	{
		buf[i] = next[i];
	}
	next = buf;
	len += read(fd, buf + len, MAXLINE - len);
	return readLine(fd, buf);
}

int startsWith(unsigned char* prefix, unsigned char* str)
{
	size_t len = strlen(prefix);
	return len > strlen(str) ? 0 : strncmp(prefix, str, len) == 0;
}

void process(int conn) {
    printf("Accept request, conn is %d\n", conn);

	unsigned char buf[MAXLINE];
	len = 0;
	unsigned char* line = readLine(conn, buf);
	printf("REQUEST: %s\n", line);
	if (startsWith("GET /frame.jpg", line))
	{
		unsigned char* frame;
		int length = 0;
		if (!camFrame(&frame, &length))
		{
			printf("Failed to get camera frame.\n");
			sprintf(buf, "HTTP/1.1 500 Camera Failure\r\n");
			sprintf(buf + strlen(buf), "\r\n");
			write(conn, buf, strlen(buf));
			return;
		}
		printf("Got frame! (len=%i)\n", length);

		sprintf(buf, "HTTP/1.1 200 OK\r\n");
		sprintf(buf + strlen(buf), "Content-Type: image/jpeg\r\n");
		// sprintf(buf + strlen(buf), "Content-Length: %i\r\n", length);
		sprintf(buf + strlen(buf), "\r\n");
		write(conn, buf, strlen(buf));
		write(conn, frame, length);
	}
	else if (startsWith("GET / ", line))
	{
		sprintf(buf, "HTTP/1.1 200 OK\r\n");
		sprintf(buf + strlen(buf), "Content-Type: text/html\r\n");
		// sprintf(buf + strlen(buf), "Content-Length: %i\r\n", length);
		sprintf(buf + strlen(buf), "\r\n");
		sprintf(buf + strlen(buf), "<h1>Camera Test</h1><img onload='frame()' id='camera' /><script>\nvar camera = document.getElementById('camera');\nvar i = 0;\nfunction frame()\n{\ncamera.src = 'frame.jpg?n=' + (i++);\n}\nframe();\n</script>");
		write(conn, buf, strlen(buf));
	}
	else
	{
		sprintf(buf, "HTTP/1.1 404 Not Found\r\n");
		sprintf(buf + strlen(buf), "\r\n");
		write(conn, buf, strlen(buf));
	}
	/*
	for (int i = 0; i < 20; i++)
	{
		unsigned char* line = readLine(conn, buf);
		printf("LINE %i: %s (LEN=%i, %i)\n", i, line, len, line[0]);
		if (line[0] == '\0') break;
	}
	printf("END\n");
	*/

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
    while(1)
	{
        int conn = serverAccept(fd);
		process(conn);
		serverClose(conn);
    }

    return 0;
}
