using System;

namespace Shapeshifter.WindowsDesktop.Services.Window
{
	using Infrastructure.Threading.Interfaces;

	using Interfaces;

	using Native.Interfaces;
	
	class WindowThreadMerger: IWindowThreadMerger
    {
		readonly IWindowNativeApi windowNativeApi;
		readonly IUserInterfaceThread userInterfaceThread;

		public WindowThreadMerger(
			IWindowNativeApi windowNativeApi,
			IUserInterfaceThread userInterfaceThread)
		{
			this.windowNativeApi = windowNativeApi;
			this.userInterfaceThread = userInterfaceThread;
		}

		public void MergeThread(int threadId)
		{
			Run(threadId, true);
        }

		public void UnmergeThread(int threadId)
		{
			Run(threadId, false);
		}

		void Run(int threadId, bool merge)
		{
			windowNativeApi.AttachThreadInput(
				(IntPtr)threadId,
				(IntPtr)userInterfaceThread.Id,
				merge);
        }
	}
}
