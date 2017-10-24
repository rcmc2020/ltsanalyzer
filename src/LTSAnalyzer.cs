using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;

namespace LTSAnalyzer
{
   class LTSAnalyzer
   {
      AnalysisModel _model;
      Dictionary<string, Node> _nodes;
      Dictionary<string, Way> _ways;
      Dictionary<string, Relation> _relations;
      Options _options;
      string _osmBase;
      string _minLat;
      string _minLon;
      string _maxLat;
      string _maxLon;

      public LTSAnalyzer(Options options)
      {
         _model = new AnalysisModel();
         _model.Initialize();
         _nodes = new Dictionary<string, Node>();
         _ways = new Dictionary<string, Way>();
         _relations = new Dictionary<string, Relation>();
         _options = options;
      }

      /// <summary>
      /// Processes the OSM file and loads the required elements.
      /// </summary>
      public void Load()
      {
         Stopwatch sw = new Stopwatch();
         sw.Start();
         if (_options.Verbose) Console.WriteLine("Loading '" + _options.Filename + "'...");
         int notes = 0, meta = 0, bounds = 0, nodes = 0, ways = 0, relations = 0;
         string test;
         XmlReaderSettings readerSettings = new XmlReaderSettings();
         readerSettings.IgnoreWhitespace = true;
         using (XmlReader reader = XmlReader.Create(_options.Filename, readerSettings))
         {
            reader.MoveToContent();
            if (reader.IsStartElement("osm"))
            {
               reader.Read();
               while (reader.IsStartElement())
               {
                  switch (reader.Name)
                  {
                     case "note":
                        notes++;
                        test = (string)reader.ReadElementContentAs(typeof(System.String), null);
                        break;
                     case "meta":
                        meta++;
                        ProcessMeta(reader);
                        break;
                     case "bounds":
                        bounds++;
                        ProcessBounds(reader);
                        break;
                     case "node":
                        nodes++;
                        ProcessNodes(reader);
                        break;
                     case "way":
                        ways++;
                        ProcessWays(reader);
                        break;
                     case "relation":
                        relations++;
                        ProcessRelations(reader);
                        break;
                     default:
                        throw new Exception("Unknown element type: " + reader.Name);
                  }
               }
            }
         }
         CleanupNodes();
         if (_options.Timers) Console.WriteLine("Loading - Elapsed time: " + sw.Elapsed);
      }

      /// <summary>
      /// Cleanup the node list to save memory.
      /// </summary>
      private void CleanupNodes()
      {
         HashSet<string> deletionList = new HashSet<string>();
         foreach (KeyValuePair<string, Node> kv in _nodes)
         {
            if (!kv.Value.IsReferenced)
            {
               deletionList.Add(kv.Key);
            }
         }
         foreach (string nodeId in deletionList)
         {
            _nodes.Remove(nodeId);
         }
      }

      /// <summary>
      /// Processes a meta element at the current read position in the reader.
      /// Exits with the reader positioned at the next sibling element.
      /// </summary>
      /// <param name="reader"></param>
      private void ProcessMeta(XmlReader reader)
      {
         if (reader.IsStartElement("meta"))
         {
            _osmBase = reader.GetAttribute("osm_base");
            if (reader.IsEmptyElement)
            {
               reader.Read();
            }
            else
            {
               throw new Exception("Unexpected element: " + reader.ReadOuterXml());
            }
         }
         else
         {
            throw new Exception("Unexpected node: " + reader.Name);
         }
      }

      /// <summary>
      /// Processes a bounds element at the current read position in the reader.
      /// Exits with the reader positioned at the next sibling element.
      /// </summary>
      /// <param name="reader"></param>
      private void ProcessBounds(XmlReader reader)
      {
         if (reader.IsStartElement("bounds"))
         {
            _minLat = reader.GetAttribute("minlat");
            _minLon = reader.GetAttribute("minlon");
            _maxLat = reader.GetAttribute("maxlat");
            _maxLon = reader.GetAttribute("maxlon");
            if (reader.IsEmptyElement)
            {
               reader.Read();
            }
            else
            {
               throw new Exception("Unexpected element: " + reader.ReadOuterXml());
            }
         }
         else
         {
            throw new Exception("Unexpected node: " + reader.Name);
         }
      }

