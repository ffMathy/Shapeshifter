namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using Interfaces;
	using Serilog;

	class AggregateArgumentProcessor : IAggregateArgumentProcessor
	{
		readonly IEnumerable<INoArgumentProcessor> noArgumentProcessors;
		readonly ILogger logger;
		readonly IEnumerable<ISingleArgumentProcessor> singleArgumentProcessors;

		readonly ICollection<IArgumentProcessor> processorsUsed;

		bool isProcessed;
		bool shouldTerminate;

		public IEnumerable<IArgumentProcessor> ProcessorsUsed
		{
			get
			{
				if (!isProcessed)
				{
					throw new InvalidOperationException(
						"Can't get the processors used before all arguments have been processed.");
				}

				return processorsUsed;
			}
		}

		public bool ShouldTerminate
		{
			get
			{
				if (!isProcessed)
				{
					throw new InvalidOperationException(
						"Can't get termination consensus before all arguments have been processed.");
				}

				return shouldTerminate;
			}
		}

		public AggregateArgumentProcessor(
			IEnumerable<ISingleArgumentProcessor> singleArgumentProcessors,
			IEnumerable<INoArgumentProcessor> noArgumentProcessors,
			ILogger logger)
		{
			this.singleArgumentProcessors = singleArgumentProcessors;
			this.noArgumentProcessors = noArgumentProcessors;
			this.logger = logger;

			processorsUsed = new List<IArgumentProcessor>();
		}

		public async Task ProcessArgumentsAsync(string[] arguments)
		{
			logger.Information("Processing given command line arguments {@arguments}.", arguments);

			processorsUsed.Clear();

			logger.Information("Running single argument processors.");
			await ProcessSingleArgumentProcessorsAsync(arguments);

			if (!processorsUsed.Any())
			{
				logger.Information("Running no-argument processors since no single argument processor was used.");
				ProcessNoArgumentProcessors();
			}

			shouldTerminate = processorsUsed.Any(
				x => x.Terminates);
			isProcessed = true;

			logger.Verbose($"All command line arguments have been processed. Argument processor {(shouldTerminate ? "will" : "won't")} trigger termination.");
		}

		void ProcessNoArgumentProcessors()
		{
			foreach (var processor in noArgumentProcessors)
			{
				logger.Verbose("Triggering no-argument processor {processor}.", processor.GetType().Name);
				ProcessArgumentsWithNoArgumentProcessor(processor);
			}
		}

		void ProcessArgumentsWithNoArgumentProcessor(INoArgumentProcessor processor)
		{
			if (!processor.CanProcess())
			{
				return;
			}

			processor.Process();
			processorsUsed.Add(processor);
		}

		async Task ProcessSingleArgumentProcessorsAsync(string[] arguments)
		{
			foreach (var processor in singleArgumentProcessors)
			{
				await ProcessArgumentsWithSingleArgumentProcessorAsync(arguments, processor);
			}
		}

		async Task ProcessArgumentsWithSingleArgumentProcessorAsync(string[] arguments, ISingleArgumentProcessor processor)
		{
			if (!processor.CanProcess(arguments))
			{
				return;
			}

			logger.Verbose("Triggering argument processor {processor} with arguments {@arguments}.", processor.GetType().Name, arguments);

			await processor.ProcessAsync(arguments);
			processorsUsed.Add(processor);
		}
	}
}