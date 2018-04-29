using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shapeshifter.Website.Models.GitHub.Request
{
    public class SerializableException
	{
		public string Message { get; set; }
		public string Context { get; set; }
		public string Name { get; set; }
		public string StackTrace { get; set; }
	}
}
