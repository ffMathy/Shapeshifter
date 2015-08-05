using Shapeshifter.Core.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
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
                var text = Data.Text.Trim();
                text = Regex.Replace(text, @"\s+", " ");
                text = text.Substring(0, Math.Min(text.Length, 512));

                return text;
            }
        }
    }
}
