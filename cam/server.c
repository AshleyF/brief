#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <arpa/inet.h>
#include <signal.h>
#include <netinet/tcp.h>

#include "server.h"

int serverInit(int port)
{
    int fd, opt=1;
    struct sockaddr_in serveraddr = {0};
    serveraddr.sin_family = AF_INET;
    serveraddr.sin_addr.s_addr = htonl(INADDR_ANY);
    serveraddr.sin_port = htons((unsigned short)port);
    if ((fd = socket(AF_INET, SOCK_STREAM, 0)) < 0) return -1;
    if (setsockopt(fd, SOL_SOCKET, SO_REUSEADDR, (const void*)&opt , sizeof(int)) < 0) return -1; // no "Address already in use" error from bind.
    if (setsockopt(fd, 6 /* TCP */, TCP_CORK, (const void*)&opt , sizeof(int)) < 0) return -1;
    if (bind(fd, (struct sockaddr*)&serveraddr, sizeof(serveraddr)) < 0) return -1; // fd endpoint for all port requests (any IP address)
    if (listen(fd, 1024) < 0) return -1; // accepting connection requests
    if (fd <= 0)
	{
        exit(fd);
    }
    printf("Web server (port %i)\n", port);
    signal(SIGPIPE, SIG_IGN); // Ignore SIGPIPE signal, so if browser cancels the request, it won't kill the whole process.
	return fd;
}

int serverAccept(int fd)
{
    struct sockaddr_in clientaddr;
    socklen_t clientlen = sizeof clientaddr;
	return accept(fd, (struct sockaddr*)&clientaddr, &clientlen);
}

void serverClose(int conn)
{
	shutdown(conn, SHUT_WR);
	close(conn);
}
