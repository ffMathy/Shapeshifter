using System.Windows;
using Autofac;
using System.Reflection;
using System;
using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies;

namespace Shapeshifter.UserInterface.WindowsDesktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
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

        static void RegisterAssemblyTypes(ContainerBuilder builder, Assembly assembly)
        {
            builder
                .RegisterAssemblyTypes(assembly)
                .AsSelf()
                .AsImplementedInterfaces();
        }

        static bool InDesignMode
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
            var main = Container.Resolve<Main>();

            Current.DispatcherUnhandledException += (sender, exceptionEventArguments) =>
            {
                MessageBox.Show(exceptionEventArguments.Exception.ToString(), "Shapeshifter error", MessageBoxButton.OK);
                exceptionEventArguments.Handled = true;
                Current.Shutdown();
            };

            main.Start(e.Args);
        }
    }
}
