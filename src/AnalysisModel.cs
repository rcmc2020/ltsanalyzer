using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LTSAnalyzer
{
   class AnalysisModel
   {
      /// <summary>
      /// The number of levels in the analysis. Currently this value is fixed but 
      /// in future versions, it may be loaded with the analysis definition.
      /// </summary>
      static int _levels;

      static AnalysisModel()
      {
         _levels = -1;
      }

      public AnalysisModel() {}

      public void Initialize()
      {
         // If we load the analysis model from a definition file, we 
         // can initialize the number of levels value here.
         _levels = 4;
      }

      public static int LevelCount
      {
         get
         {
            if (_levels == -1)
            {
               throw new Exception("AnalysisModel.Initialize not called.");
            }
            return _levels;
         }
      }

      /// <summary>
      /// Returns the number of lanes in defined for the way if possible, else -1.
      /// </summary>
      public int Lanes(Way way)
      {
         if (way.Tags.ContainsKey("lanes"))
         {
            string l = way.Tags["lanes"];
            if (l.Contains(";"))
            {
               string[] list = l.Split(';');
               int lmax = 1;
               foreach (string s in list)
               {
                  if (int.Parse(s) > lmax)
                  {
                     lmax = int.Parse(s);
                  }
               }
               return lmax;
            }
            else
            {
               return int.Parse(way.Tags["lanes"]);
            }
         }
         else return -1;
      }

      /// <summary>
      /// Returns the maximum speed of the way.
      /// </summary>
      public int MaxSpeed(Way way)
      {
         if (way.Tags.ContainsKey("maxspeed"))
         {
            int result;
            string maxspeed = way.Tags["maxspeed"];
            if (maxspeed == "national")
            {
               return 40;
            }
            else if (int.TryParse(maxspeed, out result))
            {
               return result;
            }
         }
         else
         {
            if (way.HasTag("highway", "motorway"))
            {
               return 100;
            }
         }
         return 50;
      }

      public void Run(Dictionary<string, Way> ways, Dictionary<string, Node> nodes)
      {
         int level;
         foreach (KeyValuePair<string, Way> kv in ways)
         {
            string id = kv.Key;
            Way way = kv.Value;
            level = EvaluateWay(id, way);
            way.Level = level;
            // This marks all nodes in our file with being referenced in that level.
            if (level > 0)
            {
               foreach (string node in way.Nodes)
               {
                  nodes[node].SetLevelReference(level);
               }
            }
         }
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="id"></param>
      /// <param name="way"></param>
      /// <returns></returns>
      public int EvaluateWay(string id, Way way)
      {
         if (way.HasTag("highway") || way.HasTag("bicycle"))
         {
            if (way.HasTag("bicycle", "no"))
            {
               return 0;
            }
            if (way.HasTag("highway", "motorway") || way.HasTag("highway", "motorway_link"))
            {
               return 0;
            }
            if (way.HasTag("highway", "service"))
            {
               return (way.HasTag("service", "driveway") || way.HasTag("service", "parking_aisle")) ? 0 : 2;
            }
            if (way.HasTag("highway", "cycleway") || way.HasTag("highway", "footway"))
            {
               return 1;
            }
            if (way.HasTag("highway", "path"))
            {
               return 1;
            }
            if (Lanes(way) > 2 || ((!way.HasTag("highway", "residential")) && Lanes(way) > 1 && way.HasTag("oneway", "yes")))
            {
               if (MaxSpeed(way) <= 40)
               {
                  return 3;
               }
               else
               {
                  return 4;
               }
            }
            if (way.HasTag("highway", "residential"))
            {
               if (MaxSpeed(way) > 50)
               {
                  return 3;
               }
               return 2;
            }
            if (MaxSpeed(way) <= 40)
            {
               return 2;
            }
            else
            {
               return 3;
            }
         }
         return 0;
      }
   }
}