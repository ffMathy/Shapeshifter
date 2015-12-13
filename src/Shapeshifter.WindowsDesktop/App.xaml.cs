namespace Shapeshifter.WindowsDesktop
{
    using System;
    using System.Windows;

    using Autofac;

    using Infrastructure.Dependencies;

    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    
    public partial class App: Application
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
            lock (typeof (App))
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

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, exceptionEventArguments) =>
            {
                MessageBox.Show(
                    exceptionEventArguments.ExceptionObject.ToString(),
                    "Shapeshifter error",
                    MessageBoxButton.OK);
                Current.Shutdown();
            };

            var main = Container.Resolve<Main>();
            main.Start(e.Args);
        }
    }
}