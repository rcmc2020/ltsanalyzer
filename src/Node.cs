using System;
using System.Collections.Generic;
using System.Text;

namespace LTSAnalyzer {
	public class Node : ElementBase {
		bool[] _levelReference;

		public string Lat { get; set; }

		public string Lon { get; set; }

		/// <summary>
		/// A list of all the ways to which this node belongs.
		/// </summary>
		List<string> _ways;

		/// <summary>
		/// This is a list of the id of ways that reference this node.
		/// </summary>
		public List<string> Ways {
			get {
				return _ways;
			}
		}

		public Node(string lat, string lon) {
			Lat = lat;
			Lon = lon;
		}

		/// <summary>
		/// Indicates whether the element belongs to the specified level .
		/// </summary>
		/// <param name="level">A value from 1 to 4 indicating the level.</param>
		/// <returns>True if the element belongs to the specified level, else False.</returns>
		public bool IsLevel(int level) {
			return (_levelReference == null) ? false : _levelReference[level - 1];
		}

		/// <summary>
		/// Marks this element as belonging to the specified level.
		/// </summary>
		/// <param name="level">A value from 1 to 4 indicating the level.</param>
		public void SetLevelReference(int level) {
			if (_levelReference == null) {
				_levelReference = new bool[StressModel.LevelCount];
			}
			_levelReference[level - 1] = true;
		}

		/// <summary>
		/// Add a reference to a new way to the list of ways.
		/// </summary>
		/// <param name="wayId"></param>
		public void AddWay(string wayId) {
			if (_ways == null) {
				_ways = new List<string>();
			}
			if (!_ways.Contains(wayId)) {
				_ways.Add(wayId);
			}
		}

		/// <summary>
		/// Returns True if the node has been referenced in a way.
		/// </summary>
		public bool IsReferenced {
			get {
				return _ways != null;
			}
		}

		/// <summary>
		/// Strip trailing zeroes from string if it's a decimal value.
		/// </summary>
		/// <param name="value">A numeric string</param>
		/// <returns>A numeric string with trailing zeroes removed.</returns>
		public override string ToString() {
			return "[" + ((Lon[Lon.Length - 1] == '0') ? double.Parse(Lon).ToString() : Lon) + "," + ((Lat[Lat.Length - 1] == '0') ? double.Parse(Lat).ToString() : Lat) + "]";
		}
	}
}
