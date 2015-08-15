using System.Windows;
using Autofac;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Services;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows;
using System.Reflection;
using System;
using System.Linq;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Files;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class App : Application
    {
        private static ILifetimeScope container;

        public static ILifetimeScope Container
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
                //TODO: this is not the responsibility of "App". move out to a separate class.

                var builder = new ContainerBuilder();

                RegisterAssemblyTypes(builder, typeof(IClipboardData).Assembly);
                RegisterAssemblyTypes(builder, typeof(App).Assembly);

                RegisterServices(builder);

                if (callback != null)
                {
                    callback(builder);
                }

                var newContainer = builder.Build();
                LaunchServices(newContainer);

                container = newContainer;
            }
        }

        private static void LaunchServices(ILifetimeScope container)
        {
            var serviceTypes = GetServiceTypes();
            foreach (var serviceType in serviceTypes)
            {
                var interfaces = serviceType.GetInterfaces();
                var mainInterface = interfaces.First();

                container.Resolve(mainInterface);
            }
        }

        private static void RegisterServices(ContainerBuilder builder)
        {
            var serviceTypes = GetServiceTypes();
            foreach (var serviceType in serviceTypes)
            {
                builder.RegisterType(serviceType)
                    .AsImplementedInterfaces()
                    .SingleInstance();
            }
        }

        private static Type[] GetServiceTypes()
        {
            return new[] {
                        typeof(UpdateService),
                        typeof(ClipboardUserInterfaceMediator),
                        typeof(FileIconService),
                        typeof(ClipboardHookService),
                        typeof(ImagePersistenceService),
                        typeof(KeyboardHookService),
                        typeof(DataSourceService),
                        typeof(Downloader),
                        typeof(FileManager),
                        typeof(DesignerImageConverterService)
                    };
        }

        private static void RegisterAssemblyTypes(ContainerBuilder builder, Assembly assembly)
        {
            builder
                .RegisterAssemblyTypes(assembly)
                .AsSelf();

            builder
                .RegisterAssemblyTypes(assembly)
                .AsImplementedInterfaces();
        }

        public static bool InDesignMode
        {
            get
            {
                return !(Current is App);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            container.Dispose();
            base.OnExit(e);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //start the main window.
            var window = MainWindow = new ClipboardListWindow();
            window.Show();

            //start the clipboard mediator.
            var mediator = Container.Resolve<IClipboardUserInterfaceMediator>();
            mediator.Connect();
        }
    }
}
