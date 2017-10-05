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
      /// Adds a tag key/value from the current read position.
      /// </summary>
      /// <param name="i_reader"></param>
      public void AddTag(XmlReader reader)
      {
         string key = reader.GetAttribute("k");
         string value = reader.GetAttribute("v");
         Tags.Add(key, value);
      }

      /// <summary>
      /// Determines whether the tag key exists in the element.
      /// </summary>
      /// <param name="key"></param>
      /// <returns></returns>
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
