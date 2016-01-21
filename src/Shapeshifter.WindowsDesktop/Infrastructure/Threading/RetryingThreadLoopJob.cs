using System;
using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Infrastructure.Threading
{
    using System.Collections.Generic;
    using System.Threading;

    public class RetryingThreadLoopJob
    {
        public Func<Task> Action { get; set; }
        public Func<Exception, bool> IsExceptionIgnored { get; set; }

        public CancellationToken CancellationToken { get; set; }
            = default(CancellationToken);

        public int AttemptsBeforeFailing { get; set; }
        public int IntervalInMilliseconds { get; set; }
    }
}
