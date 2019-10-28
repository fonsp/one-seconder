/*
one-seconder
by Fons van der Plas
http://github.com/fonsp/one-seconder

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

Upscale a (horizontal) video to 1080p:
$ ffmpeg -i in.mp4 -vf scale=1920:1080 -strict -2 out.mp4

Convert a 120fps iPhone slo-mo video to a slow 30fps video (remember to upscale the output to 1080p):
$ ffmpeg -i in.mov -vf "setpts=4.0*PTS" -af "atempo=0.5,atempo=0.5" -r 30 -strict -2 out.mp4

Copy 'modified' date (used by the program) between files:
$ touch -r from.mp4 to.mp4

*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Newtonsoft.Json;

public class Program
{
	static private void GetVideoInfo(string fileName, out int width, out int height, out double length)
	{
		int orientation = 0;
		width = height = 0;
		length = 0.0;
		ProcessStartInfo psi = new ProcessStartInfo("ffprobe", "-loglevel error -select_streams v:0 -show_entries stream_tags=rotate:stream=width,height,duration -of default=nw=1:nk=1 \"" + fileName + "\"");
		psi.UseShellExecute = false;
		psi.CreateNoWindow = true;
		psi.RedirectStandardOutput = true;
		//psi.RedirectStandardError = true;

		Process proc = new Process();
		proc.StartInfo = psi;

		proc.Start();
		proc.WaitForExit();

		string output = proc.StandardOutput.ReadToEnd();

		string[] outputLines = output.Split(new char[] { '\r', '\n', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if(!(outputLines.Length == 3 || outputLines.Length == 4))
		{
			Console.WriteLine("Trouble reading file " + fileName);
		}
		else
		{
			int.TryParse(outputLines[0], out width);
			int.TryParse(outputLines[1], out height);
			double.TryParse(outputLines[2], out length);
			if(outputLines.Length == 4)
			{
				int.TryParse(outputLines[3], out orientation);
			}
			if((orientation / 90) % 2 != 0)
			{
				int temp = width;
				width = height;
				height = temp;
			}
		}
	}

	static Process runningProcess = null;

	static private void CreateTestVideo(string filename, double offset, double length)
	{
		//ProcessStartInfo psi = new ProcessStartInfo("ffmpeg", "-y -v error -i \"" + filename + "\" -ss " + offset + " -t " + length + " -codec copy testScreen.mp4");
		ProcessStartInfo psi = new ProcessStartInfo("ffmpeg", "-y -v error -i \"" + filename + "\" -ss " + offset + " -t " + length + " -codec:a copy -codec:v libx264 -preset ultrafast testScreen.mp4");
		psi.UseShellExecute = false;
		psi.CreateNoWindow = true;
		psi.RedirectStandardOutput = false;

		if(runningProcess != null)
		{
			runningProcess.WaitForExit();
		}
		runningProcess = new Process();
		runningProcess.StartInfo = psi;
		runningProcess.Start();
	}

	static private void CreateTestVideo(string filename)
	{
		ProcessStartInfo psi = new ProcessStartInfo("ffmpeg", "-y -v error -i \"" + filename + "\" -codec copy testScreen.mp4");
		psi.UseShellExecute = false;
		psi.CreateNoWindow = true;
		psi.RedirectStandardOutput = false;

		if(runningProcess != null)
		{
			runningProcess.WaitForExit();
		}
		runningProcess = new Process();
		runningProcess.StartInfo = psi;
		runningProcess.Start();
	}

	static private void CreateCompileScriptVideo(string videoPath, double offset, double length, DateTime dt, bool pad, bool upscale, bool writeTxt)
	{
		if(writeTxt)
		{
			File.WriteAllLines(CreateFilename(dt) + ".txt", new string[] { videoPath, offset.ToString(), length.ToString() });
		}

		List<string> commands = new List<string>();
		
		commands.Add("");

		string newPath = videoPath;
		if(upscale)
		{
			newPath = Path.Combine(Path.GetDirectoryName(newPath), "upscale" + Path.GetFileName(newPath));
			commands.Add("ffmpeg -i \"" + videoPath + "\" -vf scale=1920:1080 -codec:v libx264 -preset slow -strict -2 \"" + newPath + "\"");
		}
		if(pad)
		{
			newPath = Path.Combine(Path.GetDirectoryName(newPath), "pad" + Path.GetFileName(newPath));
			commands.Add("ffmpeg -i \"" + videoPath + "\" -filter_complex 'scale=607:1080, pad=1920:1080:656:0:black' -codec:v libx264 -preset slow -strict -2 \"" + newPath + "\"");
		}
		commands.Add("ffmpeg -y -v error -i \"" + newPath + "\" -ss " + offset + " -t " + length + " -vf drawtext=\"fontfile =/usr/share/fonts/truetype/lato/Lato-Bold.ttf: text = '" + CreateDateString(dt) + "': fontcolor = white: fontsize = 48: box = 1: boxcolor = black@0.5: boxborderw = 10: x = w / 32: y = h - w / 32 - text_h\" -codec:a copy -codec:v libx264 -preset slow -strict -2 " + CreateFilename(dt) + ".mkv");

		File.WriteAllLines(CreateFilename(dt) + ".sh", commands.ToArray());
	}

	static private string CreateFilename(DateTime dt)
	{
		return dt.Year.ToString("D4") + "-" + dt.Month.ToString("D2") + "-" + dt.Day.ToString("D2");
	}

	//static string[] days = { "Mo", "Tu", "We", "Th", "Fr", "Sa", "Su" };
	static string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

	static private string CreateDateString(DateTime dt)
	{
		return dt.Day + " " + months[dt.Month - 1] + ", " + dt.Year;
	}

	public class VideoRecord
	{
		public string jsonPath;
		public string parentDir;
		public string videoFileName;
		public DateTime dateTime;
		public double length;
		public int width, height;

		public VideoRecord(string jsonPath, DateTime dateTime, double length, int width, int height)
		{
			this.jsonPath = jsonPath;
			this.parentDir = Path.GetDirectoryName(jsonPath);
			this.videoFileName = Path.GetFileName(jsonPath.Substring(0, jsonPath.Length - 5));
			this.dateTime = dateTime;
			this.length = length;
			this.width = width;
			this.height = height;
		}

		public override string ToString()
		{
			return dateTime.ToString() + " - (" + length + " sec): " + jsonPath;
		}
	}

	public struct Rotation
	{
		public string command, longName, abbreviation;
		public Rotation(string command, string longName, string abbreviation)
		{
			this.command = command;
			this.longName = longName;
			this.abbreviation = abbreviation;
		}
	}

	static Rotation defaultRotation = new Rotation(null, "default", "default");
	static private Rotation[] rotations = new Rotation[] {
		defaultRotation,
		new Rotation("transpose=1", "clockwise", "c"),
		new Rotation("transpose=2", "counter-clockwise", "cc"),
		new Rotation("vflip,hflip", "flipped", "flip")
	};

	static private string CreateModifiedVideo(VideoRecord video, Rotation rotation, bool brighter, bool pad)
	{
		string newFilename =
			(pad ? "pad" : "") +
			(brighter ? "bright" : "") +
			(rotation.abbreviation != "default" ? rotation.abbreviation : "") +
			video.videoFileName;
		string newPath = Path.Combine(video.parentDir, newFilename);

		if(runningProcess != null)
		{
			runningProcess.WaitForExit();
		}

		if(File.Exists(newPath))
		{
			return newPath;
		}

		ProcessStartInfo psi;

		if(pad)
		{
			string defaultPath = CreateModifiedVideo(video, rotation, brighter, false);
			psi = new ProcessStartInfo("ffmpeg", "-i \"" + defaultPath + "\" -filter_complex 'scale=607:1080, pad=1920:1080:656:0:black' -codec:v libx264 -preset slow -strict -2 \"" + newPath + "\"");
			//Console.WriteLine("-i \"" + defaultPath + "\" -filter_complex 'scale=607:1080, pad=1920:1080:656:0:black' -codec:v libx264 -preset slow -strict -2 \"" + newPath + "\"");
		}
		else if(brighter)
		{
			string defaultPath = CreateModifiedVideo(video, rotation, false, pad);
			psi = new ProcessStartInfo("ffmpeg", "-i \"" + defaultPath + "\" -vf \"curves = all = '0/0 .3/.7 .9/1 1/1'\" -codec:v libx264 -preset slow -strict -2 \"" + newPath + "\"");

		}
		else if(rotation.abbreviation != "default")
		{
			string defaultPath = CreateModifiedVideo(video, defaultRotation, brighter, pad);
			psi = new ProcessStartInfo("ffmpeg", "-i \"" + defaultPath + "\" -vf '" + rotation.command + "' -codec:v libx264 -preset slow -strict -2 \"" + newPath + "\"");
			//Console.WriteLine("-i \"" + defaultPath + "\" -vf '" + rotation.command + "' -codec:v libx264 -preset slow -strict -2 \"" + newPath + "\"");
		}
		else
		{
			return newPath;
		}

		psi.UseShellExecute = false;
		psi.CreateNoWindow = true;
		psi.RedirectStandardOutput = true;
		psi.RedirectStandardError = true;


		runningProcess = new Process();
		runningProcess.StartInfo = psi;
		runningProcess.Start();

		return newPath;
	}


	static public void Main()
	{
		Console.WriteLine();
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine("One seconder - visit https://github.com/fonsp/one-seconder for more info and instructions.");
		Console.ResetColor();
		Console.WriteLine();

		string folderName = null;
		while(string.IsNullOrWhiteSpace(folderName) || !Directory.Exists(folderName))
		{
			Console.WriteLine("Google Takeout folder (should contain 'archive_browser.html'):");
			folderName = Console.ReadLine();

			if(Directory.Exists(folderName))
			{
				if(!Directory.GetFiles(folderName, "archive_browser.html").Any())
				{
					Console.WriteLine("Not a Google Takout folder.");
					folderName = null;
				}
			}
			else
			{
				Console.WriteLine("Directory does not exist.");
			}
		}

		string[] videoExtensions = new string[] { "mov", "MOV", "mp4", "MP4", "mkv", "MKV" };
		var videoJsonExtensions = videoExtensions.Select(s => "." + s + ".json").ToList();

		List<string> jsonFileNames = Directory.EnumerateFiles(folderName, "*.json", SearchOption.AllDirectories)
			.Where(s => videoJsonExtensions.Where(s.EndsWith).Any())
			.ToList();

		//jsonFileNames = jsonFileNames.Take(10).ToList();

		int count = jsonFileNames.Count;

		Console.WriteLine("Reading video lengths...");

		List<double> lengths = new List<double>();
		List<int> widths = new List<int>();
		List<int> heights = new List<int>();

		for(int i = 0; i < count; i++)
		{
			Console.CursorLeft = 0;
			Console.Write("    ");
			Console.CursorLeft = 0;
			Console.Write((100 * i / count) + "%");
			int w, h;
			double l;
			GetVideoInfo(jsonFileNames[i].Substring(0, jsonFileNames[i].Length - 5), out w, out h, out l);

			lengths.Add(l);
			widths.Add(w);
			heights.Add(h);
		}
		Console.CursorLeft = 0;
		Console.WriteLine("100%");
		Console.WriteLine();

		double lengthPerDay = -1.0;
		while(lengthPerDay <= 0.0)
		{
			Console.WriteLine("How many seconds per day? (Choose 1.0 for the classic length.)");
			double.TryParse(Console.ReadLine(), out lengthPerDay);
		}

		//var toRemove = Enumerable.Range(0, count).Where(i => lengths[i] < 1.0 || widths[i] != 1920 || heights[i] != 1080).ToList();
		var toRemove = Enumerable.Range(0, count).Where(i => lengths[i] < lengthPerDay).ToList();
		Console.WriteLine("Ignoring {0} short (<{1} sec) clip{2}.", toRemove.Count, lengthPerDay, toRemove.Count == 1 ? "" : "s");

		for(int i = 0; i < toRemove.Count; i++)
		{
			jsonFileNames.RemoveAt(toRemove[i] - i);
			lengths.RemoveAt(toRemove[i] - i);
			widths.RemoveAt(toRemove[i] - i);
			heights.RemoveAt(toRemove[i] - i);
			count--;
		}

		var jsonDef = new { title = "", photoTakenTime = new { timestamp = -1 } };

		List<VideoRecord> videos = jsonFileNames.Select((f, i) => new VideoRecord(
			f,
			DateTimeOffset.FromUnixTimeSeconds(
				JsonConvert.DeserializeAnonymousType(File.ReadAllText(f), jsonDef).photoTakenTime.timestamp
				).UtcDateTime,
			lengths[i],
			widths[i],
			heights[i]
			)).ToList();

		Console.WriteLine();
		double dayStart = double.MinValue;
		while(dayStart == double.MinValue)
		{
			Console.WriteLine("At what hour does a new day start? (Choose 0.0 for midnight)");
			double.TryParse(Console.ReadLine(), out dayStart);
		}

		Console.WriteLine("(Using local time zone with daylight saving based on recording date.)");

		videos.ForEach(v =>
		{
			v.dateTime = v.dateTime.AddHours(-dayStart).ToLocalTime();
		});

		var firstDay = videos.Min(v => v.dateTime).Date;
		var lastDay = videos.Max(v => v.dateTime).Date;

		//videos.ForEach(v => Console.WriteLine(v));

		for(DateTime date = firstDay; date <= lastDay; date = date.AddDays(1))
		{
			//List<int> indices = Enumerable.Range(0, count).Where(i => dates[i].Date == date).ToList();
			var todaysVideos = videos.Where(v => v.dateTime.Date == date).ToList();
			var num = todaysVideos.Count();

			Console.WriteLine("{0}: {1} videos found.", date.ToString("d MMM yyyy"), num);
			if(File.Exists(CreateFilename(date) + ".txt"))
			{
				Console.WriteLine("Already selected, continue? [y/n]");
				if(Console.ReadKey().KeyChar.ToString().ToLower() == "y")
				{
					continue;
				}
			}

			if(num > 0)
			{
				bool doneWithToday = false;
				while(!doneWithToday)
				{
					Rotation chosenRotation = defaultRotation;
					bool chosenBrighter = false;
					bool chosenPad = false;

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

						chosenRotation = defaultRotation;
						chosenBrighter = false;
						chosenPad = false;
					}

					VideoRecord video = todaysVideos[index];
					double length = video.length;

					string chosenPath = null;

					bool skipToday = false;
					double offset = -1.0;

					string info;

					while((!skipToday) && (offset < 0.0 || offset >= length))
					{
						Console.WriteLine("Choose offset: [0.0 - {0}], [full] for an uncut preview or [skip] to ignore this day", length - lengthPerDay);
						string input = Console.ReadLine();

						bool modifiedCommand = false;


						if(rotations.Where(r => r.abbreviation == input).Any())
						{
							chosenRotation = rotations.Where(r => r.abbreviation == input).First();
							modifiedCommand = true;
						}
						switch(input)
						{
							case "bright":
								modifiedCommand = true;
								chosenBrighter = true;
								break;
							case "dark":
								modifiedCommand = true;
								chosenBrighter = false;
								break;
							case "pad":
								modifiedCommand = true;
								chosenPad = true;
								break;
							case "unpad":
								modifiedCommand = true;
								chosenPad = false;
								break;
						}

						chosenPath = CreateModifiedVideo(video, chosenRotation, chosenBrighter, chosenPad);
						info = "Showing preview with " + chosenRotation.longName + " orientation";

						if(chosenBrighter)
						{
							info += ", brightened";
						}
						if(chosenPad)
						{
							info += ", padded";
						}
						info += "...";

						if(input == "full")
						{
							Console.ForegroundColor = ConsoleColor.Yellow;
							Console.WriteLine(info);
							Console.ResetColor();
							CreateTestVideo(chosenPath);
						}
						else if(input == "back")
						{
							skipToday = true;
						}
						else if(input == "skip")
						{
							skipToday = true;
							doneWithToday = true;
							break;
						}
						else if(!modifiedCommand)
						{
							double.TryParse(input, out offset);
						}
					}

					if(!skipToday)
					{
						Console.ForegroundColor = ConsoleColor.Yellow;
						Console.WriteLine(info);
						Console.ResetColor();
						CreateTestVideo(chosenPath, offset, lengthPerDay);

						Console.WriteLine("Satisfied with preview? [y/n] or [r] to render without continuing to the next day (in case the preview is unclear)");
						string consoleInput = Console.ReadKey().KeyChar.ToString();
						Console.WriteLine();

						int width, height;
						if(chosenRotation.abbreviation[0] == 'c')
						{
							width = video.height;
							height = video.width;
						}
						else
						{
							width = video.width;
							height = video.height;
						}

						bool willUpscale = false, willPad = false;

						if(width > height)
						{
							if(width < 1920)
							{
								willUpscale = true;
							}
						}
						else
						{
							willPad = true;
						}

						if(consoleInput == "r")
						{
							CreateCompileScriptVideo(chosenPath, offset, lengthPerDay, video.dateTime, willPad, willUpscale, false);
						}
						doneWithToday = consoleInput.ToLower() == "y";
						if(doneWithToday)
						{
							CreateCompileScriptVideo(chosenPath, offset, lengthPerDay, video.dateTime, willPad, willUpscale, true);
						}
					}
				}
			}
		}

		if(runningProcess != null)
		{
			runningProcess.WaitForExit();
		}
	}
}
