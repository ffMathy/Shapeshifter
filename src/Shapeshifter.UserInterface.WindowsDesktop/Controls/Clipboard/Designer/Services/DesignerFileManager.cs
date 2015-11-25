using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    [ExcludeFromCodeCoverage]
    internal class DesignerFileManager : IFileManager, IDesignerService
    {
        public string PrepareFolder(string path)
        {
            return null;
        }

        public string WriteBytesToTemporaryFile(string path, byte[] bytes)
        {
            return null;
        }
    }
}