namespace Shapeshifter.WindowsDesktop
{
	using Autofac;
	using Serilog;

	using Controls.Window.Interfaces;
	using Infrastructure.Dependencies;
	using Services.Web.Updates.Interfaces;
	using System;
	using System.Diagnostics;
	using System.Reflection;
	using System.Threading;
	using System.Windows;

	using Information;

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

		static Program()
		{
			//if(InstallationInformation.TargetExecutableFile != CurrentProcessInformation.GetCurrentProcessFilePath())
			//	CosturaUtility.Initialize();
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

		[STAThread]
		public static void Main(string[] args)
		{
			try
			{
				AppDomain.CurrentDomain.FirstChanceException += (sender, e) => {
					if (Debugger.IsAttached)
					{
						Log.Debug(e.Exception, "First chance exception occured.");
					}
				};

				AppDomain.CurrentDomain.UnhandledException += (sender, exceptionEventArguments) => {
					var exception = (Exception)exceptionEventArguments.ExceptionObject;
					OnGlobalErrorOccured(exception);
				};

				var app = new App(Container);
				app.InitializeComponent();
				app.Run();
			}
			catch (Exception ex) when (OnGlobalErrorOccured(ex))
			{
			}
		}

		public static bool OnGlobalErrorOccured(Exception ex)
		{
			Log.Logger.Error(ex, "An unhandled error occured.");

			if (Debugger.IsAttached)
			{
				var window = Container.Resolve<IMainWindow>();
				window.Hide();

				Debugger.Break();
			}

			Log.CloseAndFlush();

			var updateService = Container.Resolve<IUpdateService>();
			var updateTask = updateService.UpdateAsync();

			MessageBox.Show(
				"Woops, something bad happened with Shapeshifter, and it needs to close. We're so sorry!\n\nYou can find a detailed log file with details in %TEMP%\\Shapeshifter\\Shapeshifter.log.",
				"Shapeshifter error",
				MessageBoxButton.OK,
				MessageBoxImage.Error);

			updateTask.Wait();

			Thread.Sleep(10000);

			if (!Debugger.IsAttached)
			{
				Process.GetCurrentProcess().Kill();
			}

			return false;
		}

		//public static void Void()
		//{
		//	//this method exists to force the static constructor to load and initialize assemblies via costura fody.
		//}
	}
}