      /// <summary>
      /// Processes a node element at the current read position in the reader.
      /// Exits with the reader positioned at the next sibling element.
      /// <param name="i_reader"></param>
      private void ProcessNodes(XmlReader reader)
      {
         if (reader.IsStartElement("node"))
         {
            string id = reader.GetAttribute("id");
            string lat = reader.GetAttribute("lat");
            string lon = reader.GetAttribute("lon");
            Node node = new Node(lat, lon);
            if (reader.IsEmptyElement)
            {
               reader.Read();
            }
            else
            {
               while (reader.Read())
               {
                  if (reader.Name == "tag")
                  {
                     node.AddTag(reader);
                  }
                  else
                  {
                     if (reader.NodeType == XmlNodeType.EndElement)
                     {
                        reader.Read();
                        break;
                     }
                     else
                     {
                        throw new Exception("Unexpected element: " + reader.ReadOuterXml());
                     }
                  }
               }
            }
            _nodes.Add(id, node);
         }
         else
         {
            throw new Exception("Unexpected node: " + reader.Name);
         }
      }

      /// <summary>
      /// Processes a way element at the current read position in the reader.
      /// Exits with the reader positioned at the next sibling element.
      /// </summary>
      /// <param name="i_reader">An XmlReader object with the read position on a way element.</param>
      private void ProcessWays(XmlReader reader)
      {
         if (reader.IsStartElement("way"))
         {
            Way way = new Way();
            string id = reader.GetAttribute("id");
            if (reader.IsEmptyElement)
            {
               // Next node.
               reader.Read();
            }
            else
            {
               while (reader.Read())
               {
                  if (reader.Name == "tag")
                  {
                     way.AddTag(reader);
                  }
                  else if (reader.Name == "nd")
                  {
                     string nodeRef = reader.GetAttribute("ref");
                     way.Nodes.Add(nodeRef);
                  }
                  else
                  {
                     if (reader.NodeType == XmlNodeType.EndElement)
                     {
                        reader.Read();
                        break;
                     }
                     else
                     {
                        throw new Exception("Unexpected element: " + reader.ReadOuterXml());
                     }

                  }
               }
            }
            // This is a preliminary test to make sure we only add ways that are potentially valid
            // routes. This could be expanded to further filter the ways but this will typically 
            // be done in the analysis phase.
            if (way.Tags.ContainsKey("highway")) 
            {
               _ways.Add(id, way);
               foreach (string nodeRef in way.Nodes) 
               {
                  _nodes[nodeRef].AddWay(id);
               }
            }
         }
         else
         {
            throw new Exception("Unexpected node: " + reader.Name);
         }
      }

      /// <summary>
      /// Processes a relation element at the current read position in the reader.
      /// </summary>
      /// <param name="i_reader"></param>
      private void ProcessRelations(XmlReader i_reader)
      {
         string type, nref, role;
         if (i_reader.IsStartElement("relation"))
         {
            Relation relation = new Relation();
            string id = i_reader.GetAttribute("id");
            if (i_reader.IsEmptyElement)
            {
               // Next relation.
               i_reader.Read();
            }
            else
            {
               while (i_reader.Read())
               {
                  if (i_reader.Name == "tag")
                  {
                     relation.AddTag(i_reader);
                  }
                  else if (i_reader.Name == "member")
                  {
                     type = i_reader.GetAttribute("type");
                     nref = i_reader.GetAttribute("ref");
                     role = i_reader.GetAttribute("role");
                     relation.Members.Add(new Member(type, nref, role));
                  }
                  else
                  {
                     if (i_reader.NodeType == XmlNodeType.EndElement)
                     {
                        i_reader.Read();
                        break;
                     }
                     else
                     {
                        throw new Exception("Unexpected element: " + i_reader.ReadOuterXml());
                     }

                  }
               }
            }
            _relations.Add(id, relation);
         }
         else
         {
            throw new Exception("Unexpected node: " + i_reader.Name);
         }
      }

      /// <summary>
      /// Classifies each way into its proper level and marks the nodes for that level.
      /// </summary>
      public void Analyze()
      {
         Stopwatch sw = new Stopwatch();
         sw.Start();
         if (_options.Verbose) Console.WriteLine("Running analysis...");
         _model.RunAnalysis(_ways, _nodes);
         if (_options.Timers) Console.WriteLine("Analyze - Elapsed time: " + sw.Elapsed);
      }

      /// <summary>
      /// Generates the output files based on the command line input options.
      /// </summary>
      public void CreateLevelFiles()
      {
         Stopwatch sw = new Stopwatch();
         sw.Start();
         if (_options.Verbose) Console.WriteLine("Generating output files...");
         switch (_options.OutputType)
         {
            case OutputType.OSM: CreateLevelFilesOSM(); break;
            case OutputType.GeoJSON: CreateLevelFilesGeoJson(); break;
            default:
               throw new Exception("Error: Invalid OutputType.");
         }
         if (_options.Timers) Console.WriteLine("FileGen - Elapsed time: " + sw.Elapsed);
      }

