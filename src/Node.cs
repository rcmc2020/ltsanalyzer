using System;
using System.Collections.Generic;
using System.Text;

namespace LTSAnalyzer
{
	class Node : ElementBase
	{
		private string _lat;

		public string Lat
		{
			get { return _lat; }
			set { _lat = value; }
		}
		private string _lon;

		public string Lon
		{
			get { return _lon; }
			set { _lon = value; }
		}
		public Node(string lat, string lon)
		{
			_lat = lat;
			_lon = lon;
		}
	}
}
