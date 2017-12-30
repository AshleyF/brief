#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <openssl/sha.h>
#include "camera.h"
#include "server.h"
#include "utility.h"

unsigned char buf[MAXLINE];

void process(int conn)
{
	unsigned char* frame;
	int frameLen = 0;
	resetLine();
	unsigned char* line = readLine(conn, buf);
	if (startsWith("GET /old ", line))
	{
		sprintf(buf, "HTTP/1.1 200 OK\r\n");
		sprintf(buf + strlen(buf), "Content-Type: text/html\r\n");
		sprintf(buf + strlen(buf), "\r\n");
		sprintf(buf + strlen(buf), "<h1>Camera Image Request Per Frame Test</h1><img onload='frame()' id='camera' /><script>\nvar camera = document.getElementById('camera');\nvar i = 0;\nfunction frame()\n{\ncamera.src = 'frame.jpg?n=' + (i++);\n}\nframe();\n</script>");
		write(conn, buf, strlen(buf));
	}
	else if (startsWith("GET / ", line))
	{
		sprintf(buf, "HTTP/1.1 200 OK\r\n");
		sprintf(buf + strlen(buf), "Content-Type: text/html\r\n");
		sprintf(buf + strlen(buf), "\r\n");
		sprintf(buf + strlen(buf), "<h1>Camera WebSocket Stream Test</h1><img id='camera' /><script>\n"
                                   "var ws = new WebSocket('ws://' + location.hostname + '/socket');\n"
                                   // "ws.onopen = function() { alert('Open'); }\n"
                                   "ws.onclose = function() { alert('Close'); }\n"
                                   "ws.onerror = function(error) { alert('Error: ' + error); }\n"
                                   "ws.onmessage = function(message) {\n"
								   "  document.getElementById('camera').src = (window.URL || window.webkitURL).createObjectURL(message.data);\n"
                                   "}\n"
		                           "</script>");
		// */
		write(conn, buf, strlen(buf));
	}
	else if (startsWith("GET /frame.jpg", line))
	{
		if (!camFrame(&frame, &frameLen))
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
		write(conn, frame, frameLen);
	}
	else if (startsWith("GET /socket ", line))
	{
		printf("SOCKET\n");
		while (line[0] != '\0')
		{
			line = readLine(conn, buf);
			// printf("LINE: %s\n", line);
			unsigned char* header = "Sec-WebSocket-Key: ";
			if (startsWith(header, line))
			{
				line += strlen(header);
				printf("KEY: %s\n", line);
				unsigned char accept[MAXLINE];
				strcpy(accept, line);
				strcpy(accept + strlen(accept), "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
				printf("ACCEPT: %s\n", accept);
				unsigned char hash[MAXLINE];
				SHA1(accept, strlen(accept), hash);
				base64Encode(hash, 20, accept, MAXLINE);
				printf("HASH: %s\n", accept);
				sprintf(buf, "HTTP/1.1 101 Switching Protocols\r\n");
				sprintf(buf + strlen(buf), "Upgrade: websocket\r\n");
				sprintf(buf + strlen(buf), "Connection: Upgrade\r\n");
				sprintf(buf + strlen(buf), "Sec-WebSocket-Accept: %s\r\n", accept);
				sprintf(buf + strlen(buf), "\r\n");
				write(conn, buf, strlen(buf));

				while (1)
				{
					printf("FRAME (len=%i)\n", frameLen);
					if (!camFrame(&frame, &frameLen)) break;
					buf[0] = 2 | 0x80; // binary, final (TODO: assumed)
					buf[1] = 127; // indicate 64-bit (TODO: assumed)
					buf[2] = (long)frameLen >> 56;
					buf[3] = (long)frameLen >> 48;
					buf[4] = (long)frameLen >> 40;
					buf[5] = (long)frameLen >> 32;
					buf[6] = (long)frameLen >> 24;
					buf[7] = (long)frameLen >> 16;
					buf[8] = (long)frameLen >> 8;
					buf[9] = (long)frameLen;
					printf("B0=%i B1=%i B2=%i B3=%i B4=%i B5=%i B6=%i B7=%i\n", buf[2], buf[3], buf[4], buf[5], buf[6], buf[7], buf[8], buf[9]);
					write(conn, buf, 10);
					// TODO: support masking? (currently, mask bit always 0 - otherwise, send 4 bytes here)
					write(conn, frame, frameLen);
				}

				// TODO: graceful error/close
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