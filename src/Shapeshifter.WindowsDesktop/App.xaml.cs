namespace Shapeshifter.WindowsDesktop
{
    using System;
    using System.Diagnostics;
    using System.Windows;

    using Autofac;

    using Infrastructure.Dependencies;

    using Operations.Startup;

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

        static void OnError(Exception exception)
        {
            MessageBox.Show(
                exception?.ToString(),
                "Shapeshifter error",
                MessageBoxButton.OK);
            Process.GetCurrentProcess()
                   .Kill();
        }

#pragma warning disable 4014
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, exceptionEventArguments) => {
                if (Debugger.IsAttached)
                {
                    return;
                }

                OnError((Exception)exceptionEventArguments.ExceptionObject);
            };

            var main = Container.Resolve<ApplicationEntrypoint>();
            try
            {
                main.Start(e.Args);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }
#pragma warning restore 4014
    }
}