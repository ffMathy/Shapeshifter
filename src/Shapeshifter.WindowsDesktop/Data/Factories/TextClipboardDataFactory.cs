namespace Shapeshifter.WindowsDesktop.Data.Factories
{
	using System;
	using System.Text;

	using Data.Interfaces;
	using HtmlAgilityPack;
	using Interfaces;

	using Native;
	using RtfPipe;
	using RtfPipe.Converter.Text;
	using Services.Clipboard.Interfaces;
	using Shapeshifter.WindowsDesktop.Native.Interfaces;

	class TextClipboardDataFactory : ITextClipboardDataFactory
	{
		const string RichTextFormat = "Rich Text Format";
		const string HtmlFormat = "HTML Format";

		readonly IDataSourceService dataSourceService;
		readonly IClipboardNativeApi clipboardNativeApi;

		public TextClipboardDataFactory(
			IDataSourceService dataSourceService,
			IClipboardNativeApi clipboardNativeApi)
		{
			this.dataSourceService = dataSourceService;
			this.clipboardNativeApi = clipboardNativeApi;
		}

		public IClipboardData BuildData(uint format, byte[] data)
		{
			if (!CanBuildData(format))
			{
				throw new ArgumentException(
					"Can't construct data from this format.",
					nameof(format));
			}

			var text = GetProcessedTextFromRawData(format, data);
			return new ClipboardTextData(dataSourceService) {
				Text = text,
				RawData = data,
				RawFormat = format
			};
		}

		string GetProcessedTextFromRawData(uint format, byte[] data)
		{
			var text = GetTextFromRawData(format, data);

			var terminaryNullCharacterPosition = text.IndexOf('\0');
			if (terminaryNullCharacterPosition > -1)
			{
				return text.Substring(0, terminaryNullCharacterPosition);
			}
			return text;
		}

		string GetTextFromRawData(uint format, byte[] data)
		{
			switch (format)
			{
				case ClipboardNativeApi.CF_TEXT:
					return Encoding.UTF8.GetString(data);

				case ClipboardNativeApi.CF_OEMTEXT:
					return Encoding.Default.GetString(data);

				case ClipboardNativeApi.CF_UNICODETEXT:
					return Encoding.Unicode.GetString(data);

				default:
					switch (clipboardNativeApi.GetClipboardFormatName(format))
					{
						case RichTextFormat:
							return ConvertHtmlToText(
								ConvertRtfToHtml(Encoding.Default.GetString(data)));

						case HtmlFormat:
							return ConvertHtmlToText(
								Encoding.Default.GetString(data));

						default:
							throw new InvalidOperationException("Unknown format.");
					}
			}
		}

		static string ConvertHtmlToText(string html)
		{
			var document = new HtmlDocument();
			document.LoadHtml(html);

			return document.DocumentNode.InnerText;
		}

		static string ConvertRtfToHtml(string rtfCode)
		{
			return Rtf.ToHtml(rtfCode);
		}

		public bool CanBuildData(uint format)
		{
			var isCommonFormat =
				(format == ClipboardNativeApi.CF_TEXT) ||
				(format == ClipboardNativeApi.CF_OEMTEXT) ||
				(format == ClipboardNativeApi.CF_UNICODETEXT);
			if (isCommonFormat)
				return true;

			var name = clipboardNativeApi.GetClipboardFormatName(format);
			return name == RichTextFormat || name == HtmlFormat;
		}
	}
}