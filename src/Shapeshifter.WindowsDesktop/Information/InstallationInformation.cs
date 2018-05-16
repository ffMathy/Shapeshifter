using System;

namespace Shapeshifter.WindowsDesktop.Information
{
	using System.IO;

	public static class InstallationInformation
	{
		public static string TargetDirectory
		{
			get
			{
				var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
				return Path.Combine(programFilesPath, nameof(Shapeshifter));
			}
		}

		public static string TargetExecutableFile => Path.Combine(TargetDirectory, $"{nameof(Shapeshifter)}.exe");
	}
}
