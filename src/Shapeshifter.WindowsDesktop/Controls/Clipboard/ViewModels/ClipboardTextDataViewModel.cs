namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.ViewModels
{
    using System;

    using Autofac;

    using Data.Interfaces;

    using Designer.Facades;
    using Designer.Helpers;
    using Infrastructure.Environment.Interfaces;

    using Interfaces;

	class ClipboardTextDataViewModel
        : ClipboardDataViewModel<IClipboardTextData>,
          IClipboardTextDataViewModel
    {
        public ClipboardTextDataViewModel()
            : this(new Infrastructure.Environment.EnvironmentInformation(true)) { }

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
				text = text
					.Substring(0, Math.Min(text.Length, 512))
					.Replace("\r", " ")
					.Replace("\n", " ")
					.Replace("\t", " ");

				while (text.Contains("  "))
					text = text.Replace("  ", " ");

                return text;
            }
        }
    }
}