using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment.Interfaces;
using System.Windows;

namespace Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Environment
{
    class EnvironmentInformation : IEnvironmentInformation
    {
        public bool IsInDesignTime
        {
            get
            {
                return !(Application.Current is App);
            }
        }
    }
}
