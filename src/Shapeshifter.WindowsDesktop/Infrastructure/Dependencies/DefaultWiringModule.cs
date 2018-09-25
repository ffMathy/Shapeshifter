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

	using FluffySpoon.Http;

	using Native;
	using Serilog;
	using Serilog.Events;

	using Logging;
	using Services.Files;

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
			: this(new EnvironmentInformation())
		{
			this.callback = callback;
		}

		protected override void Load(ContainerBuilder builder)
		{
			//AssemblyRegistrationHelper
			//	.RegisterAssemblyTypes(builder, typeof(DefaultWiringModule).Assembly, this.environmentInformation.GetIsInDesignTime());

			//AssemblyRegistrationHelper
			//	.RegisterAssemblyTypes(builder, NativeAssemblyHelper.Assembly, this.environmentInformation.GetIsInDesignTime());
			
			RegisterHttp(builder);

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

		private static void RegisterHttp(ContainerBuilder builder)
		{
			builder.RegisterInstance(new Downloader()).AsImplementedInterfaces();
			builder.RegisterInstance(new RestClient()).AsImplementedInterfaces();
		}

		static void RegisterLogging(IEnvironmentInformation environment, ContainerBuilder builder)
		{
			if (!environment.GetIsInDesignTime())
			{
				var logPath = !environment.GetIsDebugging() && environment.GetIsRunningDeveloperVersion() ?
					Path.GetTempFileName() :
					FileManager.GetFullPathFromTemporaryPath("Shapeshifter.log");

				Log.Logger = new LoggerConfiguration()
					.MinimumLevel.Verbose()
					.Enrich.WithProperty("ProcessId", Process.GetCurrentProcess().Id)
					.Enrich.FromLogContext()
					.WriteTo.Debug(
						outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj} ({SourceContext:l}){NewLine}{Exception}")
					.WriteTo.File(
						logPath,
						restrictedToMinimumLevel: LogEventLevel.Verbose,
						fileSizeLimitBytes: int.MaxValue,
						rollOnFileSizeLimit: false,
						rollingInterval: RollingInterval.Day,
						retainedFileCountLimit: 2,
						shared: true,
						outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [#{ProcessId}] [{SourceContext:l}] [{Level:u3}]{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
					.WriteTo.Sink(new IssueReporterSink(environment))
					.CreateLogger();
			}

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