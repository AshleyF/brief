#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <openssl/sha.h>
#include "utility.h"

static int len = 0;
static unsigned char *next;

void resetLine()
{
	len = 0;
}

unsigned char* readLine(int fd, unsigned char *buf)
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
			unsigned char *line = next;
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

int startsWith(unsigned char *prefix, unsigned char *str)
{
	size_t len = strlen(prefix);
	return len > strlen(str) ? 0 : strncmp(prefix, str, len) == 0;
}

void base64Encode(unsigned char *src, size_t srcLen, unsigned char *dest, size_t destLen)
{
	const unsigned char table[65] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    const unsigned char *end = src + srcLen;
    while (end - src >= 3)
	{
        *dest++ = table[src[0] >> 2];
        *dest++ = table[((src[0] & 0x03) << 4) | (src[1] >> 4)];
        *dest++ = table[((src[1] & 0x0f) << 2) | (src[2] >> 6)];
        *dest++ = table[src[2] & 0x3f];
        src += 3;
    }

    if (end - src)
	{
        *dest++ = table[src[0] >> 2];
        if (end - src == 1)
		{
            *dest++ = table[(src[0] & 0x03) << 4];
            *dest++ = '=';
        }
        else
		{
            *dest++ = table[((src[0] & 0x03) << 4) | (src[1] >> 4)];
            *dest++ = table[(src[1] & 0x0f) << 2];
        }
        *dest++ = '=';
    }
	*dest++ = '\0';
}