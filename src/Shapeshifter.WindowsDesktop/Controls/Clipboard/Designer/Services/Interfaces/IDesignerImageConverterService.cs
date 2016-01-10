namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces
{
    using Infrastructure.Dependencies.Interfaces;

    public interface IDesignerImageConverterService: ISingleInstance
    {
        byte[] GenerateDesignerImageBytesFromFileBytes(byte[] fileBytes);
    }
}