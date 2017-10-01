using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LTSAnalyzer
{
   class AnalysisModel
   {
      /// <summary>
      /// A quick list to quickly elimate specified ways before the analysis phase.
      /// </summary>
      List<string> _ignoreKeyList;

      static int _levels;
      
      static AnalysisModel() {
         _levels = -1;
      }

      public AnalysisModel()
      {
         _ignoreKeyList = new List<string>() { 
            "addr:housenumber", 
            "addr:interpolation", 
            "aeroway", 
            "amenity", 
            "barrier", 
            "boundary", 
            "building", 
            "building:levels", 
            "building:part", 
            "disused:amenity", 
            "indoor", 
            "landcover", 
            "landuse", 
            "leisure", 
            "man_made", 
            "indoor", 
            "natural", 
            "piste:type", 
            "place", 
            "power", 
            "public_transport", 
            "railway", 
            "seamark:type", 
            "shop", 
            "waterway" };
      }
      
      public void Initialize() {
         // If we load from a file, we can initialize this value here.
         _levels = 4;
      }
      
      public static int LevelCount {
         get {
            if (_levels == -1) {
               throw new Exception("AnalysisModel.Initialize not called.");
            }
            return _levels;
         }
      }

      /// <summary>
      /// Returns True if the way is cyclable.
      /// </summary>
      public bool IsCyclable(Way way)
      {
         bool result = false;
         if (way.Tags.Count == 0)
         {
            return false;
         }
         else if (way.Tags.ContainsKey("highway"))
         {
            if (way.HasTag("bicycle", "no"))
            {
               return false;
            }
            else if (way.HasTag("highway", "motorway") || way.HasTag("highway", "motorway_link"))
            {
               if (way.HasTag("bicycle", "yes"))
               {
                  return true;
               }
               else
               {
                  return false;
               }
            }
            else if (way.Tags["highway"] == "service" && way.HasTag("service", "parking_aisle"))
            {
               return false;
            }
            return true;
         }
         else if (way.Tags.ContainsKey("bicycle"))
         {
            if (way.Tags["bicycle"] == "yes")
            {
               if (way.HasTag("piste:type", "nordic"))
               {
                  return false;
               }
               // FIXME: The following statements are incorrect.
               else if (way.HasTag("crossing", "uncontrolled"))
               {
                  return true;
               }
               else
               {
                  return true;
               }
            }
            else
            {
               // FIXME: This isn't correct.
               if (way.HasTag("barrier", "gate"))
               {
                  return false;
               }
               else
               {
                  return false;
               }
            }
         }
         else
         {
            if (way.Tags.Count == 1)
            {
               if (way.Tags.ContainsKey("note") || way.Tags.ContainsKey("level") || way.Tags.ContainsKey("layer"))
               {
                  return false;
               }
            }
            foreach (string s in _ignoreKeyList)
            {
               if (way.Tags.ContainsKey(s)) return false;
            }
         }
         return result;
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
      /// Returns True if the way is a cyclable path.
      /// </summary>
      public bool IsPath(Way way)
      {
         if (way.Tags.ContainsKey("highway"))
         {
            if (way.HasTag("highway", "cycleway") || way.HasTag("highway", "footway"))
            {
               return true;
            }
            else if (way.HasTag("highway", "path"))
            {
               if (way.HasTag("bicycle", "no"))
               {
                  return false;
               }
               else if (way.Tags.ContainsKey("cycleway"))
               {
                  return true;
               }
               else
               {
                  return true;
               }
            }
         }
         return false;
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

      /// <summary>
      /// Return True if the way is a residential road.
      /// </summary>
      public bool IsResidential(Way way)
      {
         return way.HasTag("highway", "residential");
      }

      /// <summary>
      /// Returns True if the way is one way.
      /// </summary>
      public bool IsOneWay(Way way)
      {
         return way.HasTag("oneway", "yes");
      }

      /// <summary>
      /// Returns True if the way is a service road.
      /// </summary>
      public bool IsService(Way way)
      {
         return way.HasTag("highway", "service");
      }


      public void Run(Dictionary<string, Way> ways, Dictionary<string, Node> nodes)
      {
         int level;
         foreach (KeyValuePair<string, Way> kv in ways)
         {
            string id = kv.Key;
            Way way = kv.Value;
            level = 0;
            if (IsCyclable(way))
            {
               if (IsService(way))
               {
                  level = 2;
               }
               else if (IsPath(way))
               {
                  level = 1;
               }
               else if (Lanes(way) > 2 || ((!IsResidential(way)) && Lanes(way) > 1 && IsOneWay(way)))
               {
                  if (MaxSpeed(way) <= 40)
                  {
                     level = 3;
                  }
                  else
                  {
                     level = 4;
                  }
               }
               else if (IsResidential(way))
               {
                  level = 2;
               }
               else
               {
                  if (MaxSpeed(way) <= 40)
                  {
                     level = 2;
                  }
                  else
                  {
                     level = 3;
                  }
               }
            }
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
   }
}
