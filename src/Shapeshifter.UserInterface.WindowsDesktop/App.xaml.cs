using System.Windows;
using Autofac;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Services;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Windows;
using System.Reflection;
using Shapeshifter.Core.Factories.Interfaces;

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
                    
                    RegisterAssemblyTypes(builder, typeof(IClipboardData).Assembly);
                    RegisterAssemblyTypes(builder, typeof(App).Assembly);

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

                    builder
                        .RegisterType<DataSourceService>()
                        .As<IDataSourceService>()
                        .SingleInstance();

                    container = builder.Build();
                }

                return container;
            }
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
