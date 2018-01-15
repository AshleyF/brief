#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <string.h>

#define STACK_SIZE 64 * 1024 * 1024

uint8_t stack[STACK_SIZE];
uint8_t *s = stack;

void init()
{
	memset(s, 0, sizeof(int));
}

void push(uint8_t *value, int length)
{
	printf("Push: %p (len=%i)\n", value, length);
	s += sizeof(int); // over length
	if (s + length + sizeof(int) > stack + STACK_SIZE)
	{
		fprintf(stderr, "Stack overflow\n");
		exit(1);
	}

	memcpy(s, value, length);
	s += length;
	memcpy(s, &length, sizeof(int));
	printf("Stack: %p\n", s);
}

uint8_t* pop()
{
	int length = (int)*s;
	printf("Pop (len=%i)\n", length);
	uint8_t *value = s - length;
	s = value - sizeof(int);
	if (s < stack)
	{
		fprintf(stderr, "Stack underflow\n");
		exit(1);
	}

	return value;
}

int main(int argc, char** argv)
{
	printf("VM Sandbox\n");

	int i = 123;
	long l = 456;
	char *s = "Now is the time for all good men to come to the aid of their country.";
	double d = 3.14159;

	push((uint8_t*)&i, sizeof(i));
	push((uint8_t*)&l, sizeof(l));
	push((uint8_t*)s, strlen(s) + 1); // note: special
	push((uint8_t*)&d, sizeof(d));

	double *dd = (double*)pop();
	printf("Double: %f\n", *dd);
	char *ss = (char*)pop();
	printf("String: %s\n", ss); // note: special
	long *ll = (long*)pop();
	printf("Long: %ld\n", *ll);
	int *ii = (int*)pop();
	printf("Int: %i\n", *ii);
}
