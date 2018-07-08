#!/bin/bash

for FILE1 in "$@"
do
ffmpeg -i "$FILE1" -vf 'transpose=1' -strict -2 "rot$FILE1"
touch -r "$FILE1" "rot$FILE1"
done
