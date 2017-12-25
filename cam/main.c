#include "camera.h"
#include <stdio.h>

int main(int argc, char** argv)
{
	printf("Once upon a time... V4L2 Camera Test\n");

	if (!camInit("/dev/video0"))
	{
		printf("Failed to initialize camera.\n");
		return 1;
	}

	unsigned char* frame;
	int length = 0;
	while (1)
	{
		if (!camFrame(frame, &length))
		{
			printf("Failed to get camera frame.\n");
			return 1;
		}

		printf("Got frame! (len=%i)\n", length);
	}
}
