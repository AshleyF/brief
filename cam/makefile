cam: camera.h camera.c server.h server.c main.c
	cc -o cam camera.h camera.c server.h server.c utility.h utility.c main.c -lcrypto 

clean: cam
	rm -f cam

run: cam
	./cam
