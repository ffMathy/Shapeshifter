namespace Shapeshifter.WindowsDesktop
{
	using System;
	using System.Windows;

	using Autofac;

	using Operations.Startup;

	/// <summary>
	///     Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private readonly ILifetimeScope container;

		public App()
		{
			throw new Exception("Use the other App constructor instead.");
		}

		public App(ILifetimeScope container)
		{
			this.container = container;
		}

		protected override void OnExit(ExitEventArgs e)
		{
			container.Dispose();
			base.OnExit(e);
		}

#pragma warning disable 4014
		protected override void OnStartup(StartupEventArgs e)
		{
			var main = container.Resolve<ApplicationEntrypoint>();
			main.Start(e.Args);
		}
#pragma warning restore 4014
	}
}