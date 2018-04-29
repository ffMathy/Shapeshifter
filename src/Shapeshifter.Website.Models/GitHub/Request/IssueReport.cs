using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shapeshifter.Website.Models.GitHub.Request
{
	public class IssueReport
	{
		public SerializableException Exception { get; set; }

		public string OffendingLogLine { get; set; }
		public string OffendingLogMessage { get; set; }

		public string Version { get; set; }

		public string[] RecentLogLines { get; set; }
	}
}
