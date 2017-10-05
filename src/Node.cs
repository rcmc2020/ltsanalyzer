﻿using System;
using System.Collections.Generic;
using System.Text;

namespace LTSAnalyzer
{
   class Node : ElementBase
   {
      bool[] _levelReference;

      public string Lat { get; set; }

      public string Lon { get; set; }

      public Node(string lat, string lon)
      {
         Lat = lat;
         Lon = lon;
      }

      /// <summary>
      /// Indicates whether the element belongs to the specified level .
      /// </summary>
      /// <param name="level">A value from 1 to 4 indicating the level.</param>
      /// <returns>True if the element belongs to the specified level, else False.</returns>
      public bool IsLevel(int level)
      {
         return (_levelReference == null) ? false : _levelReference[level - 1];
      }

      /// <summary>
      /// Marks this element as belonging to the specified level.
      /// </summary>
      /// <param name="level">A value from 1 to 4 indicating the level.</param>
      public void SetLevelReference(int level)
      {
         if (_levelReference == null)
         {
            _levelReference = new bool[AnalysisModel.LevelCount];
         }
         _levelReference[level - 1] = true;
      }
   }
}
