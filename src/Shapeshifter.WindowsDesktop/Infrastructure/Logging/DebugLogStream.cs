namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging
{
    using System.Diagnostics;

    using Interfaces;

    class DebugLogStream: ILogStream
    {
		private readonly IFileLogStream fileLogStream;

		public DebugLogStream(IFileLogStream fileLogStream)
		{
			this.fileLogStream = fileLogStream;
		}

        public void WriteLine(string input)
        {
            Debug.WriteLine(input);
			fileLogStream.WriteLine(input);
        }
    }
}