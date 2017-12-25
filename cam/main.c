#include <arpa/inet.h>
#include <signal.h>
#include <errno.h>
#include <netinet/tcp.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>

#include "camera.h"

#define LISTENQ 1024 // second argument to listen()
#define MAXLINE 1024 // max length of a line

typedef struct sockaddr SA; /* Simplifies calls to bind(), connect(), and accept() */

int open_listenfd(int port){
    int listenfd, optval=1;
    struct sockaddr_in serveraddr;
    if ((listenfd = socket(AF_INET, SOCK_STREAM, 0)) < 0) /* Create a socket descriptor */
        return -1;
    if (setsockopt(listenfd, SOL_SOCKET, SO_REUSEADDR, (const void *)&optval , sizeof(int)) < 0) /* Eliminates "Address already in use" error from bind. */
        return -1;
    // 6 is TCP's protocol number - enable this, much faster : 4000 req/s -> 17000 req/s
    if (setsockopt(listenfd, 6, TCP_CORK, (const void *)&optval , sizeof(int)) < 0)
        return -1;
    /* Listenfd will be an endpoint for all requests to port on any IP address for this host */
    memset(&serveraddr, 0, sizeof(serveraddr));
    serveraddr.sin_family = AF_INET;
    serveraddr.sin_addr.s_addr = htonl(INADDR_ANY);
    serveraddr.sin_port = htons((unsigned short)port);
    if (bind(listenfd, (SA *)&serveraddr, sizeof(serveraddr)) < 0)
        return -1;
    if (listen(listenfd, LISTENQ) < 0) /* Make it a listening socket ready to accept connection requests */
        return -1;
    return listenfd;
}

ssize_t writen(int fd, void *usrbuf, size_t n){
    size_t nleft = n;
    ssize_t nwritten;
    unsigned char *bufp = usrbuf;
    while (nleft > 0){
        if ((nwritten = write(fd, bufp, nleft)) <= 0){
            if (errno == EINTR)  /* interrupted by sig handler return */
                nwritten = 0;    /* and call write() again */
            else
                return -1;       /* errorno set by write() */
        }
        nleft -= nwritten;
        bufp += nwritten;
    }
    return n;
}

void process(int fd, struct sockaddr_in *clientaddr, unsigned char* frame, int length) {
    printf("accept request, fd is %d, pid is %d\n", fd, getpid());
	printf("LENGTH: %i\n", length);
    unsigned char buf[MAXLINE];
    sprintf(buf, "HTTP/1.1 200 OK\r\n");
    sprintf(buf + strlen(buf), "Content-Type: image/jpeg\r\n");
    sprintf(buf + strlen(buf), "Refresh: 0\r\n");
    // sprintf(buf + strlen(buf), "Content-Type: text/plain\r\n");
	// length = 5;
    // sprintf(buf + strlen(buf), "Content-Length: %i\r\n", length);
    sprintf(buf + strlen(buf), "\r\n");
	int written = write(fd, buf, strlen(buf));
	printf("WRITTEN0: %i\n", written);
    written = write(fd, frame, length);
	printf("WRITTEN1: %i\n", written);
    // write(fd, "Hello", 5);
    // writen(fd, buf, strlen(buf));
    // writen(fd, frame, length);
    // writen(fd, "Hello", 5);
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
    int default_port = 80, listenfd, connfd;
    socklen_t clientlen = sizeof clientaddr;
    listenfd = open_listenfd(default_port);
    if (listenfd <= 0)
	{
        perror("ERROR");
        exit(listenfd);
    }
    printf("listen on port %d, fd is %d\n", default_port, listenfd);
    signal(SIGPIPE, SIG_IGN); // Ignore SIGPIPE signal, so if browser cancels the request, it won't kill the whole process.

	unsigned char* frame;
	int length = 0;
    while(1)
	{
        connfd = accept(listenfd, (SA *)&clientaddr, &clientlen);

		if (!camFrame(&frame, &length))
		{
			printf("Failed to get camera frame.\n");
			return 1;
		}
		printf("Got frame! (len=%i)\n", length);

        process(connfd, &clientaddr, frame, length);
        shutdown(connfd, SHUT_WR);
        close(connfd);
    }

    return 0;
}
