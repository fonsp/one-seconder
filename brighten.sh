#!/bin/bash

for FILE1 in "$@"
do
ffmpeg -i "$FILE1" -vf "curves=all='0/0 .3/.7 .9/1 1/1'" -strict -2 "bright$FILE1"
touch -r "$FILE1" "bright$FILE1"
done
