#!/bin/bash

for FILE1 in "$@"
do
ffmpeg -i "$FILE1" -vf scale=1920:1080 -strict -2 "upscale$FILE1"
touch -r "$FILE1" "upscale$FILE1"
done
