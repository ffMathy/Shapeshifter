namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Designer.Services
{
    using WindowsDesktop.Services.Images.Interfaces;

    using Interfaces;

    class DesignerImageConverterService
        : IDesignerImageConverterService,
          IDesignerService
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