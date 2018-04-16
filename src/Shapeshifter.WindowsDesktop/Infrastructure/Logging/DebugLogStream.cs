namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging
{
    using System.Diagnostics;
	using System.Threading.Tasks;
	using Interfaces;

    class DebugLogStream: ILogStream
    {
		private readonly IFileLogStream fileLogStream;

		public DebugLogStream(IFileLogStream fileLogStream)
		{
			this.fileLogStream = fileLogStream;
		}

        public async Task WriteLineAsync(string input)
        {
            Debug.WriteLine(input);
			await fileLogStream.WriteLineAsync(input);
        }
    }
}