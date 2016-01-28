namespace Shapeshifter.WindowsDesktop.Controls.Clipboard.ViewModels
{
    using Data.Interfaces;

    using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ClipboardFileDataViewModelTest: UnitTestFor<IClipboardDataViewModel<IClipboardTextData>> { }
}