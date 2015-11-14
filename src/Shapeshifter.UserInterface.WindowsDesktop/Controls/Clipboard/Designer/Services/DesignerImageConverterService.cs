#region

using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Images.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    [ExcludeFromCodeCoverage]
    internal class DesignerImageConverterService : IDesignerImageConverterService
    {
        private readonly IImageFileInterpreter imageFileInterpreter;
        private readonly IImagePersistenceService imagePersistenceService;

        public DesignerImageConverterService(
            IImagePersistenceService imagePersistenceService,
            IImageFileInterpreter imageFileInterpreter)
        {
            this.imagePersistenceService = imagePersistenceService;
            this.imageFileInterpreter = imageFileInterpreter;
        }

        public byte[] GenerateDesignerImageBytesFromFileBytes(byte[] fileBytes)
        {
            var bitmapSource = imageFileInterpreter.Interpret(fileBytes);
            return imagePersistenceService.ConvertBitmapSourceToByteArray(bitmapSource);
        }
    }
}