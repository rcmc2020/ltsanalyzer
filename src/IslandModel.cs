using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LTSAnalyzer
{

   public class IMWay {

      /// <summary>
      /// A way is associated with a single level.
      /// </summary>
      public int Level { get; set; }

      public int Island { get; set; }

      /// <summary>
      /// A list of all nodes associated with this way.
      /// </summary>
      List<string> _nodes;

      private IMWay() {}

      public IMWay(Way way)
      {
         this.Level = way.Level;
         this._nodes = new List<string>(way.Nodes);
         this.Island = 0;
      }

      public IMWay(IMWay way)
      {
         this.Level = way.Level;
         this._nodes = new List<string>(way.Nodes);
         this.Island = 0;
      }

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

   public class IMNode {

      public string Lat { get; set; }

      public string Lon { get; set; }

      /// <summary>
      /// A list of all the ways to which this node belongs.
      /// </summary>
      List<string> _ways;

      private IMNode() {}

      public IMNode(Node node)
      {
         Lat = node.Lat;
         Lon = node.Lon;
         _ways = node.Ways;
      }

      public IMNode(string wayId, IMNode node)
      {
         Lat = node.Lat;
         Lon = node.Lon;
         _ways = new List<string>(1);
         _ways.Add(wayId);
      }

      /// <summary>
      /// This is a list of the id of ways that reference this node.
      /// </summary>
      public List<string> Ways
      {
         get
         {
            return _ways;
         }
      }
   }

   class IslandModel
   {
      int _maxStressLevel;

      Dictionary<string, IMWay> _ways;

      Dictionary<string, IMNode> _nodes;

      int _islandCount;

      /// <summary>
      /// We may use this for reallocating ways in the island analysis.
      /// </summary>
      Int64 _maxWay;

      /// <summary>
      /// We may use this for reallocating ways in the island analysis.
      /// </summary>
      Int64 _maxNode;

      public void Initialize(Dictionary<string, Way> ways, Dictionary<string, Node> nodes)
      {
         _maxWay = 0;
         _maxNode = 0;
         _islandCount = 0;
         // If we load the model from a definition file, we 
         // can do a lot of the preliminary work here.
         _maxStressLevel = 2;
         // We copy the dictionary because we are going to split ways and 
         // create new nodes. Note that we should not be modifying the original nodes 
         // in this case; we should remove the original way and generate new ways
         // and possibly new nodes as well.
         _ways = new Dictionary<string, IMWay>();
         foreach (KeyValuePair<string, Way> kv in ways)
         {
            Int64 idNum = Int64.Parse(kv.Key);
            if (idNum > _maxWay) _maxWay = idNum;
            _ways.Add(kv.Key, new IMWay(kv.Value));
         }
         _nodes = new Dictionary<string, IMNode>();
         foreach (KeyValuePair<string, Node> kv in nodes)
         {
            Int64 idNum = Int64.Parse(kv.Key);
            if (idNum > _maxNode) _maxNode = idNum;
            _nodes.Add(kv.Key, new IMNode(kv.Value));
         }
      }

      public void RunAnalysis()
      {
         Pass1();
         Pass2();
         Pass3();
      }

      private void Pass1()
      {
         string[] wayIds = _ways.Keys.ToArray();

         // We use a fix list of strings rather than iterate over the
         // collection because we are adjusting the collection as we go.
         foreach (string wayId in wayIds)
         {
            EvaluateWay(wayId);
         }
      }

      /// <summary>
      /// Look at each way and see if it crosses a stressful boundary.
      /// </summary>
      /// <param name="wayId"></param>
      /// <param name="way"></param>
      private void EvaluateWay(string wayId)
      {
         IMWay crossWay;
         IMWay way = _ways[wayId];

         // If the island number is 0, then we haven't processed this way before.
         if (way.Island == 0)
         {
            if (way.Level > 0 && way.Level <= _maxStressLevel)
            {
               way.Island = 1;
               int nodeStart = 0;
               int nodeEnd = way.Nodes.Count - 1;
               int nodeNo = 0;
               string[] nodeIds = way.Nodes.ToArray();
               foreach (string nodeId in nodeIds)
               {
                  IMNode node = _nodes[nodeId];
                  if (node.Ways.Count > 1)
                  {
                     // This node is an intersection.
                     string[] crossways = node.Ways.Where(s => s != wayId).ToArray();
                     bool cantCross = false;
                     foreach (string crosswayId in crossways)
                     {
                        crossWay = _ways[crosswayId];
                        // FIXME: This test is more complex than this. For now,
                        // we're just testing the code with a simple test.
                        if (crossWay.Level > _maxStressLevel)
                        {
                           cantCross = true;
                           break;
                        }
                     }
                     if (cantCross)
                     {
                        // This node represents an impassable intersection.
                        if (nodeNo == nodeStart)
                        {
                           DeadStart(wayId, way);
                        }
                        else if (nodeNo == nodeEnd)
                        {
                           // We're at the end of the way. We can just remove the connectivity.
                           DeadEnd(wayId, way);
                        }
                        else
                        {
                           // This means that the way is split by a stress node.
                           string newWayId = SplitWay(wayId, way, nodeNo);

                           // Now evaluate the new section of the way.
                           EvaluateWay(newWayId);
                           break;
                        }
                     }
                  }
                  nodeNo++;
               }
            }
            else
            {
               // This indicates that it doesn't belong to any island because it is high-stress.
               way.Island = -1;
            }
         }
      }

      /// <summary>
      /// 
      /// </summary>
      private void Pass2()
      {
         int island = 2;
         string wayId;
         IMWay way;

         // Tried this recursively but it blew out the stack.
         Queue<string> pending = new Queue<string>(_ways.Keys);
         Stack<string> connected = new Stack<string>();
         while (pending.Count > 0)
         {
            bool islandFound = false;
            int waysInIsland = 0;
            connected.Push(pending.Dequeue());
            while (connected.Count > 0)
            {
               wayId = connected.Pop();
               way = _ways[wayId];
               if (way.Island == 1)
               {
                  islandFound = true;
                  waysInIsland++;
                  way.Island = island;
                  foreach (string nodeId in way.Nodes)
                  {
                     IMNode node = _nodes[nodeId];
                     foreach (string connectedWay in node.Ways)
                     {
                        if (connectedWay != wayId)
                        {
                           connected.Push(connectedWay);
                        }
                     }
                  }
               }
            }
            if (islandFound)
            {
               island++;
            }
         }
         _islandCount = island - 2;
      }

      private void Pass3()
      {
         Dictionary<int, List<string>> islands = new Dictionary<int, List<string>>(_islandCount);
         foreach (string wayId in _ways.Keys)
         {
            IMWay way = _ways[wayId];
            if (way.Island == 1)
            {
               throw new Exception("We should never get here. All Islands should have a value of either -1 or a value greater than 1.");
            }
            else if (way.Island > 1)
            {
               if (islands.ContainsKey(way.Island))
               {
                  islands[way.Island].Add(wayId);
               }
               else
               {
                  islands.Add(way.Island, new List<string>{wayId});
               }
            }
         }
         int max = 0;
         foreach (KeyValuePair<int, List<string>> kv in islands)
         {
            if (kv.Value.Count > max)
            {
               max = kv.Value.Count;
            }
         }
         int[] dist = new int[max + 1];
         foreach (KeyValuePair<int, List<string>> kv in islands)
         {
            dist[kv.Value.Count]++;
         }
      }

      private string NextNodeId()
      {
         return (++_maxNode).ToString();
      }

      private string NextWayId()
      {
         return (++_maxWay).ToString();
      }

      private void DeadStart(string wayId, IMWay way)
      {
         string newNodeId = NextNodeId();
         IMNode newNode = new IMNode(wayId, _nodes[way.Nodes[0]]);
         _nodes.Add(newNodeId, newNode);
         _nodes[way.Nodes[0]].Ways.Remove(wayId);
         way.Nodes[0] = newNodeId;
      }

      private void DeadEnd(string wayId, IMWay way)
      {
         string newNodeId = NextNodeId();
         int pos = way.Nodes.Count - 1;
         IMNode newNode = new IMNode(wayId, _nodes[way.Nodes[pos]]);
         _nodes.Add(newNodeId, newNode);
         _nodes[way.Nodes[pos]].Ways.Remove(wayId);
         way.Nodes[pos] = newNodeId;
      }

      /// <summary>
      /// Splits the way
      /// </summary>
      /// <param name="way"></param>
      /// <param name="nodeNo"></param>
      private string SplitWay(string wayId, IMWay way, int nodeNo)
      {
         string newWayId;
         IMWay newway = new IMWay(way);

         // Use this new node which has no references to other ways as the
         // terminus for the current way.
         way.Nodes.RemoveRange(nodeNo + 1, way.Nodes.Count - nodeNo - 1);
         DeadEnd(wayId, way);

         // Take the remainder of the way and create a new starting node.
         newWayId = NextWayId();
         newway.Nodes.RemoveRange(0, nodeNo);
         DeadStart(newWayId, newway);
         _ways.Add(newWayId, newway);

         return newWayId;
      }
   }
}