      /// <summary>
      /// Strip trailing zeroes from string if it's a decimal value.
      /// </summary>
      /// <param name="value">A numeric string</param>
      /// <returns>A numeric string with trailing zeroes removed.</returns>
      private string FormatLatLong(string value)
      {
         return (value[value.Length - 1] == '0') ? double.Parse(value).ToString() : value;
      }

      /// <summary>
      /// Generates the GeoJSON output files.
      /// </summary>
      public void CreateLevelFilesGeoJson()
      {
         string path = _options.Directory;
         string prefix = _options.Prefix;
         for (int level = 1; level <= AnalysisModel.LevelCount; level++)
         {
            string filename = Path.Combine(path, prefix + level.ToString() + ".json");
            if (File.Exists(filename))
            {
               File.Delete(filename);
            }
            using (StreamWriter writer = new StreamWriter(filename, false))
            {
               writer.Write("{\"type\":\"FeatureCollection\",\"features\":[");
               bool fsep = false;
               foreach (KeyValuePair<string, Way> kvway in _ways)
               {
                  StringBuilder sb = new StringBuilder();
                  Way w = kvway.Value;
                  if (w.Level == level)
                  {
                     if (fsep) sb.Append(",");
                     fsep = true;
                     sb.Append("{\"type\":\"Feature\",\"id\":\"way/");
                     sb.Append(kvway.Key);
                     sb.Append("\",\"properties\":{\"id\":\"way/");
                     sb.Append(kvway.Key);
                     sb.Append("\"},\"geometry\":{\"type\":\"LineString\",\"coordinates\":[");
                     bool csep = false;
                     foreach (string nd in w.Nodes)
                     {
                        Node node = _nodes[nd];
                        if (csep) sb.Append(",");
                        csep = true;
                        sb.Append("[");
                        sb.Append(FormatLatLong(node.Lon));
                        sb.Append(",");
                        sb.Append(FormatLatLong(node.Lat));
                        sb.Append("]");
                     }
                     sb.Append("]}}");
                  }
                  writer.Write(sb.ToString());
               }
               writer.Write("]}");
            }
         }
      }

      /// <summary>
      /// Generates the OSM output files.
      /// </summary>
      public void CreateLevelFilesOSM()
      {
         string path = _options.Directory;
         string prefix = _options.Prefix;
         XmlWriterSettings xs = new XmlWriterSettings();
         xs.Indent = true;
         for (int level = 1; level <= AnalysisModel.LevelCount; level++)
         {
            string filename = Path.Combine(path, prefix + level.ToString() + ".osm");
            if (File.Exists(filename))
            {
               File.Delete(filename);
            }
            using (XmlWriter writer = XmlWriter.Create(filename, xs))
            {
               writer.WriteStartDocument();
               writer.WriteStartElement("osm");
               writer.WriteAttributeString("version", "0.6");
               writer.WriteAttributeString("generator", "Bike Ottawa");
               {
                  writer.WriteStartElement("note");
                  writer.WriteString("The data included in this document originated from www.openstreetmap.org and was converted by Bike Ottawa. The data is made available under ODbL.");
                  writer.WriteEndElement();

                  writer.WriteStartElement("meta");
                  writer.WriteAttributeString("osm_base", _osmBase);
                  writer.WriteEndElement();

                  writer.WriteStartElement("bounds");
                  writer.WriteAttributeString("minlat", _minLat);
                  writer.WriteAttributeString("minlon", _minLon);
                  writer.WriteAttributeString("maxlat", _maxLat);
                  writer.WriteAttributeString("maxlon", _maxLon);
                  writer.WriteEndElement();

                  foreach (KeyValuePair<string, Node> kvnode in _nodes)
                  {
                     Node n = kvnode.Value;
                     if (n.IsLevel(level))
                     {
                        writer.WriteStartElement("node");
                        writer.WriteAttributeString("id", kvnode.Key);
                        writer.WriteAttributeString("lat", n.Lat);
                        writer.WriteAttributeString("lon", n.Lon);
                        writer.WriteEndElement();
                     }
                  }
                  foreach (KeyValuePair<string, Way> kvway in _ways)
                  {
                     Way w = kvway.Value;
                     if (w.Level == level)
                     {
                        writer.WriteStartElement("way");
                        writer.WriteAttributeString("id", kvway.Key);
                        {
                           foreach (string nd in w.Nodes)
                           {
                              writer.WriteStartElement("nd");
                              writer.WriteAttributeString("ref", nd);
                              writer.WriteEndElement();
                           }
                        }
                        writer.WriteEndElement();
                     }
                  }
               }
               writer.WriteEndElement();
               writer.WriteEndDocument();
            }
         }
      }
   }
}
