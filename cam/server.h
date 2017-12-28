#ifndef SERVER_H
#define SERVER_H

int serverInit(int port);
int serverAccept(int fd);
void serverClose(int conn);

#endif
