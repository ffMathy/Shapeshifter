using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Images.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    [ExcludeFromCodeCoverage]
    class DesignerImageConverterService : IDesignerImageConverterService
    {
        readonly IImageFileInterpreter imageFileInterpreter;
        readonly IImagePersistenceService imagePersistenceService;

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
