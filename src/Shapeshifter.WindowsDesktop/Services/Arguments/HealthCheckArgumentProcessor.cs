namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
    using System;
    using System.IO;
    using System.Linq;

    using Interfaces;
	using Shapeshifter.WindowsDesktop.Infrastructure.Dependencies;

	class HealthCheckArgumentProcessor : ISingleArgumentProcessor
    {
		public HealthCheckArgumentProcessor()
		{

		}

        public bool Terminates
            => true;

        public bool CanProcess(string[] arguments)
        {
            return arguments.Contains("healthcheck");
        }

        public void Process(string[] arguments)
        {
			
        }
    }
}