using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Files.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    [ExcludeFromCodeCoverage]
    internal class DesignerFileManager : IFileManager
    {
        public string PrepareTemporaryPath(string path)
        {
            return null;
        }

        public string WriteBytesToTemporaryFile(string path, byte[] bytes)
        {
            return null;
        }
    }
}