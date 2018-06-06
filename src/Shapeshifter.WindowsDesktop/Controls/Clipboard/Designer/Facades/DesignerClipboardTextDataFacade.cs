namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Designer.Facades
{
    using Data;
    using Data.Interfaces;

	class DesignerClipboardTextDataFacade
        : ClipboardTextData,
          IClipboardTextData
    {
        public DesignerClipboardTextDataFacade(
		)
            :
                base()
        {
            Text =
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed eu purus vehicula, tincidunt velit eget, varius quam. Duis sollicitudin ultrices ipsum, et mollis tellus convallis vitae. Proin lobortis sapien eget varius imperdiet. In hac habitasse platea dictumst. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas.";
        }
    }
}