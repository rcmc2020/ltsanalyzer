using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LTSAnalyzer
{

   public class LatLong
   {
      double Lat { get; set; }
      double Long { get; set; }

      public LatLong(double lat, double lon)
      {
         Lat = lat;
         Long = lon;
      }
   }

   public class PWNode
   {
      public double Lat { get; set; }
      public double Lon { get; set; }
      public double LastAngle { get; set; }

      public PWNode(IMNode node, double angle) {
         Lat = double.Parse(node.Lat);
         Lon = double.Parse(node.Lon);
         LastAngle = angle;
      }
   }


   // Given a set of connected lines, find the smallest polygon that 
   // encloses all lines.
   public class PolygonizeWays
   {
      Dictionary<string, IMWay> _ways;
      Dictionary<string, IMNode> _nodes;
      Dictionary<string, PWNode> _nodeCache;

      public PolygonizeWays() { }

      public void Initialize(Dictionary<string, IMWay> ways, Dictionary<string, IMNode> nodes)
      {
         _ways = ways;
         _nodes = nodes;
      }

      private void InitializeNodeCache(List<string> wayset)
      {
         string nodeId;
         _nodeCache = new Dictionary<string, PWNode>();
         foreach (string wayId in wayset)
         {
            IMWay way = _ways[wayId];
            for (int i = 0; i < way.Nodes.Count; i++)
            {
               nodeId = way.Nodes[i];
               if (_nodeCache.ContainsKey(nodeId))
               {
                  // FIXME: Counts?
               }
               else
               {
                  PWNode pwNode = new PWNode(_nodes[nodeId], 0.0);
                  _nodeCache.Add(nodeId, pwNode);
               }
            }
         }
      }

      public List<LatLong> ProcessWays(List<string> wayset)
      {
         double angle;
         PWNode pwnode;
         PWNode refpwnode;
         List<LatLong> result = new List<LatLong>();
         List<string> pwnodeList = new List<string>();
         InitializeNodeCache(wayset);
         string startNodeId = StartingPoint;
         string previousNodeId = "";
         double previousMaxAngle = 0.0;
         string nodeId = startNodeId;
         double refAngle = 0.0;
         double maxAngle = 360.0;
         throw new NotImplementedException("Untested. Needs cleanup. Not final form.");
         while (!string.IsNullOrEmpty(nodeId)) {
            // Now we start scanning all points that are connected to this node in 
            // a clockwise direction.
            pwnode = _nodeCache[nodeId];
            result.Add(new LatLong(pwnode.Lat, pwnode.Lon));

            // We need to check the list of all nodes that are connected to this node.
            List<string> nodeIds = GetReferencedNodes(nodeId);
            nodeId = "";
            foreach (string refNodeId in nodeIds)
            {
               if (refNodeId != previousNodeId)
               {
                  refpwnode = _nodeCache[refNodeId];
                  double o = refpwnode.Lat - pwnode.Lat;
                  double a = refpwnode.Lon - pwnode.Lon;
                  double radians = Math.Atan2(o, a);
                  angle = (radians >= 0) ? 360 - radians * (180.0 / Math.PI) : -(radians * (180.0 / Math.PI));
                  // We are looking for the next node that occurs in a clockwise angle from 
                  // our base angle on the pwnode. Note that it may return to the last node 
                  // we were at.
                  double effectiveAngle = ((360 - refAngle) + angle + 360) % 360;
                  if (effectiveAngle < maxAngle)
                  {
                     maxAngle = angle;
                     nodeId = refNodeId;
                  }

                  // We flip the angle around so that we start from the view
                  // of the last point on the next round.
                  refAngle = (maxAngle + 180.0) % 360.0;
               }
            }
            previousNodeId = nodeId;
            previousMaxAngle = maxAngle;
            if (nodeId == "")
            {
               nodeId = previousNodeId;
               refAngle = (maxAngle + 180.0) % 360.0;
            }
            else if (nodeId == startNodeId)
            {
               // We've come full circle.
               pwnode = _nodeCache[nodeId];
               result.Add(new LatLong(pwnode.Lat, pwnode.Lon));
               nodeId = "";
            }
         }

         return result;
      }

      private List<string> GetReferencedNodes(string nodeId) 
      {
         List<string> result = new List<string>();
         List<string> ways = _nodes[nodeId].Ways;
         foreach (string wayId in ways)
         {
            IMWay way = _ways[wayId];
            for (int n = 0; n < way.Nodes.Count; n++)
            {
               if (way.Nodes[n] == nodeId) {
                  // We've found the node referenced within the way so 
                  // now find the connected nodes.
                  if (n > 0) {
                     if (!result.Contains(way.Nodes[n - 1])) {
                        result.Add(way.Nodes[n - 1]);
                     }
                  }
                  if (n < way.Nodes.Count - 1) {
                     if (!result.Contains(way.Nodes[n + 1])) {
                        result.Add(way.Nodes[n + 1]);
                     }
                  }
               }
            }
         }

         return result;
      }

      /// <summary>
      /// Find the starting point of the entire set.
      /// </summary>
      private string StartingPoint
      {
         get
         {
            double maxValue = -90.0;
            double lon;
            string startNode = "";

            foreach (KeyValuePair<string, PWNode> kv in _nodeCache)
            {

               lon = kv.Value.Lon;
               if (lon > maxValue)
               {
                  maxValue = lon;
                  startNode = kv.Key;
               }
            }

            return startNode;
         }
      }
   }
}