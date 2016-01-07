namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces
{
    using Shared.Infrastructure.Dependencies.Interfaces;

    public interface IDesignerImageConverterService: ISingleInstance
    {
        byte[] GenerateDesignerImageBytesFromFileBytes(byte[] fileBytes);
    }
}