using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;

namespace LTSAnalyzer {
	class LTSAnalyzer {
		StressModel _stressModel;
		Dictionary<string, Node> _nodes;
		Dictionary<string, Way> _ways;
		Dictionary<string, Relation> _relations;
		HashSet<string> _usedNodes;
		Options _options;
		string _osmBase;
		string _minLat;
		string _minLon;
		string _maxLat;
		string _maxLon;

		public LTSAnalyzer(Options options) {
			_nodes = new Dictionary<string, Node>();
			_ways = new Dictionary<string, Way>();
			_relations = new Dictionary<string, Relation>();
			_options = options;
			_stressModel = new StressModel();
			_usedNodes = new HashSet<string>();
		}

		/// <summary>
		/// Processes the OSM file and loads the required elements.
		/// </summary>
		public void Load1() {
			Stopwatch sw = new Stopwatch();
			sw.Start();
			if (_options.Verbose) Console.WriteLine("Loading '" + _options.Filename + "'...");
			int notes = 0, meta = 0, bounds = 0, nodes = 0, ways = 0, relations = 0;
			string test;
			XmlReaderSettings readerSettings = new XmlReaderSettings();
			readerSettings.IgnoreWhitespace = true;
			using (XmlReader reader = XmlReader.Create(_options.Filename, readerSettings)) {
				reader.MoveToContent();
				if (reader.IsStartElement("osm")) {
					reader.Read();
					while (reader.IsStartElement()) {
						switch (reader.Name) {
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
								ProcessNodes1(reader);
								break;
							case "way":
								ways++;
								ProcessWays1(reader);
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
			if (_options.Timers) Console.WriteLine("Loading Pass 1 - Elapsed time: " + sw.Elapsed);
		}

		/// <summary>
		/// Processes the OSM file and loads the required elements.
		/// </summary>
		public void Load2() {
			Stopwatch sw = new Stopwatch();
			sw.Start();
			if (_options.Verbose) Console.WriteLine("Loading '" + _options.Filename + "'...");
			int notes = 0, meta = 0, bounds = 0, nodes = 0, ways = 0, relations = 0;
			string test;
			XmlReaderSettings readerSettings = new XmlReaderSettings();
			readerSettings.IgnoreWhitespace = true;
			using (XmlReader reader = XmlReader.Create(_options.Filename, readerSettings)) {
				reader.MoveToContent();
				if (reader.IsStartElement("osm")) {
					reader.Read();
					while (reader.IsStartElement()) {
						switch (reader.Name) {
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
								ProcessNodes2(reader);
								break;
							case "way":
								ways++;
								ProcessWays2(reader);
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
			if (_options.Timers) Console.WriteLine("Loading Pass 2 - Elapsed time: " + sw.Elapsed);
		}

		/// <summary>
		/// Processes a meta element at the current read position in the reader.
		/// Exits with the reader positioned at the next sibling element.
		/// </summary>
		/// <param name="reader"></param>
		private void ProcessMeta(XmlReader reader) {
			if (reader.IsStartElement("meta")) {
				_osmBase = reader.GetAttribute("osm_base");
				if (reader.IsEmptyElement) {
					reader.Read();
				}
				else {
					throw new Exception("Unexpected element: " + reader.ReadOuterXml());
				}
			}
			else {
				throw new Exception("Unexpected node: " + reader.Name);
			}
		}

		/// <summary>
		/// Processes a bounds element at the current read position in the reader.
		/// Exits with the reader positioned at the next sibling element.
		/// </summary>
		/// <param name="reader"></param>
		private void ProcessBounds(XmlReader reader) {
			if (reader.IsStartElement("bounds")) {
				_minLat = reader.GetAttribute("minlat");
				_minLon = reader.GetAttribute("minlon");
				_maxLat = reader.GetAttribute("maxlat");
				_maxLon = reader.GetAttribute("maxlon");
				if (reader.IsEmptyElement) {
					reader.Read();
				}
				else {
					throw new Exception("Unexpected element: " + reader.ReadOuterXml());
				}
			}
			else {
				throw new Exception("Unexpected node: " + reader.Name);
			}
		}

		/// <summary>
		/// Processes a node element at the current read position in the reader.
		/// Exits with the reader positioned at the next sibling element.
		/// </summary>
		/// <param name="i_reader"></param>
		private void ProcessNodes1(XmlReader reader) {
			if (reader.IsStartElement("node")) {
				if (reader.IsEmptyElement) {
					reader.Read();
				}
				else {
					while (reader.Read()) {
						if (reader.Name != "tag") {
							if (reader.NodeType == XmlNodeType.EndElement) {
								reader.Read();
								break;
							}
							else {
								throw new Exception("Unexpected element: " + reader.ReadOuterXml());
							}
						}
					}
				}
			}
			else {
				throw new Exception("Unexpected node: " + reader.Name);
			}
		}

		/// <summary>
		/// Processes a node element at the current read position in the reader.
		/// Exits with the reader positioned at the next sibling element.
		/// </summary>
		/// <param name="i_reader"></param>
		private void ProcessNodes2(XmlReader reader) {
			if (reader.IsStartElement("node")) {
				string id = reader.GetAttribute("id");
				string lat = reader.GetAttribute("lat");
				string lon = reader.GetAttribute("lon");
				Node node = new Node(lat, lon);
				if (reader.IsEmptyElement) {
					reader.Read();
				}
				else {
					while (reader.Read()) {
						if (reader.Name == "tag") {
							node.AddTag(reader);
						}
						else {
							if (reader.NodeType == XmlNodeType.EndElement) {
								reader.Read();
								break;
							}
							else {
								throw new Exception("Unexpected element: " + reader.ReadOuterXml());
							}
						}
					}
				}
				if (_usedNodes.Contains(id)) {
					_nodes.Add(id, node);
				}
			}
			else {
				throw new Exception("Unexpected node: " + reader.Name);
			}
		}

		/// <summary>
		/// Processes a way element at the current read position in the reader.
		/// Exits with the reader positioned at the next sibling element.
		/// </summary>
		/// <param name="i_reader">An XmlReader object with the read position on a way element.</param>
		private void ProcessWays1(XmlReader reader) {
			if (reader.IsStartElement("way")) {
				Way way = new Way();
				string id = reader.GetAttribute("id");
				if (reader.IsEmptyElement) {
					// Next node.
					reader.Read();
				}
				else {
					while (reader.Read()) {
						if (reader.Name == "tag") {
							way.AddTag(reader);
						}
						else if (reader.Name == "nd") {
							string nodeRef = reader.GetAttribute("ref");
							way.Nodes.Add(nodeRef);
						}
						else {
							if (reader.NodeType == XmlNodeType.EndElement) {
								reader.Read();
								break;
							}
							else {
								throw new Exception("Unexpected element: " + reader.ReadOuterXml());
							}

						}
					}
				}
				// This is a preliminary test to make sure we only add ways that are potentially valid
				// routes. This could be expanded to further filter the ways but this will typically 
				// be done in the analysis phase.
				if (StressModel.BikingPermitted(id, way)) {
					_ways.Add(id, way);
					foreach (string nodeRef in way.Nodes) {
						if (!_usedNodes.Contains(nodeRef)) {
							_usedNodes.Add(nodeRef);
						}
					}
				}
			}
			else {
				throw new Exception("Unexpected node: " + reader.Name);
			}
		}

		/// <summary>
		/// Processes a way element at the current read position in the reader.
		/// Exits with the reader positioned at the next sibling element.
		/// </summary>
		/// <param name="i_reader">An XmlReader object with the read position on a way element.</param>
		private void ProcessWays2(XmlReader reader) {
			if (reader.IsStartElement("way")) {
				if (reader.IsEmptyElement) {
					// Next node.
					reader.Read();
				}
				else {
					while (reader.Read()) {
						if (reader.Name == "tag") {
						}
						else if (reader.Name == "nd") {
						}
						else {
							if (reader.NodeType == XmlNodeType.EndElement) {
								reader.Read();
								break;
							}
							else {
								throw new Exception("Unexpected element: " + reader.ReadOuterXml());
							}

						}
					}
				}
			}
			else {
				throw new Exception("Unexpected node: " + reader.Name);
			}
		}

		/// <summary>
		/// Processes a relation element at the current read position in the reader.
		/// </summary>
		/// <param name="i_reader"></param>
		private void ProcessRelations(XmlReader i_reader) {
			if (i_reader.IsStartElement("relation")) {
				if (i_reader.IsEmptyElement) {
					i_reader.Read();
				}
				else {
					while (i_reader.Read()) {
						if (i_reader.Name == "tag") {
						}
						else if (i_reader.Name == "member") {
						}
						else {
							if (i_reader.NodeType == XmlNodeType.EndElement) {
								i_reader.Read();
								break;
							}
							else {
								throw new Exception("Unexpected element: " + i_reader.ReadOuterXml());
							}

						}
					}
				}
			}
			else {
				throw new Exception("Unexpected node: " + i_reader.Name);
			}
		}

		/// <summary>
		/// Classifies each way into its proper level and marks the nodes for that level.
		/// </summary>
		public void AnalyzeStressModel() {
			Stopwatch sw = new Stopwatch();
			sw.Start();
			_stressModel.Initialize(_ways, _nodes);
			if (_options.Verbose) Console.WriteLine("Running stress analysis...");
			_stressModel.RunAnalysis();
			if (_options.Timers) Console.WriteLine("Stress  - Elapsed time: " + sw.Elapsed);
		}

		/// <summary>
		/// Generates the output files based on the command line input options.
		/// </summary>
		public void CreateLevelFiles() {
			Stopwatch sw = new Stopwatch();
			sw.Start();
			if (_options.Verbose) Console.WriteLine("Generating output files...");
			switch (_options.OutputType) {
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
		private string FormatLatLong(string value) {
			return (value[value.Length - 1] == '0') ? double.Parse(value).ToString() : value;
		}

		/// <summary>
		/// Generates the GeoJSON output files.
		/// </summary>
		public void CreateLevelFilesGeoJson() {
			string path = _options.Directory;
			string prefix = _options.Prefix;
			for (int level = 1; level <= StressModel.LevelCount; level++) {
				string filename = Path.Combine(path, prefix + level.ToString() + ".json");
				if (File.Exists(filename)) {
					File.Delete(filename);
				}
				using (StreamWriter writer = new StreamWriter(filename, false)) {
					writer.Write("{\"type\":\"FeatureCollection\",\"features\":[");
					bool fsep = false;
					foreach (KeyValuePair<string, Way> kvway in _ways) {
						StringBuilder sb = new StringBuilder();
						Way w = kvway.Value;
						if (w.Level == level) {
							if (fsep) sb.Append(",");
							fsep = true;
							sb.Append("{\"type\":\"Feature\",\"id\":\"way/");
							sb.Append(kvway.Key);
							sb.Append("\",\"properties\":{\"id\":\"way/");
							sb.Append(kvway.Key);
							sb.Append("\"},\"geometry\":{\"type\":\"LineString\",\"coordinates\":[");
							bool csep = false;
							foreach (string nd in w.Nodes) {
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

		// <summary>
		/// Generates the OSM output files.
		/// </summary>
		public void CreateLevelFilesOSM() {
			string path = _options.Directory;
			string prefix = _options.Prefix;
			XmlWriterSettings xs = new XmlWriterSettings();
			xs.Indent = true;

			for (int level = 1; level <= StressModel.LevelCount; level++) {
				string filename = Path.Combine(path, prefix + level.ToString() + ".osm");
				if (File.Exists(filename)) {
					File.Delete(filename);
				}
				using (XmlWriter writer = XmlWriter.Create(filename, xs)) {
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

						foreach (KeyValuePair<string, Node> kvnode in _nodes) {
							Node n = kvnode.Value;
							if (n.IsLevel(level)) {
								writer.WriteStartElement("node");
								writer.WriteAttributeString("id", kvnode.Key);
								writer.WriteAttributeString("lat", n.Lat);
								writer.WriteAttributeString("lon", n.Lon);
								writer.WriteEndElement();
							}
						}
						foreach (KeyValuePair<string, Way> kvway in _ways) {
							Way w = kvway.Value;
							if (w.Level == level) {
								writer.WriteStartElement("way");
								writer.WriteAttributeString("id", kvway.Key);
								{
									foreach (string nd in w.Nodes) {
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
