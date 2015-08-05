using System;
using System.ComponentModel;
using System.Windows;
using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer;

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

                while(text.Contains("  "))
                {
                    text = text.Replace("  ", " ");
                }

                return text;
            }
        }
    }
}
