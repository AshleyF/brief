#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include "camera.h"
#include "server.h"

// abc\r\nde -> abc\0 + de
// de (shift, read)
// def\r\n\ghir\n\r\n
// abc\r\ndef\r\n\r\n

const int MAXLINE = 1024;

int len = 0;
unsigned char* next;

unsigned char* readLine(int fd, unsigned char* buf)
{
	// printf("PARSE LINE\n");
	if (len <= 0)
	{
		// fill buffer
		// printf("  READ\n");
		// printf("  BUFFER0: %s\n", buf);
		len = read(fd, buf, MAXLINE);
		if (len == 0)
		{
			perror("Unable to read from socket.");
			exit(-1);
		}
		// printf("  BUFFER1 (LEN=%i): %s\n", len, buf);
		next = buf;
	}

	// find terminator
	// printf("  FIND TERMINATOR\n");
	for (int i = 0; i < len; i++)
	{
		if (next[i] == '\r')
		{
			// found - replace with '\0' and update `next`/`len`
			// printf("    FOUND\n");
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

	// printf("    NOT FOUND\n");
	// not found - shift remaining data and fill remaining buffer
	// printf("  SHIFT\n");
	// printf("  OLD BUFFER: %s\n", buf);
	for (int i = 0; i < len; i++)
	{
		//        de -> abc\0 + de
		// de (shift, read)
		buf[i] = next[i];
	}
	next = buf;
	len += read(fd, buf + len, MAXLINE - len);
	// printf("  NEW BUFFER: %s\n", buf);
	// printf("  LEN=%i\n", len);
	return readLine(fd, buf);
}

int startsWith(unsigned char* prefix, unsigned char* str)
{
	size_t len = strlen(prefix);
	if (len > strlen(str))
	{
		printf("TOOLONG (%s=%ld, %s=%ld)\n", prefix, len, str, strlen(str));
	}
	printf("COMP '%s': %i (LEN=%ld)\n", prefix, strncmp(prefix, str, len), len);
	return len > strlen(str) ? 0 : strncmp(prefix, str, len) == 0;
}

void process(int conn) {
    printf("Accept request, conn is %d\n", conn);

	unsigned char buf[MAXLINE];
	len = 0;
	unsigned char* line = readLine(conn, buf);
	printf("HEADER: %s\n", line);
	if (startsWith("GET /frame.jpg", line))
	{
		printf("FRAME\n");

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
		printf("PAGE\n");
		sprintf(buf, "HTTP/1.1 200 OK\r\n");
		sprintf(buf + strlen(buf), "Content-Type: text/html\r\n");
		// sprintf(buf + strlen(buf), "Content-Length: %i\r\n", length);
		sprintf(buf + strlen(buf), "\r\n");
		sprintf(buf + strlen(buf), "<h1>Camera Test</h1><img onload='frame()' id='camera' /><script>\nvar camera = document.getElementById('camera');\nvar i = 0;\nfunction frame()\n{\ncamera.src = 'frame.jpg?n=' + (i++);\n}\nframe();\n</script>");
		write(conn, buf, strlen(buf));
	}
	else
	{
		printf("NOT FOUND\n");
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
		printf("ACCEPTED CONNECTION: %i\n", conn);
		process(conn);
		serverClose(conn);
		printf("CLOSED\n");
    }

    return 0;
}
