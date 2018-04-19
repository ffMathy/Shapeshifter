namespace Shapeshifter.WindowsDesktop
{
	using Serilog;
	using Serilog.Context;
	using Serilog.Core;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Threading;

	public static class CrossThreadLogContext
	{
		private static readonly Dictionary<int, IDictionary<string, object>> threadContexts;

		public static IReadOnlyDictionary<int, IDictionary<string, object>> ThreadContexts => threadContexts;

		static CrossThreadLogContext()
		{
			threadContexts = new Dictionary<int, IDictionary<string, object>>();
		}

		public static IDisposable Add(string name, object value)
		{
			lock (threadContexts)
			{
				if (!threadContexts.ContainsKey(Thread.CurrentThread.ManagedThreadId))
					threadContexts.Add(Thread.CurrentThread.ManagedThreadId, new Dictionary<string, object>());

				var dictionary = threadContexts[Thread.CurrentThread.ManagedThreadId];
				if(!dictionary.ContainsKey(name))
					dictionary.Add(name, value);

				dictionary[name] = value;

				return LogContext.PushProperty(name, value, true);
			}
		}
	}

	public static class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			try
			{
				App.Main();
			}
			catch (Exception ex) when (ExceptionLogCallback(ex))
			{
			}
		}

		private static bool ExceptionLogCallback(Exception ex)
		{
			var contexts = CrossThreadLogContext.ThreadContexts;
			var parameters = contexts.ContainsKey(Thread.CurrentThread.ManagedThreadId) ? 
				contexts[Thread.CurrentThread.ManagedThreadId] : 
				null;

			Log.Logger.Error(ex, "An unhandled error occured. {@parameters}", parameters);
			Log.CloseAndFlush();

			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}
			else
			{
				Process.GetCurrentProcess().Kill();
			}

			return true;
		}
	}
}