using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

public class Program
{
	static private void PrintList<T>(List<T> list)
	{
		Console.Write("[");
		if (list.Count > 0)
		{
			Console.Write(list[0]);
			for(int i = 1; i < list.Count; i++)
			{
				Console.Write(", ");
				Console.Write(list[i]);
			}
		}
		Console.WriteLine("]");
	}

	static private void GetVideoInfo(string fileName, out int width, out int height, out double length)
	{
		width = height = 0;
		length = 0.0;
		ProcessStartInfo psi = new ProcessStartInfo("ffprobe", "-v error -select_streams v:0 -show_entries stream=width,height,duration -of default=noprint_wrappers=1:nokey=1 " + fileName);
		psi.UseShellExecute = false;
		psi.CreateNoWindow = true;
		psi.RedirectStandardOutput = true;

		Process proc = new Process();
		proc.StartInfo = psi;

		proc.Start();
		proc.WaitForExit();

		string output = proc.StandardOutput.ReadToEnd();
		
		string[] outputLines = output.Split(new char[] { '\r', '\n', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
		/*
		foreach(string s in outputLines)
		{
			Console.WriteLine(s);
		}
		Console.WriteLine(outputLines.Length);
		Console.ReadKey();
		*/
		
		int.TryParse(outputLines[0], out width);
		int.TryParse(outputLines[1], out height);
		double.TryParse(outputLines[2], out length);
		
	}

	static private void CreateTestVideo(string filename, double offset)
	{
		ProcessStartInfo psi = new ProcessStartInfo("ffmpeg", "-y -v error -i " + filename + " -ss " + offset + " -t 1 -codec copy testScreen.mp4");
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
		ProcessStartInfo psi = new ProcessStartInfo("ffmpeg", "-y -v error -i " + filename + " -codec copy testScreen.mp4");
		psi.UseShellExecute = false;
		psi.CreateNoWindow = true;
		psi.RedirectStandardOutput = false;

		Process proc = new Process();
		proc.StartInfo = psi;

		proc.Start();
		proc.WaitForExit();
	}

	static private void CreateVideo(string filename, double offset, DateTime dt)
	{
		//ProcessStartInfo psi = new ProcessStartInfo("ffmpeg", "-y -i " + filename + " -ss " + offset + " -t 1 -codec copy" + CreateFilename(dt));
		ProcessStartInfo psi = new ProcessStartInfo("ffmpeg", "-y -v error -i " + filename + " -ss " + offset + " -t 1 -vf drawtext=\"fontfile =/usr/share/fonts/truetype/lato/Lato-Bold.ttf: text = '" + CreateDateString(dt) + "': fontcolor = white: fontsize = 48: box = 1: boxcolor = black@0.5: boxborderw = 10: x = w / 32: y = h - w / 32 - text_h\" -codec:a copy -strict -2 " + CreateFilename(dt));
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
		return dt.Year.ToString("D4") + "-" + dt.Month.ToString("D2") + "-" + dt.Day.ToString("D2") + ".mp4";
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
		//folderName = "/mnt/c/Users/Fons/Desktop/testeterrr/";
		while (string.IsNullOrWhiteSpace(folderName) || !Directory.Exists(folderName))
		{
			Console.WriteLine("Folder name:");
			folderName = Console.ReadLine();
		}



		List<string> fileNames = Directory.GetFiles(folderName).ToList();
		int count = fileNames.Count;

		Console.WriteLine("Processing files...");

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

			//Console.WriteLine(w);

			lengths.Add(l);
			widths.Add(w);
			heights.Add(h);
		}
		Console.CursorLeft = 0;
		Console.WriteLine("100%");

		//var toRemove = Enumerable.Range(0, count).Where(i => lengths[i] < 1.0 || widths[i] != 1920 || heights[i] != 1080).ToList();
		var toRemove = Enumerable.Range(0, count).Where(i => lengths[i] < 1.0).ToList();
		Console.WriteLine("Ignoring {0} short (<1 sec) clips.", toRemove.Count);

		for(int i = 0; i < toRemove.Count; i++)
		{
			fileNames.RemoveAt(toRemove[i] - i);
			lengths.RemoveAt(toRemove[i] - i);
			count--;
		}

		List<DateTime> dates = fileNames.Select(f => new FileInfo(f).LastWriteTime).ToList();


		var firstDay = dates.Min().Date;
		var lastDay = dates.Max().Date;

		//Console.WriteLine(firstDay);
		//Console.WriteLine(lastDay);

		//CreateTestVideo(fileNames[0], 0.0);
		//CreateVideo(fileNames[0], 1.0, dates[0]);
		//Console.WriteLine(CreateDateString(dates[0]));


		
		for(DateTime date = firstDay; date <= lastDay; date = date.AddDays(1))
		{
			List<int> indices = Enumerable.Range(0, count).Where(i => dates[i].Date == date).ToList();
			Console.WriteLine("{0}: {1} videos found.", date.ToShortDateString(), indices.Count);
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
						Console.WriteLine("Choose offset: [0.0 - {0}] or [full] or [skip]", length - 1.0);
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
						CreateTestVideo(fileNames[vidNum], offset);

						Console.WriteLine("Satisfied? [y/n]");
						done = Console.ReadKey().KeyChar.ToString().ToLower() == "y";
						Console.WriteLine();
						if (done)
						{
							CreateVideo(fileNames[vidNum], offset, dates[vidNum]);
						}
					}
				}

			}
		}
		
		//Console.WriteLine(File.Exists(@"/mnt/c/Users/Fons/Desktop/test.MOV"));
		//Console.WriteLine(new FileInfo(@"/mnt/c/Users/Fons/Desktop/test.MOV").LastWriteTime);
	}
}
