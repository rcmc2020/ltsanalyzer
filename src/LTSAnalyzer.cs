﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace LTSAnalyzer
{
	class LTSAnalyzer
	{
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
		}
		
		/// <summary>
		/// Processes a meta node at the current read position in the reader.
		/// </summary>
		/// <param name="reader"></param>
		private void ProcessMeta(XmlReader reader) {
			if (reader.IsStartElement("meta"))
			{
				_osmBase = reader.GetAttribute("osm_base");
				if (reader.IsEmptyElement) 
				{
					reader.Read();
				}
				else {
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
		/// Processes a node at the current read position in the reader.
		/// </summary>
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
		/// </summary>
		/// <param name="i_reader"></param>
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
							way.Nodes.Add(reader.GetAttribute("ref"));
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
				_ways.Add(id, way);
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
			int level;
			foreach (KeyValuePair<string, Way> kv in _ways)
			{
				string id = kv.Key;
				Way way = kv.Value;
				level = 0;
				if (way.IsCyclable)
				{
					if (way.IsService)
					{
						level = 2;
					}
					else if (way.IsPath)
					{
						level = 1;
					}
					else if (way.Lanes > 2 || ((!way.IsResidential) && way.Lanes > 1 && way.IsOneWay))
					{
						if (way.MaxSpeed <= 40)
						{
							level = 3;
						}
						else
						{
							level = 4;
						}
					}
					else if (way.IsResidential)
					{
						level = 2;
					}
					else
					{
						if (way.MaxSpeed <= 40)
						{
							level = 2;
						}
						else
						{
							level = 3;
						}
					}
				}
				way.Level = level;
				// This marks all nodes in our file with being referenced in that level.
				if (level > 0)
				{
					foreach (string node in way.Nodes)
					{
						_nodes[node].SetLevelReference(level);
					}
				}
			}

		}

		/// <summary>
		/// Generates the output files.
		/// </summary>
		/// <param name="options"></param>
		public void CreateLevelFiles()
		{
			string path = _options.Directory;
			string rootname = _options.Output;
			XmlWriterSettings xs = new XmlWriterSettings();
			xs.Indent = true;
			for (int f = 1; f <= ElementBase._levels; f++)
			{
				string filename = Path.Combine(path, rootname + f.ToString() + ".osm");
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
							if (n.IsLevel(f))
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
							if (w.Level == f)
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