using System;
using System.Collections.Generic;
using System.Text;

namespace LTSAnalyzer
{
   class Way : ElementBase
   {
      /// <summary>
      /// A list of all nodes associated with this way.
      /// </summary>
      List<string> _nodes;

      /// <summary>
      /// A way is associated with a single level.
      /// </summary>
      public int Level { get; set; }

      /// <summary>
      /// The calculated maximum speed of the way.
      /// </summary>
      public int MaxSpeed { get; set; }

      /// <summary>
      /// The calculated number of lanes in the way.
      /// </summary>
      public int Lanes { get; set; }

      /// <summary>
      /// True if there is parking on the way.
      /// </summary>
      public bool IsParkingPresent { get; set; }

      /// <summary>
      /// Returns the list of all nodes associated with this way.
      /// </summary>
      public List<string> Nodes
      {
         get
         {
            if (_nodes == null)
            {
               _nodes = new List<string>();
            }
            return _nodes;
         }
         set { _nodes = value; }
      }
   }
}
