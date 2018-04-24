using Shapeshifter.WindowsDesktop.Infrastructure.Threading.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading
{
	class ThreadDeferrer : IThreadDeferrer
	{
		IThreadLoop threadLoop;
		IThreadDelay threadDelay;

		DateTime currentDefer;
		Action action;

		public ThreadDeferrer(
			IThreadLoop threadLoop,
			IThreadDelay threadDelay)
		{
			this.threadLoop = threadLoop;
			this.threadDelay = threadDelay;
		}

		public async Task DeferAsync(
			int milliseconds, 
			Action action)
		{
			currentDefer = DateTime.UtcNow.Add(TimeSpan.FromMilliseconds(milliseconds));
			this.action = action;
			
			if(threadLoop.IsRunning)
				return;
				
			await threadLoop.StartAsync(async () => {
				if(DateTime.UtcNow >= currentDefer) {
					threadLoop.Stop();

					action();
					return;
				}

				var delta = currentDefer - DateTime.UtcNow;
				await threadDelay.ExecuteAsync((int)delta.TotalMilliseconds);
			});
		}
	}
}
