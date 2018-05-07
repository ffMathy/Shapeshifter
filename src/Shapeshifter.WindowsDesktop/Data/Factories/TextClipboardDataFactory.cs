namespace Shapeshifter.WindowsDesktop.Data.Factories
{
	using System;
	using System.Text;

	using Data.Interfaces;
	using HtmlAgilityPack;
	using Interfaces;

	using Native;
	using RtfPipe;

	class TextClipboardDataFactory : ITextClipboardDataFactory
	{
		const string RichTextFormat = "Rich Text Format";
		const string HtmlFormat = "HTML Format";

		public IClipboardData BuildData(IClipboardFormat format, byte[] data)
		{
			if (!CanBuildData(format))
			{
				throw new ArgumentException(
					"Can't construct data from this format.",
					nameof(format));
			}

			var text = GetProcessedTextFromRawData(format, data);
			if(text == null)
				return null;

			return new ClipboardTextData() {
				Text = text.Trim(),
				RawData = data,
				RawFormat = format
			};
		}

		string GetProcessedTextFromRawData(IClipboardFormat format, byte[] data)
		{
			var text = GetTextFromRawData(format, data);
			if(text == null)
				return null;

			var terminaryNullCharacterPosition = text.IndexOf('\0');
			if (terminaryNullCharacterPosition > -1)
			{
				text = text.Substring(0, terminaryNullCharacterPosition);
			}
			
			return HtmlEntity.DeEntitize(text);
		}

		string GetTextFromRawData(IClipboardFormat format, byte[] data)
		{
			switch (format.Number)
			{
				case ClipboardNativeApi.CF_TEXT:
					return Encoding.UTF8.GetString(data);

				case ClipboardNativeApi.CF_OEMTEXT:
					return Encoding.Default.GetString(data);

				case ClipboardNativeApi.CF_UNICODETEXT:
					return Encoding.Unicode.GetString(data);

				default:
					var text = Encoding.UTF8.GetString(data);
					switch (format.Name)
					{
						case RichTextFormat:
							return ConvertHtmlToText(
								ConvertRtfToHtml(text));

						case HtmlFormat:
							return ConvertHtmlToText(text);

						default:
							throw new InvalidOperationException("Unknown format.");
					}
			}
		}

		static string ConvertHtmlToText(string html)
		{
			if(html == null)
				return null;

			if(!html.Contains("<"))
				return html;

			var document = new HtmlDocument();
			document.LoadHtml(html.Substring(html.IndexOf("<")));

			return document.DocumentNode.InnerText;
		}

		static string ConvertRtfToHtml(string rtfCode)
		{
			if(!rtfCode.StartsWith(@"{\rtf"))
				return null;

			const string missingVersionString = @"{\rtf\";
			if (rtfCode.StartsWith(missingVersionString))
				rtfCode = @"{\rtf1\" + rtfCode.Substring(missingVersionString.Length);

			return Rtf.ToHtml(rtfCode);
		}

		public bool CanBuildData(IClipboardFormat format)
		{
			var isCommonFormat =
				(format.Number == ClipboardNativeApi.CF_TEXT) ||
				(format.Number == ClipboardNativeApi.CF_OEMTEXT) ||
				(format.Number == ClipboardNativeApi.CF_UNICODETEXT);
			if (isCommonFormat)
				return true;
				
			return format.Name == RichTextFormat || format.Name == HtmlFormat;
		}
	}
}
