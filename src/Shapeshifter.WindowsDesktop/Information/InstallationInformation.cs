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
				var rootPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
				return Path.Combine(rootPath, nameof(Shapeshifter));
			}
		}

		public static string TargetExecutableFile => Path.Combine(TargetDirectory, $"{nameof(Shapeshifter)}.exe");
	}
}
