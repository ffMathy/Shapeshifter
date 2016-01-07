namespace Shapeshifter.WindowsDesktop.Shared.Services.Arguments
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Interfaces;

    class AggregateArgumentProcessor : IAggregateArgumentProcessor
    {
        readonly IEnumerable<IArgumentProcessor> argumentProcessors;
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
            IEnumerable<IArgumentProcessor> argumentProcessors)
        {
            this.argumentProcessors = argumentProcessors;

            processorsUsed = new List<IArgumentProcessor>();
        }

        public void ProcessArguments(string[] arguments)
        {
            lock (processorsUsed)
            {
                processorsUsed.Clear();
                
                foreach (var processor in argumentProcessors)
                {
                    ProcessArgumentsWithProcessor(arguments, processor);
                }
            }

            shouldTerminate = processorsUsed.Any(
                x => x.Terminates);
            isProcessed = true;
        }

        void ProcessArgumentsWithProcessor(string[] arguments, IArgumentProcessor processor)
        {
            if (!processor.CanProcess(arguments))
            {
                return;
            }

            processor.Process(arguments);
            processorsUsed.Add(processor);
        }
    }
}
