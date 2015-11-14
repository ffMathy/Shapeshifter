#region

using System.Diagnostics.CodeAnalysis;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Services.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;

#endregion

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Facades
{
    [ExcludeFromCodeCoverage]
    internal class DesignerDataSourceFacade : IDataSource
    {
        private byte[] icon;

        private readonly IDesignerImageConverterService designerImageConverterService;

        public DesignerDataSourceFacade(
            IDesignerImageConverterService designerImageConverterService)
        {
            this.designerImageConverterService = designerImageConverterService;
        }

        private byte[] DecorateIcon(byte[] iconBytes)
        {
            return designerImageConverterService.GenerateDesignerImageBytesFromFileBytes(iconBytes);
        }

        public byte[] Icon
        {
            get { return icon; }
            set { icon = DecorateIcon(value); }
        }

        public string Text { get; set; }
    }
}