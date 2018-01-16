using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LTSAnalyzer {

	class StressModel {

		Dictionary<string, Way> _ways;

		Dictionary<string, Node> _nodes;

		/// <summary>
		/// This is used for testing.
		/// </summary>
		Dictionary<string, int> _tagCollection;

		/// <summary>
		/// The number of levels in the analysis. Currently this value is fixed but 
		/// in future versions, it may be loaded with the analysis definition.
		/// </summary>
		static int _levels;

		static StressModel() {
			_levels = -1;
		}

		public StressModel() { }

		public void Initialize(Dictionary<string, Way> ways, Dictionary<string, Node> nodes) {
			// If we load the analysis model from a definition file, we 
			// can initialize the number of levels value here.
			_nodes = nodes;
			_ways = ways;
			_levels = 4;
			_tagCollection = new Dictionary<string, int>();

		}

		public static int LevelCount {
			get {
				if (_levels == -1) {
					throw new Exception("AnalysisModel.Initialize not called.");
				}
				return _levels;
			}
		}

		/// <summary>
		/// Returns the number of lanes.
		/// </summary>
		private int Lanes(Way way) {
			if (way.Tags.ContainsKey("lanes")) {
				string l = way.Tags["lanes"];
				if (l.Contains(";")) {
					string[] list = l.Split(';');
					int lmax = 1;
					foreach (string s in list) {
						if (int.Parse(s) > lmax) {
							lmax = int.Parse(s);
						}
					}
					return lmax;
				}
				else {
					return int.Parse(way.Tags["lanes"]);
				}
			}
			else {
				return 2;
			}
		}

		/// <summary>
		/// Calculates the maximum speed for this way.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="way"></param>
		private int MaxSpeed(Way way) {
			if (way.Tags.ContainsKey("maxspeed")) {
				int result;
				string maxspeed = way.Tags["maxspeed"];
				if (maxspeed == "national") {
					return 40;
				}
				else if (int.TryParse(maxspeed, out result)) {
					return result;
				}
				else {
					throw new Exception("Error: Unknown maxspeed value '" + maxspeed + "'.");
				}
			}
			else {
				if (way.HasTag("highway", "motorway")) {
					return 100;
				}
				else {
					return 50;
				}
			}
		}

		/// <summary>
		/// Determines if the way has parking on it.
		/// </summary>
		/// <param name="way"></param>
		/// <returns></returns>
		private bool ParkingPresent(string id, Way way) {
			if (way.HasTag("parking")) {
				string newtag = "parking=" + way.Tags["parking"];
				if (!_tagCollection.ContainsKey(newtag)) {
					_tagCollection.Add(newtag, 1);
				}
				else {
					_tagCollection[newtag]++;
				}
				switch (way.Tags["parking"]) {
					case "yes": return true;
					case "no": return false;
					default:
						break;
				}
			}
			if (way.TagStartsWith("parking:")) {
				foreach (KeyValuePair<string, string> tag in way.Tags) {
					string k = tag.Key;
					if (k.StartsWith("parking:")) {
						string v = tag.Value;
						switch (tag.Key) {
							case "parking:condition:both:default":
							case "parking:condition:both:maxstay":
							case "parking:condition:both:time_interval":
							case "parking:condition:left":
							case "parking:condition:left:time_interval":
							case "parking:condition:right":
							case "parking:condition:right:maxstay":
							case "parking:condition:right:time_interval":
							case "parking:lane":
							case "parking:lane:both":
							case "parking:lane:left":
							case "parking:lane:right":
							default:
								string newtag = k + "=" + v;
								if (!_tagCollection.ContainsKey(newtag)) {
									_tagCollection.Add(newtag, 1);
								}
								else {
									_tagCollection[newtag]++;
								}
								break;
						}
					}
				}
			}
			return false;
		}

		public void RunAnalysis() {
			int level;
			foreach (KeyValuePair<string, Way> kv in _ways) {
				string id = kv.Key;
				Way way = kv.Value;

				way.MaxSpeed = MaxSpeed(way);
				way.Lanes = Lanes(way);
				way.IsParkingPresent = ParkingPresent(id, way);

				level = EvaluateWay(id, way);
				way.Level = level;
				// This marks all nodes in our file with being referenced in that level.
				if (way.Level > 0) {
					foreach (string node in way.Nodes) {
						_nodes[node].SetLevelReference(level);
					}
				}
			}
			if (_tagCollection.Count > 0) {
				// FIXME: This collection is used during testing to gather information on tags.
				// It should be removed before final deployment.
				using (StreamWriter writer = new StreamWriter(@"tags.txt", false)) {
					foreach (KeyValuePair<string, int> kv in _tagCollection) {
						writer.Write(kv.Key);
						writer.Write(" : ");
						writer.Write(kv.Value.ToString());
						writer.Write("\r\n");
					}
				}
			}
		}

		public bool HasSeparatingMedian(string id, Way way) {
			return false;
		}

		public int EvaluateWay(string id, Way way) {
			if (BikingPermitted(id, way)) {
				if (IsSeparatedPath(id, way)) {
					return 1;
				}
				else if (IsBikeLane(id, way)) {
					return BikeLaneAnalysis(id, way);
				}
				else if (IsMixedTraffic(id, way)) {
					return MixedTrafficAnalysis(id, way);
				}
				else {
					throw new NotImplementedException("What is here?");
				}
			}

			return 0;
		}

		public int MixedTrafficAnalysis(string id, Way way) {
			int maxSpeed = MaxSpeed(way);
			int lanes = Lanes(way);
			bool markedCentreLine = MarkedCentreLine(way);
			bool isResidential = IsResidential(way);

			if (maxSpeed <= 40) {
				if ((lanes <= 3 && !markedCentreLine) || (lanes < 3 && isResidential)) {
					return 1;
				}
				else if (lanes <= 3) {
					return 2;
				}
				else if (lanes <= 5) {
					return 3;
				}
				return 4;
			}
			else if (maxSpeed <= 50) {
				if (lanes <= 3) {
					if (!markedCentreLine || (lanes < 3 && isResidential)) {
						return 2;
					}
					return 3;
				}
				else {
					return 4;
				}
			}
			else {
				return 4;
			}
		}

		public int BikeLaneAnalysis(string id, Way way) {
			if (ParkingPresent(id, way)) {
				return BikeLaneAnalysisParkingPresent(id, way);
			}
			else {
				return BikeLaneAnalysisNoParking(id, way);
			}
		}

		public int BikeLaneAnalysisParkingPresent(string id, Way way) {
			int lts = 0;
			int lanes = Lanes(way);
			int maxSpeed = MaxSpeed(way);
			double width = BikeAndParkingWidth(way);
			int blockageLTS = BikeLaneBlockageLTS(way);
			bool isResidential = IsResidential(way);

			if (lanes <= 1) {
				lts = Math.Max(lts, 1);
			}
			else if (lanes >= 2) {
				lts = Math.Max(lts, 3);
			}

			if (width <= 4.1) {
				lts = Math.Max(lts, 3);
			}
			else if (width <= 4.25 || (width < 4.5 && (maxSpeed < 40 || isResidential))) {
				lts = Math.Max(lts, 2);
			}
			else {
				lts = Math.Max(lts, 1);
			}

			if (maxSpeed <= 40) {
				lts = Math.Max(lts, 1);
			}
			else if (maxSpeed <= 50) {
				lts = Math.Max(lts, 2);
			}
			else if (maxSpeed <= 55) {
				lts = Math.Max(lts, 3);
			}
			else {
				lts = Math.Max(lts, 4);
			}

			lts = Math.Max(lts, BikeLaneBlockageLTS(way));

			return lts;
		}

		public int BikeLaneAnalysisNoParking(string id, Way way) {
			int lts = 0;
			int lanes = Lanes(way);
			int maxSpeed = MaxSpeed(way);
			double width = BikeAndParkingWidth(way);
			int blockageLTS = BikeLaneBlockageLTS(way);
			bool isResidential = IsResidential(way);

			if (lanes <= 1) {
				lts = Math.Max(lts, 1);
			}
			else if (lanes == 2 && HasSeparatingMedian(id, way)) {
				lts = Math.Max(lts, 2);
			}
			else if (lanes >= 2) {
				lts = Math.Max(lts, 3);
			}

			return lts;
		}

		private bool BikingPermitted(string id, Way way) {
			if (way.HasTag("highway") || way.HasTag("bicycle")) {
				if (way.HasTag("bicycle", "no")) {
					return false;
				}
				if (way.HasTag("highway", "motorway") || way.HasTag("highway", "motorway_link")) {
					return false;
				}
				if (way.HasTag("highway", "service")) {
					if (way.HasTag("service", "driveway") || way.HasTag("service", "parking_aisle")) {
						return false;
					}
				}
				if (way.HasTag("footway", "sidewalk")) {
					if (!way.HasTag("bicycle", "yes")) {
						if (way.HasTag("highway", "footway") || way.HasTag("highway", "path")) {
							return false;
						}
					}
				}
			}
			else {
				return false;
			}

			return true;
		}

		private bool IsSeparatedPath(string id, Way way) {
			if (way.HasTag("highway", "path") || way.HasTag("highway", "footway") || way.HasTag("highway", "cycleway")) {
				return true;
			}
			// FIXME: This doesn't seem to be covered by the Ottawa OSM guide. E.g. Laurier.
			if (way.HasTag("cycleway", "track")) {
				return true;
			}

			return false;
		}

		private bool IsBikeLane(string id, Way way) {
			if (!IsSeparatedPath(id, way)) {
				if (way.HasTag("cycleway", "lane") || way.HasTag("cycleway:right", "lane") || way.HasTag("cycleway:middle", "lane") || way.HasTag("cycleway", "opposite_lane")) {
					return true;
				}
				if (way.TagStartsWith("shoulder")) {
					return true;
				}
			}

			return false;
		}

		private bool IsMixedTraffic(string id, Way way) {
			if (BikingPermitted(id, way)) {
				if (!IsSeparatedPath(id, way)) {
					if (!IsBikeLane(id, way)) {
						return true;
					}
				}
			}
			return false;
		}

		private bool IsResidential(Way way) {
			return way.HasTag("highway", "residential");
		}

		private int BikeLaneBlockageLTS(Way way) {
			// FIXME: This should return 1 if rare or 3 if frequent. It may
			// only apply in commercial areas. See Mineta Documentation.
			return 1;
		}

		private double BikeAndParkingWidth(Way way) {
			// FIXME: This is the sum of bike and parking lane width. It includes
			// marked buffer and paved gutter. We currently can't count it so we
			// just assume the maximum to remove the effect from the calculation.
			return double.MaxValue;
		}

		private bool MarkedCentreLine(Way way) {
			// FIXME: We don't have this information.
			return true;
		}
	}
}