using Autofac;
using Shapeshifter.WindowsDesktop.Infrastructure.Dependencies.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shapeshifter.WindowsDesktop.Controls.Window.Interfaces
{
	public interface ISourceClipboardQuantityOverlay :
          IDisposable,
		  IWindow,
          ISingleInstance
	{
	}
}
