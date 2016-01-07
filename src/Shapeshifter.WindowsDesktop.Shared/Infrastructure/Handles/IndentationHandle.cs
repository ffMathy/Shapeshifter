namespace Shapeshifter.WindowsDesktop.Shared.Infrastructure.Handles
{
    using Interfaces;

    using Logging;

    class IndentationHandle : IIndentationHandle
    {
        readonly Logger logger;

        public IndentationHandle(
            Logger logger)
        {
            this.logger = logger;

            logger.IncreaseIndentation();
        }

        public void Dispose()
        {
            logger.DecreaseIndentation();
        }
    }
}