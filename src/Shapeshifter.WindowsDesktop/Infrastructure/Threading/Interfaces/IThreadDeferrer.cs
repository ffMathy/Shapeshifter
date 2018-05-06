using System;
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
