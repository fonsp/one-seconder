#!/bin/bash

for FILE1 in "$@"
do
ffmpeg -i $FILE1 -vf "setpts=4.0*PTS" -af "atempo=0.5,atempo=0.5" -r 30 -strict -2 slow$FILE1
touch -r $FILE1 slow$FILE1
done
