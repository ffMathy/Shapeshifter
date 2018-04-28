namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.Factories.Interfaces
{
	using Clipboard.Interfaces;

	using Data.Interfaces;

	public interface IClipboardDataControlFactory
	{
		int Priority { get; }

		bool CanBuildControl(IClipboardDataPackage data);

		IClipboardControl BuildControl(IClipboardDataPackage clipboardData);
	}
}