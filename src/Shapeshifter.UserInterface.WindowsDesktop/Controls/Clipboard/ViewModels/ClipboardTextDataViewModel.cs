using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Autofac;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Facades;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.Designer.Helpers;
using Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels
{
    internal class ClipboardTextDataViewModel : ClipboardDataViewModel<IClipboardTextData>, IClipboardTextDataViewModel
    {
        private static readonly Regex whitespaceSubstitutionExpression;

        static ClipboardTextDataViewModel()
        {
            whitespaceSubstitutionExpression = new Regex(@"\s+", RegexOptions.Compiled);
        }

        [ExcludeFromCodeCoverage]
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
        private void PrepareDesignMode()
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