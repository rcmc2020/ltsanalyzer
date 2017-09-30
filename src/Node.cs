using System;
using System.Collections.Generic;
using System.Text;

namespace LTSAnalyzer
{
   class Node : ElementBase
   {
      public string Lat { get; set; }

      public string Lon { get; set; }

      public Node(string lat, string lon)
      {
         Lat = lat;
         Lon = lon;
      }
   }
}
