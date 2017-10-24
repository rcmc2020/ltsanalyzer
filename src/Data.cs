using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace LTSAnalyzer
{
   class Data
   {
      public static readonly IList<string> NamespaceTags = new ReadOnlyCollection<string>(
         new List<string> {
            "area:highway",
            "bus:lanes",
            "bridge:movable",
            "bridge:structure",
            "cycleway:buffer",
            "cycleway:left",
            "cycleway:right",
            "cycleway:seasonal",
            "destination:ref",
            "destination:street",
            "hgv:conditional",
            "hgv:lanes",
            "lanes:backward",
            "lanes:both_ways",
            "lanes:bus",
            "lanes:forward",
            "lanes:psv",
            "maxspeed:advisory",
            "maxspeed:backward",
            "maxspeed:conditional",
            "maxspeed:forward",
            "maxspeed:school",
            "mtb:scale",
            "mtb:scale:uphill",
            "oneway:bicycle",
            "parking:condition:both:default",
            "parking:condition:both:maxstay",
            "parking:condition:both:time_interval",
            "parking:condition:left",
            "parking:condition:left:time_interval",
            "parking:condition:right",
            "parking:condition:right:maxstay",
            "parking:condition:right:time_interval",
            "parking:lane",
            "parking:lane:both",
            "parking:lane:left",
            "parking:lane:right",
            "piste:type",
            "piste:grooming",
            "piste:difficulty",
            "psv:lanes",
            "ramp:bicycle",
            "shoulder:access:bicycle",
            "shoulder:surface",
            "temporary:access",
            "temporary:date_off",
            "temporary:date_on",
            "turn:lanes",
            "turn:lanes:backward",
            "turn:lanes:both_ways",
            "turn:lanes:forward"
         }
      );
   }
}
