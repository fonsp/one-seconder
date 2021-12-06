#!/bin/bash

# mkdir originals &> /dev/null

for FILE1 in "$@"
do
ffmpeg -i "$FILE1" -c:a copy -c:v libx264 -crf 22 -tune grain -y -map_metadata 0 "compressed_$FILE1"
touch -r "$FILE1" "compressed_$FILE1"
# mv "$FILE1" "originals/$FILE1"
done
