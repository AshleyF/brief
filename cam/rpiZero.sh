#!/usr/bin/env bash

sudo apt-get install libssl-dev &&
sudo modprobe bcm2835-v4l2      &&
sudo make run
