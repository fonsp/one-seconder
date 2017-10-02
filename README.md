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
