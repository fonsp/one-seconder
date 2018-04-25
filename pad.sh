#!/bin/bash

for FILE1 in "$@"
do
ffmpeg -i $FILE1 -filter_complex 'scale=607:1080, pad=1920:1080:656:0:black' -strict -2 pad$FILE1
touch -r $FILE1 pad$FILE1
done
