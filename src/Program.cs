using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace LTSAnalyzer
{
   class Program
   {
      static void Main(string[] args)
      {
         Options options = new Options();
         if (options.Load(args))
         {
            if (options.Verbose) Console.WriteLine("Cycling Level of Traffic Stress Analyzer v0.1");
            LTSAnalyzer osm = new LTSAnalyzer(options);
            osm.Load();
            osm.Analyze();
            osm.CreateLevelFiles();
         }
      }
   }
}
