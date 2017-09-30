using System;
using System.Collections.Generic;
using System.Text;

namespace LTSAnalyzer
{
	class Way : ElementBase
	{
		/// <summary>
		/// A list of all nodes associated with this way.
		/// </summary>
		List<string> _nodes;

		/// <summary>
		/// A way is associated with a single level.
		/// </summary>
		public int Level { get; set; }

		/// <summary>
		/// A quick list to quickly elimate specified ways before the analysis phase.
		/// </summary>
		static List<string> _ignoreKeyList;

		static Way()
		{
			_ignoreKeyList = new List<string>() { 
				"addr:housenumber", 
				"addr:interpolation", 
				"aeroway", 
				"amenity", 
				"barrier", 
				"boundary", 
				"building", 
				"building:levels", 
				"building:part", 
				"disused:amenity", 
				"indoor", 
				"landcover", 
				"landuse", 
				"leisure", 
				"man_made", 
				"indoor", 
				"natural", 
				"piste:type", 
				"place", 
				"power", 
				"public_transport", 
				"railway", 
				"seamark:type", 
				"shop", 
				"waterway" };
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

		/// <summary>
		/// Returns True if the way is cyclable.
		/// </summary>
		public bool IsCyclable
		{
			get
			{
				bool result = false;
				if (this.Tags.Count == 0)
				{
					return false;
				}
				else if (_tags.ContainsKey("highway"))
				{
					if (HasTag("bicycle", "no"))
					{
						return false;
					}
					else if (HasTag("highway", "motorway") || HasTag("highway", "motorway_link"))
					{
						if (HasTag("bicycle", "yes"))
						{
							return true;
						}
						else
						{
							return false;
						}
					}
					else if (_tags["highway"] == "service" && (HasTag("service", "parking_aisle") || HasTag("service", "driveway")))
					{
						return false;
					}
					return true;
				}
				else if (_tags.ContainsKey("bicycle"))
				{
					if (_tags["bicycle"] == "yes")
					{
						if (HasTag("piste:type", "nordic"))
						{
							return false;
						}
						// FIXME: The following statements are incorrect.
						else if (HasTag("crossing", "uncontrolled"))
						{
							return true;
						}
						else
						{
							return true;
						}
					}
					else
					{
						// FIXME: This isn't correct.
						if (HasTag("barrier", "gate"))
						{
							return false;
						}
						else
						{
							return false;
						}
					}
				}
				else
				{
					if (_tags.Count == 1)
					{
						if (_tags.ContainsKey("note") || _tags.ContainsKey("level") || _tags.ContainsKey("layer"))
						{
							return false;
						}
					}
					foreach (string s in _ignoreKeyList)
					{
						if (_tags.ContainsKey(s)) return false;
					}
				}
				return result;
			}
		}

		/// <summary>
		/// Returns the number of lanes in defined for the way if possible, else -1.
		/// </summary>
		public int Lanes
		{
			get
			{
				if (_tags.ContainsKey("lanes"))
				{
					string l = _tags["lanes"];
					if (l.Contains(";"))
					{
						string[] list = l.Split(';');
						int lmax = 1;
						foreach (string s in list)
						{
							if (int.Parse(s) > lmax)
							{
								lmax = int.Parse(s);
							}
						}
						return lmax;
					}
					else
					{
						return int.Parse(_tags["lanes"]);
					}
				}
				else return -1;
			}
		}

		/// <summary>
		/// Returns True if the way is a cyclable path.
		/// </summary>
		public bool IsPath
		{
			get
			{
				if (_tags.ContainsKey("highway"))
				{
					if (HasTag("highway", "cycleway") || HasTag("highway", "footway"))
					{
						return true;
					}
					else if (HasTag("highway", "path"))
					{
						if (HasTag("bicycle", "no"))
						{
							return false;
						}
						else if (_tags.ContainsKey("cycleway"))
						{
							return true;
						}
						else
						{
							return true;
						}
					}
				}
				return false;
			}
		}

		/// <summary>
		/// Returns the maximum speed of the way.
		/// </summary>
		public int MaxSpeed
		{
			get
			{
				if (_tags.ContainsKey("maxspeed"))
				{
					int result;
					string maxspeed = _tags["maxspeed"];
					if (maxspeed == "national")
					{
						return 40;
					}
					else if (int.TryParse(maxspeed, out result))
					{
						return result;
					}
				}
				else
				{
					if (HasTag("highway", "motorway"))
					{
						return 100;
					}
				}
				return 50;
			}
		}

		/// <summary>
		/// Return True if the way is a residential road.
		/// </summary>
		public bool IsResidential
		{
			get
			{
				return HasTag("highway", "residential");
			}
		}

		/// <summary>
		/// Returns True if the way is one way.
		/// </summary>
		public bool IsOneWay
		{
			get
			{
				return HasTag("oneway", "yes");
			}
		}

		/// <summary>
		/// Returns True if the way is a service road.
		/// </summary>
		public bool IsService
		{
			get
			{
				return HasTag("highway", "service");
			}
		}
	}
}
