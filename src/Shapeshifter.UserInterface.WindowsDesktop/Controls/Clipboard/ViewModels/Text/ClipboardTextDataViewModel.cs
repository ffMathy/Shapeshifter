using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels
{
    class ClipboardTextDataViewModel : ClipboardDataViewModel<ClipboardTextData>
    {
        public ClipboardTextDataViewModel()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                Data = new DesignerClipboardTextDataFacade();
            }
        }

        public ClipboardTextDataViewModel(ClipboardTextData data) : base(data)
        {
        }

        public string FriendlyText
        {
            get
            {
                var text = Data
                    .Text
                    .Substring(0, Math.Min(Data.Text.Length, 512))
                    .Replace("\n", " ")
                    .Replace("\r", " ")
                    .Replace("\t", " ")
                    .Trim();

                // Combine all whitespaces
                text = Regex.Replace(text, @"\s+", " ");

                return text;
            }
        }
    }
}
