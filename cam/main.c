#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <openssl/sha.h>
#include "camera.h"
#include "server.h"
#include "utility.h"

void process(int conn) {
    // printf("Accept request, conn is %d\n", conn);

	unsigned char buf[MAXLINE];
	resetLine();
	unsigned char* line = readLine(conn, buf);
	// printf("REQUEST: %s\n", line);
	if (startsWith("GET / ", line))
	{
		sprintf(buf, "HTTP/1.1 200 OK\r\n");
		sprintf(buf + strlen(buf), "Content-Type: text/html\r\n");
		sprintf(buf + strlen(buf), "\r\n");
		sprintf(buf + strlen(buf), "<h1>Camera Test</h1><img onload='frame()' id='camera' /><script>\nvar camera = document.getElementById('camera');\nvar i = 0;\nfunction frame()\n{\ncamera.src = 'frame.jpg?n=' + (i++);\n}\nframe();\n</script>");
		/* sprintf(buf + strlen(buf), "<h1>Socket Test</h1><script>\n"
		                           "alert('Opening socket');\n"
                                   "var ws = new WebSocket('ws://' + location.hostname + '/socket');\n"
                                   "ws.onopen = function() { alert('Open'); }\n"
                                   "ws.onclose = function() { alert('Close'); }\n"
                                   "ws.onerror = function(error) { alert('Error: ' + error); }\n"
                                   "ws.onmessage = function(message) {\n"
                                   "  var reply = prompt('Message: ' + message.data);\n"
                                   "  ws.send(reply);\n"
                                   "}\n"
		                           "</script>");
		// */
		write(conn, buf, strlen(buf));
	}
	else if (startsWith("GET /frame.jpg", line))
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

		sprintf(buf, "HTTP/1.1 200 OK\r\n");
		sprintf(buf + strlen(buf), "Content-Type: image/jpeg\r\n");
		sprintf(buf + strlen(buf), "\r\n");
		write(conn, buf, strlen(buf));
		write(conn, frame, length);
	}
	else if (startsWith("GET /socket ", line))
	{
		printf("SOCKET\n");
		while (line[0] != '\0')
		{
			line = readLine(conn, buf);
			printf("LINE: %s\n", line);
			unsigned char* header = "Sec-WebSocket-Key: ";
			if (startsWith(header, line))
			{
				line += strlen(header);
				printf("KEY: %s", line);
				size_t length = sizeof(line);
				unsigned char hash[1024];
				SHA1(line, length, hash);
				printf("HASH: %s", hash);
			}
		}
	}
	else
	{
		sprintf(buf, "HTTP/1.1 404 Not Found\r\n");
		sprintf(buf + strlen(buf), "\r\n");
		write(conn, buf, strlen(buf));
	}
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