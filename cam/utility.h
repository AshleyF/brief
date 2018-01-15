#ifndef UTILITY_H
#define UTILITY_H

#include <stdlib.h>

#define MAXLINE 1024

void resetLine();
unsigned char* readLine(int fd, unsigned char *buf);
int startsWith(unsigned char *prefix, unsigned char *str);
void base64Encode(unsigned char *src, size_t srcLen, unsigned char *dest, size_t destLen);

#endif