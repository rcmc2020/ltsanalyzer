using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LTSAnalyzer
{
   class StressModel
   {
      Dictionary<string, Way> _ways;

      Dictionary<string, Node> _nodes;

      /// <summary>
      /// This is used for testing.
      /// </summary>
      Dictionary<string, int> _tagCollection;
      
      /// <summary>
      /// The number of levels in the analysis. Currently this value is fixed but 
      /// in future versions, it may be loaded with the analysis definition.
      /// </summary>
      static int _levels;

      static StressModel()
      {
         _levels = -1;
      }

      public StressModel() {}

      public void Initialize(Dictionary<string, Way> ways, Dictionary<string, Node> nodes)
      {
         // If we load the analysis model from a definition file, we 
         // can initialize the number of levels value here.
         _nodes = nodes;
         _ways = ways;
         _levels = 4;
         _tagCollection = new Dictionary<string, int>();

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
      private int Lanes(Way way)
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
         else
         {
            return 2;
         }
      }

      /// <summary>
      /// Calculates the maximum speed for this way.
      /// </summary>
      /// <param name="id"></param>
      /// <param name="way"></param>
      private int MaxSpeed(Way way)
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
            else
            {
               throw new Exception("Error: Unknown maxspeed value '" + maxspeed + "'.");
            }
         }
         else
         {
            if (way.HasTag("highway", "motorway"))
            {
               return 100;
            }
            else
            {
               return 50;
            }
         }
      }

      /// <summary>
      /// Determines if the way has parking on it.
      /// </summary>
      /// <param name="way"></param>
      /// <returns></returns>
      private bool ParkingPresent(Way way)
      {
         bool isParking = false;
         if (way.HasTag("parking")) 
         {
            isParking = true;
            string newtag = "parking=" + way.Tags["parking"];
            if (!_tagCollection.ContainsKey(newtag))
            {
               _tagCollection.Add(newtag, 1);
            }
            else
            {
               _tagCollection[newtag]++;
            }
            switch (way.Tags["parking"])
            {
               case "yes": return true;
               case "no": return false;
               default:
                  break;
            }
         }
         if (way.TagStartsWith("parking:"))
         {
            if (isParking)
            {
               int debug = 1;
            }
            foreach (KeyValuePair<string, string> tag in way.Tags)
            {
               string k = tag.Key;
               if (k.StartsWith("parking:"))
               {
                  string v = tag.Value;
                  switch (tag.Key)
                  {
                     case "parking:condition:both:default":
                     case "parking:condition:both:maxstay":
                     case "parking:condition:both:time_interval":
                     case "parking:condition:left":
                     case "parking:condition:left:time_interval":
                     case "parking:condition:right":
                     case "parking:condition:right:maxstay":
                     case "parking:condition:right:time_interval":
                     case "parking:lane":
                     case "parking:lane:both":
                     case "parking:lane:left":
                     case "parking:lane:right":
                     default:
                        string newtag = k + "=" + v;
                        if (!_tagCollection.ContainsKey(newtag))
                        {
                           _tagCollection.Add(newtag, 1);
                        }
                        else
                        {
                           _tagCollection[newtag]++;
                        }
                        break;
                  }
               }
            }
         }
         return false;
      }

      public void RunAnalysis()
      {
         int level;
         foreach (KeyValuePair<string, Way> kv in _ways)
         {
            string id = kv.Key;
            Way way = kv.Value;
            
            way.MaxSpeed = MaxSpeed(way);
            way.Lanes = Lanes(way);
            way.IsParkingPresent = ParkingPresent(way);

            level = EvaluateWay(id, way);
            way.Level = level;
            // This marks all nodes in our file with being referenced in that level.
            if (way.Level > 0)
            {
               foreach (string node in way.Nodes)
               {
                  _nodes[node].SetLevelReference(level);
               }
            }
         }
         if (_tagCollection.Count > 0) {
            // FIXME: This collection is used during testing to gather information on tags.
            // It should be removed before final deployment.
            using (StreamWriter writer = new StreamWriter(@"tags.txt", false))
            {
               foreach (KeyValuePair<string, int> kv in _tagCollection)
               {
                  writer.Write(kv.Key);
                  writer.Write(" : ");
                  writer.Write(kv.Value.ToString());
                  writer.Write("\r\n");
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
         bool cyclewayLeft = false;
         bool cyclewayRight = false;
         bool parkingLaneRight = false;
         bool parkingLaneLeft = false;
         List<string> NamespaceTags = way.TagsWithNamespace();
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
            if (way.HasTag("footway", "sidewalk")) {
               if (!way.HasTag("bicycle", "yes")) {
                  if (way.HasTag("highway", "footway") || way.HasTag("highway", "path")) {
                     return 0;
                  }
                  else {
                     int debug = 1;
                  }
               }
            }
            if (way.HasTag("highway", "cycleway") || way.HasTag("highway", "footway") || way.HasTag("highway", "walkway") || way.HasTag("highway", "path"))
            {
               return 1;
            }
            if (NamespaceTags.Count > 0)
            {
               if (way.HasTag("cycleway:left"))
               {
                  cyclewayLeft = true;
               }
               if (way.HasTag("cycleway:right"))
               {
                  cyclewayRight = true;
               }
               if (way.HasTag("parking:lane:right"))
               {
                  parkingLaneRight = true;
               }
               if (way.HasTag("parking:lane:left"))
               {
                  parkingLaneLeft = true;
               }
            }
            if (way.HasTag("cycleway", "track"))
            {
               return 2;
            }
            if (way.Lanes > 2 || ((!way.HasTag("highway", "residential")) && way.Lanes > 1 && way.HasTag("oneway", "yes")))
            {
               if (way.MaxSpeed <= 40)
               {
                  if (way.HasTag("cycleway", "lane"))
                  {
                     return 2;
                  }
                  return 3;
               }
               else
               {
                  if (way.HasTag("cycleway", "lane"))
                  {
                     return 3;
                  }
                  return 4;
               }
            }
            if (way.HasTag("highway", "residential"))
            {
               if (way.MaxSpeed > 50)
               {
                  return 3;
               }
               return 2;
            }
            if (way.MaxSpeed <= 40)
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