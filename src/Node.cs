using System;
using System.Collections.Generic;
using System.Text;

namespace LTSAnalyzer {
	public class Node : ElementBase {
		bool[] _levelReference;

		public string Lat { get; set; }

		public string Lon { get; set; }

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
		/// Strip trailing zeroes from string if it's a decimal value.
		/// </summary>
		/// <param name="value">A numeric string</param>
		/// <returns>A numeric string with trailing zeroes removed.</returns>
		public override string ToString() {
			return "[" + ((Lon[Lon.Length - 1] == '0') ? double.Parse(Lon).ToString() : Lon) + "," + ((Lat[Lat.Length - 1] == '0') ? double.Parse(Lat).ToString() : Lat) + "]";
		}
	}
}
