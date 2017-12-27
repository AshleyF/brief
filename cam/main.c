#include <arpa/inet.h>
#include <signal.h>
#include <errno.h>
#include <netinet/tcp.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>

#include "camera.h"

int open_listenfd(int port){
    int fd, optval=1;
    struct sockaddr_in serveraddr = {0};
    serveraddr.sin_family = AF_INET;
    serveraddr.sin_addr.s_addr = htonl(INADDR_ANY);
    serveraddr.sin_port = htons((unsigned short)port);
    if ((fd = socket(AF_INET, SOCK_STREAM, 0)) < 0) return -1; // Create a socket descriptor
    if (setsockopt(fd, SOL_SOCKET, SO_REUSEADDR, (const void*)&optval , sizeof(int)) < 0) return -1; // Eliminates "Address already in use" error from bind.
    if (setsockopt(fd, 6, TCP_CORK, (const void*)&optval , sizeof(int)) < 0) return -1; // 6 is TCP's protocol number - enable this, much faster : 4000 req/s -> 17000 req/s
    if (bind(fd, (struct sockaddr*)&serveraddr, sizeof(serveraddr)) < 0) return -1; // fd will be an endpoint for all requests to port on any IP address for this host
    if (listen(fd, 1024) < 0) return -1; // accepting connection requests
    return fd;
}

ssize_t writen(int fd, void *usrbuf, size_t n){
    size_t nleft = n;
    ssize_t nwritten;
    unsigned char *bufp = usrbuf;
    while (nleft > 0) {
        if ((nwritten = write(fd, bufp, nleft)) <= 0) {
            if (errno == EINTR) nwritten = 0; // interrupted by sig handler return and call write() again
            else return -1; // errorno set by write()
        }
        nleft -= nwritten;
        bufp += nwritten;
    }
    return n;
}

void process(int fd, struct sockaddr_in *clientaddr, unsigned char* frame, int length) {
    printf("Accept request, fd is %d, pid is %d\n", fd, getpid());
    unsigned char buf[1024];
    sprintf(buf, "HTTP/1.1 200 OK\r\n");
    sprintf(buf + strlen(buf), "Content-Type: image/jpeg\r\n");
    // sprintf(buf + strlen(buf), "Refresh: 2\r\n");
    // sprintf(buf + strlen(buf), "Content-Type: text/plain\r\n");
    // sprintf(buf + strlen(buf), "Content-Length: %i\r\n", length);
    sprintf(buf + strlen(buf), "\r\n");
	int written = write(fd, buf, strlen(buf));
    written = write(fd, frame, length);
}

int main(int argc, char** argv)
{
	printf("V4L2 Camera Test\n");

	if (!camInit("/dev/video0"))
	{
		printf("Failed to initialize camera.\n");
		return 1;
	}

    struct sockaddr_in clientaddr;
    int port = 80, fd, connfd;
    socklen_t clientlen = sizeof clientaddr;
    fd = open_listenfd(port);
    if (fd <= 0)
	{
        exit(fd);
    }
    printf("Web server (port %i)\n", port);
    signal(SIGPIPE, SIG_IGN); // Ignore SIGPIPE signal, so if browser cancels the request, it won't kill the whole process.

	unsigned char* frame;
	int length = 0;
    while(1)
	{
        connfd = accept(fd, (struct sockaddr*)&clientaddr, &clientlen);

		//for (int i = 0; i < 8; i++) // driver seems to queue 8 frames
		{
			if (!camFrame(&frame, &length))
			{
				printf("Failed to get camera frame.\n");
				return 1;
			}
		}
		printf("Got frame! (len=%i)\n", length);

        process(connfd, &clientaddr, frame, length);
        shutdown(connfd, SHUT_WR);
        close(connfd);
    }

    return 0;
}
