using System.Windows;
using Shapeshifter.Core.Data;
using Shapeshifter.Core.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces;
using System.Text;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    class TextClipboardDataControlFactory : ITextClipboardDataControlFactory
    {
        readonly IDataSourceService dataSourceService;

        readonly IClipboardControlFactory<IClipboardTextData, IClipboardTextDataControl> textControlFactory;

        public TextClipboardDataControlFactory(
            IDataSourceService dataSourceService,
            IClipboardControlFactory<IClipboardTextData, IClipboardTextDataControl> textControlFactory)
        {
            this.dataSourceService = dataSourceService;
            this.textControlFactory = textControlFactory;
        }

        public IClipboardControl BuildControl(IClipboardData clipboardData)
        {
            return textControlFactory.CreateControl((IClipboardTextData)clipboardData);
        }

        public IClipboardData BuildData(string format, byte[] data)
        {
            return new ClipboardTextData(dataSourceService)
            {
                Text = Encoding.UTF8.GetString(data)
            };
        }

        public bool CanBuildControl(IClipboardData data)
        {
            return data is IClipboardTextData;
        }

        public bool CanBuildData(string format)
        {
            return format == DataFormats.Text;
        }
    }
}
