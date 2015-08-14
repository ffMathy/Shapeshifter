using Autofac;
using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.Text.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels
{
    class ClipboardTextDataViewModel : ClipboardDataViewModel<IClipboardTextData>, IClipboardTextDataViewModel
    {
        private static readonly Regex whitespaceSubstitutionExpression;

        static ClipboardTextDataViewModel()
        {
            whitespaceSubstitutionExpression = new Regex(@"\s+", RegexOptions.Compiled);
        }

        public ClipboardTextDataViewModel()
        {
            PrepareDesignMode();
        }

        [ExcludeFromCodeCoverage]
        private void PrepareDesignMode()
        {
            if (App.InDesignMode)
            {
                Data = App.Container.Resolve<DesignerClipboardTextDataFacade>();
            }
        }

        public string FriendlyText
        {
            get
            {
                var text = Data.Text.Trim();
                text = whitespaceSubstitutionExpression.Replace(text, " ");
                text = text.Substring(0, Math.Min(text.Length, 512));

                return text;
            }
        }
    }
}
