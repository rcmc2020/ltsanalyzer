using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LTSAnalyzer
{
	/// <summary>
	/// Class to handle command line options.
	/// </summary>
	public class Options
	{
		public string Filename { get; set; }
		public string Directory { get; set; }
		public string Output { get; set; }

		public void Usage()
		{
			Console.WriteLine("OSM Cycling Stress Analyzer");
			Console.WriteLine("Usage: ltsanalyzer -f filename [-p prefix]");
			Console.WriteLine("where:");
			Console.WriteLine("filename is the name of an OSM XML file.");
			Console.WriteLine("prefix   is the prefix to be used for all output files (default is level_).");
		}

		public bool Load(string[] args)
		{
			Directory = System.IO.Directory.GetCurrentDirectory();
			Filename = "";
			Output = "level_";
			for (int i = 0; i < args.Length; i++)
			{
				string arg = args[i];
				if (arg == "-f")
				{
					if (args.Length > i + 1)
					{
						Filename = args[i + 1];
					}
				}
				if (arg == "-p")
				{
					if (args.Length > i + 1)
					{
						Output = args[i + 1];
					}
				}
			}
			if (string.IsNullOrEmpty(Filename) || string.IsNullOrEmpty(Output))
			{
				Usage();
				return false;
			}
			else
			{
				if (!Path.IsPathRooted(Filename))
				{
					Filename = System.IO.Path.Combine(Directory, Filename);
				}
				if (!File.Exists(Filename))
				{
					Console.WriteLine("Error: File '" + Filename + "' does not exist.");
					return false;
				}
			}
			return true;
		}
	}
}
