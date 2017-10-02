using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LTSAnalyzer
{
   public enum NumericOps
   {
      GT,
      GE,
      EQ,
      NE,
      LT,
      LE
   }

   abstract class Rule
   {
      public abstract bool Evaluate(Way way, Dictionary<string, Node> nodes);
   }

   abstract class NumericValue {
      public abstract int GetValue(Way way, Dictionary<string, Node> nodes);
   }

   class TagCount : NumericValue
   {
      public override int GetValue(Way way, Dictionary<string, Node> nodes)
      {
         return way.Tags.Count;
      }
   }

   class HasTag : Rule
   {
      string _value;

      public HasTag(string value)
      {
         _value = value;
      }

      public override bool Evaluate(Way way, Dictionary<string, Node> nodes)
      {
         return (way.Tags.ContainsKey(_value));
      }
   }

   class HasTagValue : Rule
   {
      string _key;
      string _value;

      public HasTagValue(string key, string value)
      {
         _key = key;
         _value = value;
      }

      public override bool Evaluate(Way way, Dictionary<string, Node> nodes)
      {
         if (way.Tags.ContainsKey(_key))
         {
            return (way.Tags[_key] == _value);
         }
         return false;
      }
   }

   class Or : Rule
   {
      Rule _rule1;
      Rule _rule2;

      public Or(Rule rule1, Rule rule2)
      {
         _rule1 = rule1;
         _rule2 = rule2;
      }

      public override bool Evaluate(Way way, Dictionary<string, Node> nodes)
      {
         if (_rule1.Evaluate(way, nodes))
         {
            return true;
         }
         return _rule2.Evaluate(way, nodes);
      }
   }

   class And : Rule
   {
      Rule _rule1;
      Rule _rule2;

      public And(Rule rule1, Rule rule2)
      {
         _rule1 = rule1;
         _rule2 = rule2;
      }

      public override bool Evaluate(Way way, Dictionary<string, Node> nodes)
      {
         if (!_rule1.Evaluate(way, nodes)) return false;
         return _rule2.Evaluate(way, nodes);
      }
   }

   class Not : Rule
   {
      Rule _rule;

      public Not(Rule rule)
      {
         _rule = rule;
      }

      public override bool Evaluate(Way way, Dictionary<string, Node> nodes)
      {
         return !_rule.Evaluate(way, nodes);
      }
   }

   class IsOneOf : Rule
   {
      string _key;
      HashSet<string> _values;

      public IsOneOf(string key, HashSet<string> values)
      {
         _key = key;
         _values = values;

      }

      public override bool Evaluate(Way way, Dictionary<string, Node> nodes)
      {
         if (way.Tags.ContainsKey(_key))
         {
            return _values.Contains(way.Tags[_key]);
         }
         return false;
      }
   }
}
