using System.Collections.Generic;

namespace Shapeshifter.WindowsDesktop.Services.Arguments
{
    using System.Linq;

    using Shared.Services.Arguments.Interfaces;

    class AggregateArgumentProcessor: IAggregateArgumentProcessor
    {
        readonly IEnumerable<IArgumentProcessor> argumentProcessors;

        public bool ShouldTerminate { get; set; }

        public AggregateArgumentProcessor(
            IEnumerable<IArgumentProcessor> argumentProcessors)
        {
            this.argumentProcessors = argumentProcessors;
        }

        public void ProcessArguments(string[] arguments)
        {
            ShouldTerminate = false;

            var processorsUsed = new List<IArgumentProcessor>();
            foreach (var processor in argumentProcessors)
            {
                if (!processor.CanProcess(arguments))
                {
                    continue;
                }

                processor.Process(arguments);
                processorsUsed.Add(processor);
            }

            ShouldTerminate = processorsUsed.Any(x => x.Terminates);
        }
    }
}
