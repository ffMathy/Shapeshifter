using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Events
{
    [ExcludeFromCodeCoverage]
    public class DataCopiedEventArgument
    {
        public DataCopiedEventArgument(IDataObject data)
        {
            Data = data;
        }

        public IDataObject Data { get; private set; }
    }
}
