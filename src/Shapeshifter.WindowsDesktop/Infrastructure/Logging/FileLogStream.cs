namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging
{

	using Interfaces;

	using Services.Files.Interfaces;
	using Dependencies;
	using System.Threading.Tasks;
	using System.IO;

	class FileLogStream : IFileLogStream
	{
		string logFileName;

		[Inject]
		public IFileManager FileManager { get; set; }

		public async Task WriteLineAsync(string input)
		{
			try
			{
				if (logFileName == null)
					logFileName = await FileManager.WriteBytesToTemporaryFileAsync("Shapeshifter.log", new byte[0]);

				await FileManager.AppendLineToFileAsync(logFileName, input);
			}
			catch (IOException)
			{
			}
		}
	}
}