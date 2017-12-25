#ifndef CAMERA_H
#define CAMERA_H

#include <stdio.h>
#include <fcntl.h>
#include <sys/ioctl.h>
#include <sys/mman.h>
#include <linux/videodev2.h> // apt-get install libv4l-dev

#define BUF_COUNT 3

int camInit(const char* device);
int camFrame(unsigned char** frame, int* length);

#endif
