#ifndef CAMERA_H
#define CAMERA_H

#define BUF_COUNT 3

int camInit(const char *device);
int camFrame(unsigned char **frame, int *length);

#endif
