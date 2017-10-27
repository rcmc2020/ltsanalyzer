using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LTSAnalyzer
{

   public class LatLong
   {
      public double Lat { get; set; }
      public double Long { get; set; }

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
   // FIXME: The current implementation does not allow for donuts.
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

      /// <summary>
      /// This walks around the outside of the nodes to create a polygon.
      /// </summary>
      /// <param name="wayset"></param>
      /// <returns></returns>
      public List<LatLong> ProcessWays(List<string> wayset)
      {
         double angle;
         double nextAngle;
         string nextNodeId;
         string prevNodeId = "";
         string firstNodeId = "";
         PWNode currentNode;
         PWNode referencedNode;
         List<LatLong> result = new List<LatLong>();
         InitializeNodeCache(wayset);
         string startNodeId = StartingPoint;
         string currentNodeId = startNodeId;
         double refAngle = 0.0;
         double minAngle = 360.0;

         Dictionary<string, int> counts = new Dictionary<string, int>();
         // StringBuilder debug = new StringBuilder();
         // debug.Append("<html><head></head><body>");
         // int debugCount = 0;

         // We assume the first node as the starting point. 
         currentNode = _nodeCache[currentNodeId];
         result.Add(new LatLong(currentNode.Lat, currentNode.Lon));

         bool firstPass = true;
         while (!string.IsNullOrEmpty(currentNodeId))
         {
            nextNodeId = "";
            minAngle = 360.0;
            nextAngle = 0.0;
            // We need to check the list of all nodes that are connected to this node.
            List<string> nodeIds = GetReferencedNodes(currentNodeId);
            foreach (string refNodeId in nodeIds)
            {
               if (_nodeCache.ContainsKey(refNodeId))
               {
                  referencedNode = _nodeCache[refNodeId];
                  double o = referencedNode.Lat - currentNode.Lat;
                  double a = referencedNode.Lon - currentNode.Lon;
                  double radians = Math.Atan2(o, a);
                  angle = (radians >= 0) ? 360 - radians * (180.0 / Math.PI) : -(radians * (180.0 / Math.PI));
                  // We are looking for the next node that occurs in a clockwise angle from 
                  // our base angle on the pwnode. Note that it may return to the last node 
                  // we were at.
                  double effectiveAngle = ((360 - refAngle) + angle + 360) % 360;
                  // Is this the closest node within our verification arc?
                  if (refNodeId == prevNodeId)
                  {
                     if (nextNodeId == "")
                     {
                        // The only condition where we are allowed to refer right back to 
                        // the previous node is when it is the only one.
                        nextNodeId = refNodeId;
                        minAngle = 360.0;
                        nextAngle = angle;
                     }
                  }
                  else if (effectiveAngle < minAngle)
                  {
                     minAngle = effectiveAngle;
                     nextNodeId = refNodeId;
                     nextAngle = angle;
                  }
               }
            }
            if (firstPass) {
               // Our end condition is when we have encountered the firstNode again after the startNode.
               // This takes care of the case where another way occurs off the starting node.
               firstPass = false;
               firstNodeId = nextNodeId;
            }
            else if (currentNodeId == startNodeId && nextNodeId == firstNodeId) 
            {
               // We're done.
               break;
            }
            if (counts.ContainsKey(nextNodeId))
            {
               counts[nextNodeId]++;
            }
            else
            {
               counts.Add(nextNodeId, 1);
            }

            // debug.Append(@"<a href='https://www.openstreetmap.org/node/" + nextNodeId + "'>Node: " + nextNodeId + " (" + counts[nextNodeId].ToString() + ")</a><br>");
            prevNodeId = currentNodeId;
            currentNodeId = nextNodeId;
            refAngle = (nextAngle + 180.0) % 360.0;
            currentNode = _nodeCache[currentNodeId];
            result.Add(new LatLong(currentNode.Lat, currentNode.Lon));
            if (result.Count > 100000) throw new Exception("Probably in an endless loop. Aborting...");
         }
         // using (StreamWriter file = new StreamWriter(@"h:\temp.html")) file.Write(debug.ToString());
         return result;
      }

      /// <summary>
      /// Returns a list of all unique nodes referenced by the specified nodeId.
      /// </summary>
      /// <param name="nodeId"></param>
      /// <returns></returns>
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
      /// Find the starting point of the entire set. This is defined as the eastern-most 
      /// point of any of the ways.
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