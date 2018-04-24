using AutofacModule = Autofac.Module;

namespace Shapeshifter.WindowsDesktop.Infrastructure.Dependencies
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Windows.Threading;

	using Autofac;
	using AutofacSerilogIntegration;
	using Controls.Clipboard.Designer.Helpers;

	using Environment;
	using Environment.Interfaces;

	using Native;
	using Serilog;
	using Serilog.Events;
	using Serilog.Formatting;
	using Serilog.Formatting.Compact;
	using Shapeshifter.WindowsDesktop.Services.Files;
	using Shapeshifter.WindowsDesktop.Services.Processes;
	using Threading;

	public class DefaultWiringModule : AutofacModule
	{
		readonly IEnvironmentInformation environmentInformation;

		readonly Action<ContainerBuilder> callback;

		public DefaultWiringModule(
			IEnvironmentInformation environmentInformation)
		{
			this.environmentInformation = environmentInformation;
		}

		public DefaultWiringModule(Action<ContainerBuilder> callback = null)
			: this(new Infrastructure.Environment.EnvironmentInformation())
		{
			this.callback = callback;
		}

		protected override void Load(ContainerBuilder builder)
		{
			AssemblyRegistrationHelper
				.RegisterAssemblyTypes(builder, typeof(DefaultWiringModule).Assembly, this.environmentInformation.GetIsInDesignTime());

			AssemblyRegistrationHelper
				.RegisterAssemblyTypes(builder, NativeAssemblyHelper.Assembly, this.environmentInformation.GetIsInDesignTime());

			RegisterMainThread(builder);

			var environmentInformation = RegisterEnvironmentInformation(builder);
			RegisterLogging(environmentInformation, builder);

			if (environmentInformation.GetIsInDesignTime())
			{
				DesignTimeContainerHelper.RegisterFakes(builder);
			}

			callback?.Invoke(builder);

			base.Load(builder);
		}

		static void RegisterLogging(IEnvironmentInformation environment, ContainerBuilder builder)
		{
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Verbose()
				.Enrich.WithProperty("ProcessId", Process.GetCurrentProcess().Id)
				.Enrich.FromLogContext()
				.WriteTo.ColoredConsole()
				.WriteTo.Debug()
				.WriteTo.File(
					FileManager.GetFullPathFromTemporaryPath("Shapeshifter.log"),
					fileSizeLimitBytes: 1024 * 1024,
					restrictedToMinimumLevel: LogEventLevel.Verbose,
					rollOnFileSizeLimit: false,
					shared: true,
					outputTemplate: "[{ProcessId}] {Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
				.CreateLogger();

			Log.Logger.Verbose("Logger has started.");

			builder.RegisterLogger(autowireProperties: true);
		}

		static void RegisterMainThread(ContainerBuilder builder)
		{
			builder.RegisterInstance(new UserInterfaceThread(Dispatcher.CurrentDispatcher))
				   .AsImplementedInterfaces();
		}

		IEnvironmentInformation RegisterEnvironmentInformation(ContainerBuilder builder)
		{
			var environmentInformation = this.environmentInformation ?? new EnvironmentInformation();
			builder
				.RegisterInstance(environmentInformation)
				.As<IEnvironmentInformation>()
				.SingleInstance();
			return environmentInformation;
		}
	}
}