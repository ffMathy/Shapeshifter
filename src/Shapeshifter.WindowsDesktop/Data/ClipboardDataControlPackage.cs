namespace Shapeshifter.WindowsDesktop.Data
{
    using Controls.Clipboard.Interfaces;

    using Interfaces;

    public class ClipboardDataControlPackage: IClipboardDataControlPackage
    {
        public ClipboardDataControlPackage(
            IClipboardDataPackage data,
            IClipboardControl control)
        {
            Data = data;
            Control = control;
        }

        public IClipboardDataPackage Data { get; }
        public IClipboardControl Control { get; }

		public IClipboardDataControlPackage Clone()
		{
			return new ClipboardDataControlPackage(Data, Control.Clone());
		}
	}
}