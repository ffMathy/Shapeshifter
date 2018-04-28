namespace Shapeshifter.WindowsDesktop.Data.Factories.Interfaces
{
    using Data.Interfaces;

    public interface IClipboardDataFactory
    {
		int Priority { get; }

		bool CanBuildData(uint format);

        IClipboardData BuildData(uint format, byte[] rawData);
    }
}