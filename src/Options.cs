using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LTSAnalyzer
{
   public enum OutputType
   {
      OSM,
      GeoJSON
   }

   /// <summary>
   /// Class to handle command line options.
   /// </summary>
   public class Options
   {
      public string Filename { get; set; }
      public string Directory { get; set; }
      public string Prefix { get; set; }
      public OutputType OutputType { get; set; }

      public void Usage()
      {
         string name = System.AppDomain.CurrentDomain.FriendlyName;
         Console.WriteLine("OSM Cycling Stress Analyzer");
         Console.WriteLine("Usage: " + name + " -f filename [-o otype][-p prefix]");
         Console.WriteLine("where:");
         Console.WriteLine(" filename is path to the OSM XML input file.");
         Console.WriteLine(" otype    is the output file type. It is either \"osm\" or \"geojson\".");
         Console.WriteLine("          The default is osm.");
         Console.WriteLine(" prefix   is the prefix to be used for all output files.");
         Console.WriteLine("          The default is \"level_\".");
      }

      public bool Load(string[] args)
      {
         Directory = System.IO.Directory.GetCurrentDirectory();
         Filename = "";
         OutputType = OutputType.OSM;
         Prefix = "level_";
         for (int i = 0; i < args.Length; i++)
         {
            string arg = args[i];
            if (arg == "-f")
            {
               if (args.Length >= i)
               {
                  i++;
                  Filename = args[i];
               }
               else
               {
                  Console.WriteLine("Error: -f command line argument must be followed by a file path.");
                  return false;
               }
            }
            else if (arg == "-o")
            {
               if (args.Length >= i)
               {
                  i++;
                  string otype = args[i].Trim().ToLower();
                  if (otype == "osm")
                  {
                     OutputType = OutputType.OSM;
                  }
                  else if (otype == "geojson")
                  {
                     OutputType = OutputType.GeoJSON;
                  }
                  else
                  {
                     Console.WriteLine("Error: -o command line argument must be either OSM or GeoJSON.");
                     return false;
                  }
               }
               else
               {
                  Console.WriteLine("Error: -o command line argument must be followed by either OSM or GeoJSON.");
                  return false;
               }

            }
            else if (arg == "-p")
            {
               if (args.Length >= i)
               {
                  i++;
                  Prefix = args[i];
               }
               else
               {
                  Console.WriteLine("Error: -p command line argument must be followed by a file prefix.");
                  return false;
               }
            }
         }
         if (string.IsNullOrEmpty(Filename) || string.IsNullOrEmpty(Prefix))
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
