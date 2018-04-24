namespace Shapeshifter.WindowsDesktop
{
	using System;
	using System.Diagnostics;
	using System.Windows;

	using Autofac;

	using Infrastructure.Dependencies;

	using Operations.Startup;
	using Infrastructure.Environment.Interfaces;
	using Controls.Window.Interfaces;
	using Serilog;
	using System.Threading;
	using Shapeshifter.WindowsDesktop.Services.Web.Updates.Interfaces;
	using Serilog.Context;

	/// <summary>
	///     Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
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

		protected override void OnExit(ExitEventArgs e)
		{
			container.Dispose();
			base.OnExit(e);
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

#pragma warning disable 4014
		protected override void OnStartup(StartupEventArgs e)
		{
			var lastError = (Exception)null;

			DispatcherUnhandledException += (sender, exceptionEventArguments) => {
				if (exceptionEventArguments.Exception?.Message != lastError?.Message)
					lastError = exceptionEventArguments.Exception;

				OnError(exceptionEventArguments.Exception);
			};

			AppDomain.CurrentDomain.UnhandledException += (sender, exceptionEventArguments) => {
				var exception = (Exception)exceptionEventArguments.ExceptionObject;
				if (exception?.Message != lastError?.Message)
					lastError = exception;

				OnError(exception);
			};

			var main = Container.Resolve<ApplicationEntrypoint>();
			main.Start(e.Args);
		}
#pragma warning restore 4014
	}
}