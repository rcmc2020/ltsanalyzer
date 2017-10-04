using System;
using System.Collections.Generic;
using System.Xml;

namespace LTSAnalyzer
{
   /// <summary>
   /// Base class for the Relation, Way, and Node elements providing basic tag
   /// handling services.
   /// </summary>
   abstract class ElementBase
   {
      static ElementBase() { }

      bool[] _levelReference;

      protected Dictionary<string, string> _tags;

      /// <summary>
      /// Returns the tag collection for this element.
      /// </summary>
      public Dictionary<string, string> Tags
      {
         get
         {
            if (_tags == null)
            {
               _tags = new Dictionary<string, string>();
            }
            return _tags;
         }
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
         if (_levelReference == null) {
            _levelReference = new bool[AnalysisModel.LevelCount];
         }
         _levelReference[level - 1] = true;
      }

      /// <summary>
      /// Adds a tag key/value from the current read position.
      /// </summary>
      /// <param name="i_reader"></param>
      public void AddTag(XmlReader reader)
      {
         string key = reader.GetAttribute("k");
         string value = reader.GetAttribute("v");
         Tags.Add(key, value);
      }

      public bool HasTag(string key)
      {
         return _tags != null && _tags.ContainsKey(key);
      }

      /// <summary>
      /// Determines whether the current element has the specified tag with
      /// the specified value.
      /// </summary>
      /// <param name="key">The key value of the tag.</param>
      /// <param name="value">The value of the tag.</param>
      /// <returns></returns>
      public bool HasTag(string key, string value)
      {
         return _tags != null && _tags.ContainsKey(key) && _tags[key] == value;
      }
   }
}
