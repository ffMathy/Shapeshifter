using Shapeshifter.Core.Helpers;
using Shapeshifter.Core.Strategies;
using System;
using Autofac;

namespace Shapeshifter.Core.Builders
{
    abstract class ClipboardDataControlBuilderBase<TControlType, TDataType> : IClipboardDataControlBuilder<IClipboardItemControl<TControlType>, TDataType>
        where TDataType : IClipboardData
    {
        protected abstract IControlConstructionStrategy<TControlType, TDataType> HeaderBuildingStrategy { get; }
        protected abstract IControlConstructionStrategy<TControlType, TDataType> BodyBuildingStrategy { get; }

        protected abstract IImageConstructionStrategy<TDataType> BackgroundImageBuildingStrategy { get; }

        protected ClipboardDataControlBuilderBase()
        {
            //constructor of an abstract class should be protected.
        }

        public IClipboardItemControl<TControlType> Build(TDataType data)
        {
            if (!CanBuild(data))
            {
                throw new InvalidOperationException("Data format not supported.");
            }

            if (HeaderBuildingStrategy == null || BodyBuildingStrategy == null)
            {
                throw new InvalidOperationException("Both the header and body building strategies must be set before calling Build.");
            }

            var header = HeaderBuildingStrategy.ConstructControl(data);
            var body = BodyBuildingStrategy.ConstructControl(data);

            var container = InversionOfControlHelper.Container;

            var newControl = container.Resolve<IClipboardItemControl<TControlType>>();

            newControl.Header = header;
            newControl.Body = body;

            if (BackgroundImageBuildingStrategy != null)
            {
                var backgroundImage = BackgroundImageBuildingStrategy.ConstructImage(data);
                newControl.BackgroundImage = backgroundImage;
            }

            return newControl;
        }

        public abstract bool CanBuild(TDataType data);
    }
}
