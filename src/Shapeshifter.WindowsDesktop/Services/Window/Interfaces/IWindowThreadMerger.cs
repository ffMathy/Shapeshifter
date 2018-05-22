namespace Shapeshifter.WindowsDesktop.Services.Window.Interfaces
{
	public interface IWindowThreadMerger
	{
		void MergeThread(int threadId);
		void UnmergeThread(int threadId);
	}
}