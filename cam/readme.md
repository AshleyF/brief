# Wireless Camera

Primarily I'm using this on a Raspberry Pi Zero as a DropCam replacement.
It should actualy work with any V4L2 camera under Linux.

On RPi, running Ubuntu Mate, install the V4L driver:

    `apt-get install libv4l-dev` (necessary?)
    `modprobe bcm2835-v4l2`

Should then see `/dev/video0`.
