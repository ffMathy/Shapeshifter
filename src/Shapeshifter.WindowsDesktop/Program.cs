namespace Shapeshifter.WindowsDesktop
{
	using Autofac;
	using Serilog;
	using Serilog.Context;
	using Serilog.Core;
	using Shapeshifter.WindowsDesktop.Controls.Window.Interfaces;
	using Shapeshifter.WindowsDesktop.Infrastructure.Dependencies;
	using Shapeshifter.WindowsDesktop.Infrastructure.Environment.Interfaces;
	using Shapeshifter.WindowsDesktop.Services.Web.Updates.Interfaces;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Reflection;
	using System.Threading;
	using System.Windows;

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
		static ILifetimeScope container;

		static ILifetimeScope Container
		{
			get
			{
				if (container == null)
				{
					CreateContainer();
				}
				return container;
			}
		}

		public static void CreateContainer(Action<ContainerBuilder> callback = null)
		{
			lock (typeof(App))
			{
				var builder = new ContainerBuilder();
				builder.RegisterModule(new DefaultWiringModule());

				container = builder.Build();
			}
		}

		public static Version GetCurrentVersion()
		{
			var assembly = Assembly.GetExecutingAssembly();
			return assembly
				.GetName()
				.Version;
		}

		static void OnError(Exception exception)
		{
			var environmentInformation = Container.Resolve<IEnvironmentInformation>();

			var isDebugging = environmentInformation.GetIsDebugging();
			if (isDebugging)
			{
				var window = Container.Resolve<IMainWindow>();
				window.Hide();

				Debugger.Break();
			}
			else
			{
				MessageBox.Show(
					"Woops, something bad happened with Shapeshifter, and it needs to close. We're so sorry!\n\nYou can find a detailed log file with details in %TEMP%\\Shapeshifter\\Shapeshifter.log.",
					"Shapeshifter error",
					MessageBoxButton.OK,
					MessageBoxImage.Error);
			}

			var updateService = Container.Resolve<IUpdateService>();
			updateService.UpdateAsync();
		}

		[STAThread]
		public static void Main(string[] args)
		{
			try
			{
				AppDomain.CurrentDomain.UnhandledException += (sender, exceptionEventArguments) => {
					var exception = (Exception)exceptionEventArguments.ExceptionObject;
					OnError(exception);
				};

				var app = new App(Container);
				app.InitializeComponent();
				app.Run();
			}
			catch (Exception ex) when (ExceptionLogCallback(ex))
			{
			}
		}

		static bool ExceptionLogCallback(Exception ex)
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

			return false;
		}
	}
}