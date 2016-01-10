namespace Shapeshifter.WindowsDesktop.Infrastructure.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;

    using Handles;
    using Handles.Interfaces;

    using Interfaces;

    class Logger: ILogger
    {
        const int MinimumImportanceFactor = 0;
        const int IndentationSize = 2;

        readonly ILogStream stream;

        readonly IDictionary<int, int> threadIndentationCache;

        static int ManagedThreadId => Thread.CurrentThread.ManagedThreadId;

        public Logger(
            ILogStream stream)
        {
            this.stream = stream;

            threadIndentationCache = new Dictionary<int, int>();
        }

        void Log(string text)
        {
            var indentationString = GenerateIndentationString();
            stream.WriteLine($"{indentationString}{text}");
        }

        string GenerateIndentationString()
        {
            var indentationString = string.Empty;
            if (threadIndentationCache.ContainsKey(ManagedThreadId))
            {
                indentationString = new string(' ', threadIndentationCache[ManagedThreadId] * IndentationSize);
            }
            return indentationString;
        }

        public void Error(string text)
        {
            Log("Error: " + text);
        }

        public void Performance(string text)
        {
            Log("Performance information: " + text);
        }

        public void PrintStackTrace()
        {
            var stackTrace = new StackTrace();
            Log("Stack trace: " + stackTrace
                    .GetFrames()
                    .Reverse()
                    .Select(x => x.GetMethod())
                    .Select(x => x.Name)
                    .TakeWhile(x => x != nameof(PrintStackTrace))
                    .Aggregate((a, b) => $"{a} -> {b}"));
        }

        public IIndentationHandle Indent()
        {
            return new IndentationHandle(this);
        }

        internal void IncreaseIndentation()
        {
            if (threadIndentationCache.ContainsKey(ManagedThreadId))
            {
                threadIndentationCache[ManagedThreadId]++;
            }
            else
            {
                threadIndentationCache.Add(ManagedThreadId, 1);
            }
        }

        internal void DecreaseIndentation()
        {
            if (!threadIndentationCache.ContainsKey(ManagedThreadId))
            {
                throw new InvalidOperationException("The indentation has not been increased on this thread.");
            }

            var newIndentation = threadIndentationCache[ManagedThreadId]--;
            if (newIndentation == 0)
            {
                threadIndentationCache.Remove(ManagedThreadId);
            }
        }

        public void Information(
            string text,
            int importanceFactor = 0)
        {
            if (importanceFactor >= MinimumImportanceFactor)
            {
                Log("Information: " + text);
            }
        }

        public void Warning(string text)
        {
            Log("Warning: " + text);
        }
    }
}