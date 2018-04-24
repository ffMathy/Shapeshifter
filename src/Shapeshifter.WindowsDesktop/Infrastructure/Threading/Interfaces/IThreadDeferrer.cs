using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading.Interfaces
{
	public interface IThreadDeferrer
	{
		Task DeferAsync(
			int milliseconds,
			Action action);
	}
}
