using System.Windows;
using Autofac;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Services;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows;

namespace Shapeshifter.UserInterface.WindowsDesktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static IContainer container;
        public static IContainer Container
        {
            get
            {
                if (container == null)
                {
                    var builder = new ContainerBuilder();

                    //core.
                    builder
                        .RegisterAssemblyTypes(typeof(IClipboardData).Assembly)
                        .AsSelf();

                    builder
                        .RegisterAssemblyTypes(typeof(IClipboardData).Assembly)
                        .AsImplementedInterfaces();

                    //this assembly.
                    builder
                        .RegisterAssemblyTypes(typeof(App).Assembly)
                        .AsSelf();

                    builder
                        .RegisterAssemblyTypes(typeof(App).Assembly)
                        .AsImplementedInterfaces();

                    //register services.
                    builder
                        .RegisterType<ClipboardUserInterfaceMediator>()
                        .As<IClipboardUserInterfaceMediator>()
                        .SingleInstance();
                    
                    builder
                        .RegisterType<FileIconService>()
                        .As<IFileIconService>()
                        .SingleInstance();

                    builder
                        .RegisterType<ClipboardHookService>()
                        .As<IClipboardHookService>()
                        .SingleInstance();

                    builder
                        .RegisterType<KeyboardHookService>()
                        .As<IKeyboardHookService>()
                        .SingleInstance();

                    container = builder.Build();
                }

                return container;
            }
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
