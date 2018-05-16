namespace Shapeshifter.WindowsDesktop.Information
{
	using System.Diagnostics;
	using System.IO;

	public static class CurrentProcessInformation
	{
		public static Process CurrentProcess { get; }

		static CurrentProcessInformation()
		{
			CurrentProcess = Process.GetCurrentProcess();
		}

		public static string GetCurrentProcessFilePath()
		{
			return CurrentProcess.MainModule.FileName;
		}

		public static string GetCurrentProcessDirectory()
		{
			return Path.GetDirectoryName(GetCurrentProcessFilePath());
		}
	}
}
