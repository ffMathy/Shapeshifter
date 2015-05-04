using System.Windows;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services.Events
{
    public class DataCopiedEventArgument
    {
        public DataCopiedEventArgument(IDataObject data)
        {
            this.Data = data;
        }

        public IDataObject Data { get; private set; }
    }
}
