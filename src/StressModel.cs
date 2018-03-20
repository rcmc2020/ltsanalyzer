using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LTSAnalyzer {

	class StressModel {

		Dictionary<string, Way> _ways;

		Dictionary<string, Node> _nodes;

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
			int result = 2;
			if (way.Tags.ContainsKey("lanes")) {
				string l = way.Tags["lanes"];
				if (l.Contains(";")) {
					string[] list = l.Split(';');
					int lmax = 2;
					foreach (string s in list) {
						if (int.TryParse(s, out result)) {
							if (result > lmax) {
								lmax = result;
							}
						}
						else {
							int debug = 1;
						}
					}
					result = lmax;
				}
				else {
					if (int.TryParse(way.Tags["lanes"], out result)) {
						return result;
					}
					else {
						result = 2;
					}
				}
			}
			return result;
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
			if (way.HasTag("parking", "yes")) {
				return true;
			}
			if (way.TagStartsWith("parking:")) {
				foreach (KeyValuePair<string, string> tag in way.Tags) {
					string k = tag.Key;
					if (k.StartsWith("parking:lane:")) {
						string v = tag.Value;
						if (v == "parallel" || v == "perpendicular" || v == "diagonal" || v == "yes" || v == "marked") {
							return true;
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

				level = EvaluateWay(id, way);
				way.Level = level;
				// This marks the used nodes in our file with being referenced in that level.
				// We use this to divide the file up for OSM output.
				if (way.Level > 0) {
					foreach (string node in way.Nodes) {
						_nodes[node].SetLevelReference(level);
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
			int lanes;
			int maxSpeed = MaxSpeed(way);

			if (maxSpeed <= 40) {
				lanes = Lanes(way);
				if (lanes <= 3) {
					return 2;
				}
				else if (lanes <= 5) {
					return 3;
				}
			}
			else if (maxSpeed <= 50) {
				if (way.HasTag("service", "parking_aisle")) {
					return 2;
				}
				lanes = Lanes(way);
				if (lanes < 3 && IsResidential(way)) {
					return 2;
				}
				else if (lanes <= 3) {
					return 3;
				}
			}
			return 4;
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

		public static bool BikingPermitted(string id, Way way) {
			if (way.HasTag("highway") || way.HasTag("bicycle")) {
				if (way.HasTag("bicycle", "no")) {
					return false;
				}
				if (way.HasTag("highway", "motorway") || way.HasTag("highway", "motorway_link")) {
					return false;
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
			if (way.HasTag("highway", "construction")) {
				if (way.HasTag("construction", "path") || way.HasTag("construction", "footway") || way.HasTag("construction", "cycleway")) {
					return true;
				}
			}
			// FIXME: This doesn't seem to be covered by the Ottawa OSM guide. E.g. Laurier.
			if (way.TagStartsWith("cycleway", "track") || way.TagStartsWith("cycleway", "opposite_track")) {
				return true;
			}

			return false;
		}

		private bool IsBikeLane(string id, Way way) {
			if (!IsSeparatedPath(id, way)) {
				if (way.TagStartsWith("cycleway", "crossing")
					|| way.TagStartsWith("cycleway", "lane")
					|| way.TagStartsWith("cycleway", "left")
					|| way.TagStartsWith("cycleway", "opposite")
					|| way.TagStartsWith("cycleway", "opposite_lane")
					|| way.TagStartsWith("cycleway", "right")
					|| way.TagStartsWith("cycleway", "yes")
				) {
					return true;
				}
				if (way.HasTag("shoulder:access:bicycle", "yes")) {
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