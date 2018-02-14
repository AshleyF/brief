# Audio Capture/Playback

Using [ALSA (Advanced Linux Sound Architecture)](http://alsa-project.org).

    apt install libasound2-dev

To determine available hardware:

	arecord --list-devices	

The device name "plughw:0,0" means card 0, device 0
