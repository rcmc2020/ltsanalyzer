using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;

namespace LTSAnalyzer {
	public enum OutputType {
		OSM,
		GeoJSON
	}

	/// <summary>
	/// Class to handle command line options.
	/// </summary>
	public class Options {
		public string Filename { get; set; }
		public string Directory { get; set; }
		public string Prefix { get; set; }
		public OutputType OutputType { get; set; }
		public bool Verbose { get; set; }
		public bool Timers { get; set; }

		public readonly string Description = "OSM Cycling Level of Traffic Stress Analyzer";

		public void Usage() {
			string name = System.AppDomain.CurrentDomain.FriendlyName;
			Console.WriteLine(Description);
			Console.WriteLine("Usage: " + name + " -f filename [-d directory][-i][-o otype][-p prefix][-t][-v]");
			Console.WriteLine("where:");
			Console.WriteLine(" -d  dir is the location where all files will be created.");
			Console.WriteLine(" -f  filename  is path to the OSM XML input file.");
			Console.WriteLine(" -o  otype is the output file type. It is either \"osm\" or \"geojson\".");
			Console.WriteLine("     The default is geojson.");
			Console.WriteLine(" -p  prefix is the prefix to be used for all output files.");
			Console.WriteLine("     The default is \"level_\".");
			Console.WriteLine(" -t  Enables timer output.");
			Console.WriteLine(" -v  Enables verbose output.");
		}

		public bool Load(string[] args) {
			// The options are evaluated in the following order:
			// 1. Command line
			// 2. Application Settings in the config file
			// 3. Defaults (except Filename)
			NameValueCollection appSettings = ConfigurationManager.AppSettings;
			Directory = (appSettings["Directory"] == null) ? System.IO.Directory.GetCurrentDirectory() : appSettings["Directory"];
			Filename = (appSettings["InputFilename"] == null) ? "ottawa_canada.osm" : appSettings["InputFilename"];
			OutputType = (appSettings["OutputType"] == null) ? OutputType.GeoJSON : (appSettings["OutputType"].ToLower().Trim() == "osm") ? OutputType.OSM : OutputType.GeoJSON;
			Prefix = (appSettings["StressLevelPrefix"] == null) ? "level_" : appSettings["StressLevelPrefix"];
			Timers = (appSettings["DisplayTimers"] == null) ? false : appSettings["DisplayTimers"].ToLower().Trim() == "true";
			Verbose = (appSettings["Verbose"] == null) ? false : appSettings["Verbose"].ToLower().Trim() == "true";
			for (int i = 0; i < args.Length; i++) {
				string arg = args[i];
				if (arg == "-f") {
					if (args.Length >= i) {
						i++;
						Filename = args[i];
					}
					else {
						Console.WriteLine("Error: -f command line argument must be followed by a file path.");
						return false;
					}
				}
				else if (arg == "-d") {
					if (args.Length >= i) {
						i++;
						Directory = args[i];
					}
				}
				else if (arg == "-o") {
					if (args.Length >= i) {
						i++;
						string otype = args[i].Trim().ToLower();
						if (otype == "osm") {
							OutputType = OutputType.OSM;
						}
						else if (otype == "geojson") {
							OutputType = OutputType.GeoJSON;
						}
						else {
							Console.WriteLine("Error: -o command line argument must be either OSM or GeoJSON.");
							return false;
						}
					}
					else {
						Console.WriteLine("Error: -o command line argument must be followed by either OSM or GeoJSON.");
						return false;
					}

				}
				else if (arg == "-p") {
					if (args.Length >= i) {
						i++;
						Prefix = args[i];
					}
					else {
						Console.WriteLine("Error: -p command line argument must be followed by a file prefix.");
						return false;
					}
				}
				else if (arg == "-t") {
					Timers = true;
				}
				else if (arg == "-v") {
					Verbose = true;
				}
			}
			if (string.IsNullOrEmpty(Filename) || string.IsNullOrEmpty(Prefix)) {
				Usage();
				return false;
			}
			else {
				if (!File.Exists(Filename)) {
					Console.WriteLine("Error: File '" + Filename + "' does not exist.");
					return false;
				}
			}
			if (!Path.IsPathRooted(Filename) && !System.IO.Directory.Exists(Directory)) {
				Console.WriteLine("Error: Directory '" + Directory + "' does not exist.");
				return false;
			}
			return true;
		}
	}
}
