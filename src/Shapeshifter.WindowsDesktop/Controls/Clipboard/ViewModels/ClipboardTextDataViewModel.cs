namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.ViewModels
{
    using System;
    using System.Text.RegularExpressions;

    using Autofac;

    using Data.Interfaces;

    using Designer.Facades;
    using Designer.Helpers;

    using Infrastructure.Environment;
    using Infrastructure.Environment.Interfaces;

    using Interfaces;

    class ClipboardTextDataViewModel
        : ClipboardDataViewModel<IClipboardTextData>,
          IClipboardTextDataViewModel
    {
        static readonly Regex whitespaceSubstitutionExpression;

        static ClipboardTextDataViewModel()
        {
            whitespaceSubstitutionExpression = new Regex(@"\s+", RegexOptions.Compiled);
        }

        public ClipboardTextDataViewModel()
            : this(new EnvironmentInformation(true)) { }

        public ClipboardTextDataViewModel(
            IEnvironmentInformation environmentInformation)
        {
            if (environmentInformation.GetIsInDesignTime())
            {
                PrepareDesignMode();
            }
        }

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