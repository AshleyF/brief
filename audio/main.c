#include <stdio.h>
#include <stdlib.h>
#include <alsa/asoundlib.h>

#define nBlocks 1000
#define blockSize 128

short bufs[nBlocks][blockSize * 2];
const char* device = "plughw:0,0";
int rate = 44100;
int *pRate = &rate;

void capture()
{
	int i;
	int err;
	short buf[blockSize * 2];
	snd_pcm_t *capture_handle;
	snd_pcm_hw_params_t *hw_params;

	if ((err = snd_pcm_open (&capture_handle, device, SND_PCM_STREAM_CAPTURE, 0)) < 0) {
		fprintf (stderr, "cannot open audio device %s (%s)\n", 
		device,
		snd_strerror (err));
		exit (1);
	}

	if ((err = snd_pcm_hw_params_malloc (&hw_params)) < 0) {
		fprintf (stderr, "cannot allocate hardware parameter structure (%s)\n",
		snd_strerror (err));
		exit (1);
	}

	if ((err = snd_pcm_hw_params_any (capture_handle, hw_params)) < 0) {
		fprintf (stderr, "cannot initialize hardware parameter structure (%s)\n",
		snd_strerror (err));
		exit (1);
	}

	if ((err = snd_pcm_hw_params_set_access (capture_handle, hw_params, SND_PCM_ACCESS_RW_INTERLEAVED)) < 0) {
		fprintf (stderr, "cannot set access type (%s)\n",
		snd_strerror (err));
		exit (1);
	}

	if ((err = snd_pcm_hw_params_set_format (capture_handle, hw_params, SND_PCM_FORMAT_S16_LE)) < 0) {
		fprintf (stderr, "cannot set sample format (%s)\n",
		snd_strerror (err));
		exit (1);
	}

	if ((err = snd_pcm_hw_params_set_rate_near (capture_handle, hw_params, pRate, 0)) < 0) {
		fprintf (stderr, "cannot set sample rate (%s)\n",
		snd_strerror (err));
		exit (1);
	}

	if ((err = snd_pcm_hw_params_set_channels (capture_handle, hw_params, 2)) < 0) {
		fprintf (stderr, "cannot set channel count (%s)\n",
		snd_strerror (err));
		exit (1);
	}

	if ((err = snd_pcm_hw_params (capture_handle, hw_params)) < 0) {
		fprintf (stderr, "cannot set parameters (%s)\n",
		snd_strerror (err));
		exit (1);
	}

	snd_pcm_hw_params_free (hw_params);

	if ((err = snd_pcm_prepare (capture_handle)) < 0) {
		fprintf (stderr, "cannot prepare audio interface for use (%s)\n",
		snd_strerror (err));
		exit (1);
	}

	printf("Bit-width: %i\n", snd_pcm_format_width(SND_PCM_FORMAT_S16_LE));

	printf("Capturing...\n");
	for (i = 0; i < nBlocks; ++i) {
		if ((err = snd_pcm_readi(capture_handle, buf, blockSize)) != blockSize) {
			fprintf(stderr, "read from audio interface failed (%s)\n",
			snd_strerror(err));
			exit(1);
		}
		for (int j = 0; j < blockSize * 2; j++)
		{
			bufs[i][j] = buf[j];
		}
		// printf("Buf %i: %i %i %i %i %i %i ...\n", i, buf[0], buf[1], buf[2], buf[3], buf[4], buf[5]);
	}

	snd_pcm_close (capture_handle);
}

void playback()
{
	printf("Playback...\n");

	int i;
	int err;
	snd_pcm_t *playback_handle;
	snd_pcm_hw_params_t *hw_params;

	if ((err = snd_pcm_open (&playback_handle, device, SND_PCM_STREAM_PLAYBACK, 0)) < 0) {
		fprintf (stderr, "cannot open audio device %s (%s)\n", 
		device,
		snd_strerror (err));
		exit (1);
	}

	if ((err = snd_pcm_hw_params_malloc (&hw_params)) < 0) {
		fprintf (stderr, "cannot allocate hardware parameter structure (%s)\n",
		snd_strerror (err));
		exit (1);
	}

	if ((err = snd_pcm_hw_params_any (playback_handle, hw_params)) < 0) {
		fprintf (stderr, "cannot initialize hardware parameter structure (%s)\n",
		snd_strerror (err));
		exit (1);
	}

	if ((err = snd_pcm_hw_params_set_access (playback_handle, hw_params, SND_PCM_ACCESS_RW_INTERLEAVED)) < 0) {
		fprintf (stderr, "cannot set access type (%s)\n",
		snd_strerror (err));
		exit (1);
	}

	if ((err = snd_pcm_hw_params_set_format (playback_handle, hw_params, SND_PCM_FORMAT_S16_LE)) < 0) {
		fprintf (stderr, "cannot set sample format (%s)\n",
		snd_strerror (err));
		exit (1);
	}

	if ((err = snd_pcm_hw_params_set_rate_near (playback_handle, hw_params, pRate, 0)) < 0) {
		fprintf (stderr, "cannot set sample rate (%s)\n",
		snd_strerror (err));
		exit (1);
	}

	if ((err = snd_pcm_hw_params_set_channels (playback_handle, hw_params, 2)) < 0) {
		fprintf (stderr, "cannot set channel count (%s)\n",
		snd_strerror (err));
		exit (1);
	}

	if ((err = snd_pcm_hw_params (playback_handle, hw_params)) < 0) {
		fprintf (stderr, "cannot set parameters (%s)\n",
		snd_strerror (err));
		exit (1);
	}

	snd_pcm_hw_params_free (hw_params);

	if ((err = snd_pcm_prepare (playback_handle)) < 0) {
		fprintf (stderr, "cannot prepare audio interface for use (%s)\n",
		snd_strerror (err));
		exit (1);
	}

	for (i = 0; i < nBlocks; ++i) {
		if ((err = snd_pcm_writei (playback_handle, bufs[i], blockSize)) != blockSize) {
			fprintf (stderr, "write to audio interface failed (%s)\n",
			snd_strerror (err));
			exit (1);
		}
	}

	snd_pcm_close (playback_handle);
}

int main (int argc, char *argv[])
{
	capture();
	playback();
	printf("Done.\n");
}
