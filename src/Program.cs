using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;

namespace LTSAnalyzer {
	class Program {
		static void Main(string[] args) {
			Stopwatch sw = new Stopwatch();
			sw.Start();
			Options options = new Options();
			if (options.Load(args)) {
				if (options.Verbose) Console.WriteLine(options.Description);
				LTSAnalyzer osm = new LTSAnalyzer(options);
				osm.Load1();
				osm.Load2();
				osm.AnalyzeStressModel();
				osm.CreateLevelFiles();
			}
			sw.Stop();
			if (options.Timers || options.Verbose) Console.WriteLine("TOTAL -   Elapsed time: " + sw.Elapsed);
		}
	}
}
