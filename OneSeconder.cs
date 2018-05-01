/*
one-seconder
Copyright 2018, Fons van der Plas
http://github.com/fons-/one-seconder

AFTER SELECTING SECONDS:

Combine videos using:
$ ffmpeg -f concat -safe 0 -i mkvlist.txt -c:v libx264 -strict -2 out.mp4

Where mkvlist.txt looks like:

file './2017-08-15.mkv'
file './2017-08-31.mkv'
file './2017-09-01.mkv'
etc

BEFORE SELECTING SECONDS:
Make sure that every video is 1080p, horizontal, and 30fps.
Use the following commands, or the *.sh scripts in this repository (which also preserve the timestamp).

Rotate video with:
$ ffmpeg -i in.mov -vf 'transpose=2' -strict -2 out.mov

transpose
1 = 90Clockwise
2 = 90CounterClockwise

Flip a video 180 degrees with:
$ ffmpeg -i in.mov -vf 'hflip,vflip' -strict -2 out.mov

Pad a vertical video with:
$ ffmpeg -i in.mov -filter_complex 'scale=607:1080, pad=1920:1080:656:0:black' -strict -2 out.mp4

Upscale a (horizontal) video to 1080p:
$ ffmpeg -i in.mp4 -vf scale=1920:1080 -strict -2 out.mp4

Convert a 120fps iPhone slo-mo video to a slow 30fps video (remember to upscale the output to 1080p):
$ ffmpeg -i in.mov -vf "setpts=4.0*PTS" -af "atempo=0.5,atempo=0.5" -r 30 -strict -2 out.mp4

MISCELLANEOUS:
On Windows 10 with iOS: import videos using the Photos app to preserve timestamps.
On Windows: Use BulkFileChanger to change timestamps.

Copy 'modified' date (used by the program) between files:
$ touch -r from.mp4 to.mp4

*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

public class Program
{
	static private void GetVideoInfo(string fileName, out int width, out int height, out double length)
	{
		width = height = 0;
		length = 0.0;
		ProcessStartInfo psi = new ProcessStartInfo("ffprobe", "-v error -select_streams v:0 -show_entries stream=width,height,duration -of default=noprint_wrappers=1:nokey=1 \"" + fileName + "\"");
		psi.UseShellExecute = false;
		psi.CreateNoWindow = true;
		psi.RedirectStandardOutput = true;

		Process proc = new Process();
		proc.StartInfo = psi;

		proc.Start();
		proc.WaitForExit();

		string output = proc.StandardOutput.ReadToEnd();

		string[] outputLines = output.Split(new char[] { '\r', '\n', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (outputLines.Length != 3)
		{
			Console.WriteLine("Trouble reading file " + fileName);
		}
		else
		{
			int.TryParse(outputLines[0], out width);
			int.TryParse(outputLines[1], out height);
			double.TryParse(outputLines[2], out length);
		}
	}

	static private void CreateTestVideo(string filename, double offset, double length)
	{
		ProcessStartInfo psi = new ProcessStartInfo("ffmpeg", "-y -v error -i \"" + filename + "\" -ss " + offset + " -t " + length + " -codec copy testScreen.mp4");
		psi.UseShellExecute = false;
		psi.CreateNoWindow = true;
		psi.RedirectStandardOutput = false;

		Process proc = new Process();
		proc.StartInfo = psi;

		proc.Start();
		proc.WaitForExit();
	}

	static private void CreateTestVideo(string filename)
	{
		ProcessStartInfo psi = new ProcessStartInfo("ffmpeg", "-y -v error -i \"" + filename + "\" -codec copy testScreen.mp4");
		psi.UseShellExecute = false;
		psi.CreateNoWindow = true;
		psi.RedirectStandardOutput = false;

		Process proc = new Process();
		proc.StartInfo = psi;

		proc.Start();
		proc.WaitForExit();
	}

	static private void CreateVideo(string filename, double offset, double length, DateTime dt, bool writeTxt)
	{
		if(writeTxt){
			File.WriteAllLines(CreateFilename(dt) + ".txt", new string[] {filename, offset.ToString(), length.ToString()});
		}
		ProcessStartInfo psi = new ProcessStartInfo("ffmpeg", "-y -v error -i \"" + filename + "\" -ss " + offset + " -t " + length + " -vf drawtext=\"fontfile =/usr/share/fonts/truetype/lato/Lato-Bold.ttf: text = '" + CreateDateString(dt) + "': fontcolor = white: fontsize = 48: box = 1: boxcolor = black@0.5: boxborderw = 10: x = w / 32: y = h - w / 32 - text_h\" -codec:a copy -codec:v libx264 -preset slow -strict -2 " + CreateFilename(dt) + ".mkv");
		psi.UseShellExecute = false;
		psi.CreateNoWindow = true;
		psi.RedirectStandardOutput = true;

		Process proc = new Process();
		proc.StartInfo = psi;

		proc.Start();
		proc.WaitForExit();
	}

	static private string CreateFilename(DateTime dt)
	{
		return dt.Year.ToString("D4") + "-" + dt.Month.ToString("D2") + "-" + dt.Day.ToString("D2");
	}

	static string[] days = {"Mo", "Tu", "We", "Th", "Fr", "Sa", "Su"};
	static string[] months = {"Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};

	static private string CreateDateString(DateTime dt)
	{
		return dt.Day + " " + months[dt.Month-1] + ", " + dt.Year;
	}

	static public void Main()
	{
		string folderName = null;
		while (string.IsNullOrWhiteSpace(folderName) || !Directory.Exists(folderName))
		{
			Console.WriteLine("Folder name:");
			folderName = Console.ReadLine();
		}

		List<string> fileNames = Directory.GetFiles(folderName).ToList();
		int count = fileNames.Count;

		Console.WriteLine("Reading video lengths...");

		List<double> lengths = new List<double>();
		List<int> widths = new List<int>();
		List<int> heights = new List<int>();

		for (int i = 0; i < count; i++)
		{
			Console.CursorLeft = 0;
			Console.Write("    ");
			Console.CursorLeft = 0;
			Console.Write((100 * i / count) + "%");
			int w, h;
			double l;
			GetVideoInfo(fileNames[i], out w, out h, out l);

			lengths.Add(l);
			widths.Add(w);
			heights.Add(h);
		}
		Console.CursorLeft = 0;
		Console.WriteLine("100%");

		double lengthPerDay = -1.0;
		while(lengthPerDay<=0.0){
			Console.WriteLine("How many seconds per day? (Choose 1.0 for the classic length.)");
			double.TryParse(Console.ReadLine(), out lengthPerDay);
		}

		//var toRemove = Enumerable.Range(0, count).Where(i => lengths[i] < 1.0 || widths[i] != 1920 || heights[i] != 1080).ToList();
		var toRemove = Enumerable.Range(0, count).Where(i => lengths[i] < lengthPerDay).ToList();
		Console.WriteLine("Ignoring {0} short (<"+lengthPerDay+" sec) clips.", toRemove.Count);

		for(int i = 0; i < toRemove.Count; i++)
		{
			fileNames.RemoveAt(toRemove[i] - i);
			lengths.RemoveAt(toRemove[i] - i);
			count--;
		}

		List<DateTime> dates = fileNames.Select(f => new FileInfo(f).LastWriteTime).ToList();

		var firstDay = dates.Min().Date;
		var lastDay = dates.Max().Date;

		for(DateTime date = firstDay; date <= lastDay; date = date.AddDays(1))
		{

			List<int> indices = Enumerable.Range(0, count).Where(i => dates[i].Date == date).ToList();
			Console.WriteLine("{0}: {1} videos found.", date.ToShortDateString(), indices.Count);
			if(File.Exists(CreateFilename(date) + ".txt")){
				Console.WriteLine("Already selected, continue? [y/n]");
				if(Console.ReadKey().KeyChar.ToString().ToLower() == "y"){
					continue;
				}
			}
			var num = indices.Count;
			if (indices.Count > 0)
			{
				bool done = false;
				while (!done)
				{
					int index = 0;
					if(num > 1)
					{
						index = -1;
						while(index <= 0 || index > num)
						{
							Console.WriteLine("Choose video: [1 - {0}]", num);
							int.TryParse(Console.ReadLine(), out index);
						}
						index--;
					}
					int vidNum = indices[index];
					double length = lengths[vidNum];

					bool skip = false;
					double offset = -1.0;
					while ((!skip) && (offset < 0.0 || offset >= length))
					{
						Console.WriteLine("Choose offset: [0.0 - {0}] or [full] or [skip]", length - lengthPerDay);
						string input = Console.ReadLine();
						if (input == "full")
						{
							CreateTestVideo(fileNames[vidNum]);
						}
						else if (input == "skip")
						{
							skip = true;
							done = true;
							break;
						}
						else
						{
							double.TryParse(input, out offset);
						}
					}

					if (!skip)
					{
						CreateTestVideo(fileNames[vidNum], offset, lengthPerDay);

						Console.WriteLine("Satisfied? [y/n] or [r] to render without continuing");
						string consoleInput = Console.ReadKey().KeyChar.ToString();
						Console.WriteLine();
						if(consoleInput == "r"){
							CreateVideo(fileNames[vidNum], offset, lengthPerDay, dates[vidNum], false);
						}
						done = consoleInput.ToLower() == "y";
						if (done)
						{
							CreateVideo(fileNames[vidNum], offset, lengthPerDay, dates[vidNum], true);
						}
					}
				}
			}
		}
	}
}
