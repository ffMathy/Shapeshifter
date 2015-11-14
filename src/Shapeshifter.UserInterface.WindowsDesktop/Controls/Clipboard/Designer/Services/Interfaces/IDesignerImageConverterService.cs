#region

using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces
{
    public interface IDesignerImageConverterService : ISingleInstance
    {
        byte[] GenerateDesignerImageBytesFromFileBytes(byte[] fileBytes);
    }
}