namespace Shapeshifter.WindowsDesktop.Services.Messages.Interfaces
{
    using Controls.Window.Interfaces;

    using Services.Interfaces;

    public interface IWindowMessageHook: IHookService
    {
        IHookableWindow TargetWindow { get; set; }
    }
}