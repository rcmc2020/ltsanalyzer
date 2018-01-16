using System;
using System.Collections.Generic;
using System.Text;

namespace LTSAnalyzer {
	class Relation : ElementBase {
		List<Member> _members;

		internal List<Member> Members {
			get {
				if (_members == null) {
					_members = new List<Member>();
				}
				return _members;
			}
		}
	}
}
