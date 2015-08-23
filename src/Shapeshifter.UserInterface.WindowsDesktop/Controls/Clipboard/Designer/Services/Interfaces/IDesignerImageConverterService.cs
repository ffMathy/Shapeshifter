using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces
{
    public interface IDesignerImageConverterService : ISingleInstance
    {
        byte[] GenerateDesignerImageBytesFromFileBytes(byte[] fileBytes);
    }
}
