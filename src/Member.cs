using System;
using System.Collections.Generic;
using System.Text;

namespace LTSAnalyzer
{
	class Member
	{
		private string _type;

		public string Type
		{
			get { return _type; }
			set { _type = value; }
		}
		private string _ref;

		public string Reference
		{
			get { return _ref; }
			set { _ref = value; }
		}
		private string _role;

		public string Role
		{
			get { return _role; }
			set { _role = value; }
		}

		public Member(string type, string reference, string role)
		{
			_type = type;
			_ref = reference;
			_role = role;
		}
	}
}
