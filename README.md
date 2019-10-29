# one-seconder
A simple mono application to create a one-second-a-day compilation. Still in development.

Features:
- Scans a folder and selects only long (at least 1 sec) clips for every day.
- Adds a date watermark.

Running on linux (tested on Ubuntu 16.04.1):
- Install ffmpeg (2.8.11) and mono-dev
- Compile and run with `mcs OneSeconder.cs && mono OneSeconder.exe`
- Have a VLC window open which plays the testScreen.mp4 file (in the execution folder) on repeat. To update the test screen, press stop and play.

Read `OneSeconder.cs` for instructions on how to prepare the videos before selection, and on how to concatinate them afterwards.


## Video editing commands

`bright` to increase the image brightness, `unbright` to undo

`slow2` to slow the video down 2x and setting the framerate to 30, `unslow2` to undo

`slow4` to slow the video down 4x and setting the framerate to 30, `unslow4` to undo

`c` to rotate clockwise

`cc` to rotate clockwise

`flip` to rotate 180 degrees

`default` to reset rotation

_Upscaling to 1080p and padding vertical videos is done automatically after choosing your second._
