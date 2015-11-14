using System;
using System.Text;
using Shapeshifter.UserInterface.WindowsDesktop.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Factories.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Factories
{
    internal class TextClipboardDataControlFactory : ITextClipboardDataControlFactory
    {
        private readonly IDataSourceService dataSourceService;

        private readonly IClipboardControlFactory<IClipboardTextData, IClipboardTextDataControl> textControlFactory;

        public TextClipboardDataControlFactory(
            IDataSourceService dataSourceService,
            IClipboardControlFactory<IClipboardTextData, IClipboardTextDataControl> textControlFactory)
        {
            this.dataSourceService = dataSourceService;
            this.textControlFactory = textControlFactory;
        }

        public IClipboardControl BuildControl(IClipboardData clipboardData)
        {
            return textControlFactory.CreateControl((IClipboardTextData) clipboardData);
        }

        public IClipboardData BuildData(uint format, byte[] data)
        {
            if (!CanBuildData(format))
            {
                throw new ArgumentException("Can't construct data from this format.", nameof(format));
            }

            var text = GetProcessedTextFromRawData(format, data);
            return new ClipboardTextData(dataSourceService)
            {
                Text = text,
                RawData = data,
                RawFormat = format
            };
        }

        private static string GetProcessedTextFromRawData(uint format, byte[] data)
        {
            var text = GetTextFromRawData(format, data);

            var terminaryNullCharacterPosition = text.IndexOf('\0');
            if (terminaryNullCharacterPosition > -1)
            {
                return text.Substring(0, terminaryNullCharacterPosition);
            }
            return text;
        }

        private static string GetTextFromRawData(uint format, byte[] data)
        {
            switch (format)
            {
                case ClipboardApi.CF_TEXT:
                    return Encoding.UTF8.GetString(data);

                case ClipboardApi.CF_OEMTEXT:
                    return Encoding.Default.GetString(data);

                case ClipboardApi.CF_UNICODETEXT:
                    return Encoding.Unicode.GetString(data);

                default:
                    throw new InvalidOperationException("Unknown format.");
            }
        }

        public bool CanBuildControl(IClipboardData data)
        {
            return
                data is IClipboardTextData;
        }

        public bool CanBuildData(uint format)
        {
            return
                format == ClipboardApi.CF_TEXT ||
                format == ClipboardApi.CF_OEMTEXT ||
                format == ClipboardApi.CF_UNICODETEXT;
        }
    }
}