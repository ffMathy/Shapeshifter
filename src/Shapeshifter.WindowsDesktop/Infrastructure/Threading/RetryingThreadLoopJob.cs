using System;
using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading
{
    using System.Collections.Generic;
    using System.Threading;

    public class RetryingThreadLoopJob
    {
        public Func<Task> Action { get; }
        public IReadOnlyCollection<Type> IgnoredExceptionTypes { get; }

        public int Attempts { get; }
        public int Interval { get; }

        public CancellationToken CancellationToken { get; }
            = default(CancellationToken);

        public RetryingThreadLoopJob(
            Func<Task> action)
        {
            Action = action;
        }
    }
}
