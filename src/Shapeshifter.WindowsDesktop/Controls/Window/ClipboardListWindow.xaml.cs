namespace Shapeshifter.WindowsDesktop.Controls.Window
{
    using System;
    using System.Windows;
    using System.Windows.Interop;

    using Infrastructure.Events;

    using Interfaces;

    using Mediators.Interfaces;

    using ViewModels.Interfaces;

    [TemplatePart(Name = "Core", Type = typeof(FrameworkElement))]
    [TemplateVisualState(Name= "InPackagesList", GroupName= "TargetList")]
    [TemplateVisualState(Name= "InActionList", GroupName= "TargetList")]
    public partial class ClipboardListWindow
        : Window,
          IClipboardListWindow
    {
        readonly IMainWindowHandleContainer handleContainer;
        readonly IClipboardListViewModel viewModel;

        public ClipboardListWindow(
            IClipboardListViewModel viewModel,
            IMainWindowHandleContainer handleContainer)
        {
            this.handleContainer = handleContainer;
            this.viewModel = viewModel;

            SourceInitialized += ClipboardListWindow_SourceInitialized;
            Activated += ClipboardListWindow_Activated;

            InitializeComponent();
            SetupViewModel();
        }

        void ClipboardListWindow_SourceInitialized(object sender, EventArgs e)
        {
            handleContainer.Handle = HandleSource.Handle;
        }

        public HwndSource HandleSource => PresentationSource.FromVisual(this) as HwndSource;

        void ClipboardListWindow_Activated(object sender, EventArgs e)
        {
            Activated -= ClipboardListWindow_Activated;
            Hide();
        }

        void SetupViewModel()
        {
            viewModel.UserInterfaceShown += ViewModel_UserInterfaceShown;
            viewModel.UserInterfaceHidden += ViewModel_UserInterfaceHidden;
            viewModel.UserInterfacePaneSwapped += ViewModel_UserInterfacePaneSwapped;

            DataContext = viewModel;
        }

        void ViewModel_UserInterfacePaneSwapped(object sender, UserInterfacePaneSwappedEventArgument e)
        {
            switch (e.Pane)
            {
                case ClipboardUserInterfacePane.Actions:
                    VisualStateManager.GoToElementState(this, "InActionList", true);
                    //VisualStateManager.GoToElementState(this, "InActionList", true);
                    break;

                case ClipboardUserInterfacePane.ClipboardPackages:
                    VisualStateManager.GoToElementState(this, "InPackagesList", true);
                    //VisualStateManager.GoToElementState(this, "InPackagesList", true);
                    break;
            }
        }

        void ViewModel_UserInterfaceHidden(
            object sender,
            UserInterfaceHiddenEventArgument e)
        {
            Hide();
        }

        void ViewModel_UserInterfaceShown(
            object sender,
            UserInterfaceShownEventArgument e)
        {
            Show();
        }

        public void AddHwndSourceHook(HwndSourceHook hook)
        {
            HandleSource.AddHook(hook);
        }

        public void RemoveHwndSourceHook(HwndSourceHook hook)
        {
            HandleSource.RemoveHook(hook);
        }

        public void Dispose()
        {
            viewModel.UserInterfaceShown -= ViewModel_UserInterfaceShown;
            viewModel.UserInterfaceHidden -= ViewModel_UserInterfaceHidden;
            viewModel.UserInterfacePaneSwapped -= ViewModel_UserInterfacePaneSwapped;
        }
    }
}