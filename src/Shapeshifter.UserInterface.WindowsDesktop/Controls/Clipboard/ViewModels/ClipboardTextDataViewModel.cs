using Autofac;
using Shapeshifter.Core.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Helpers;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.Text.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels
{
    class ClipboardTextDataViewModel : ClipboardDataViewModel<IClipboardTextData>, IClipboardTextDataViewModel
    {
        static readonly Regex whitespaceSubstitutionExpression;

        static ClipboardTextDataViewModel()
        {
            whitespaceSubstitutionExpression = new Regex(@"\s+", RegexOptions.Compiled);
        }

        public ClipboardTextDataViewModel() : this(new EnvironmentInformation())
        {
        }

        public ClipboardTextDataViewModel(
            IEnvironmentInformation environmentInformation)
        {
            if (environmentInformation.IsInDesignTime)
            {
                PrepareDesignMode();
            }
        }

        [ExcludeFromCodeCoverage]
        void PrepareDesignMode()
        {
            var container = DesignTimeContainerHelper.CreateDesignTimeContainer();
            Data = container.Resolve<DesignerClipboardTextDataFacade>();
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
