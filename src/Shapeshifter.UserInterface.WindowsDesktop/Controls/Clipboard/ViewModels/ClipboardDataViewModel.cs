using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shapeshifter.Core.Data;

namespace Shapeshifter.UserInterface.WindowsDesktop.Controls.Clipboard.ViewModels
{
    abstract class ClipboardDataViewModel<TClipboardData> where TClipboardData : IClipboardData
    {
        protected ClipboardDataViewModel() { }

        protected ClipboardDataViewModel(TClipboardData data)
        {
            this.Data = data;
        }

        public TClipboardData Data { get; protected set; }
    }
}
