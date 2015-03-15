using Shapeshifter.Core.Helpers;
using Shapeshifter.Core.Strategies;
using System;
using Autofac;
using Shapeshifter.Core.Data;
using Shapeshifter.Core.Controls;

namespace Shapeshifter.Core.Builders
{
    public abstract class ClipboardDataControlBuilderBase<TControlType, TDataType> : IClipboardDataControlBuilder<IClipboardItemControl<TControlType>>
        where TDataType : IClipboardData
    {
        protected abstract IControlConstructionStrategy<TControlType, TDataType> HeaderBuildingStrategy { get; }
        protected abstract IControlConstructionStrategy<TControlType, TDataType> BodyBuildingStrategy { get; }
        protected abstract IControlConstructionStrategy<TControlType, TDataType> SourceBuildingStrategy { get; }
        protected abstract IControlConstructionStrategy<TControlType, TDataType> BackgroundImageBuildingStrategy { get; }

        protected ClipboardDataControlBuilderBase()
        {
            //constructor of an abstract class should be protected.
        }

        public IClipboardItemControl<TControlType> Build(IClipboardData incomingData)
        {
            if (!CanBuild(incomingData))
            {
                throw new InvalidOperationException("Data format not supported.");
            }

            if (HeaderBuildingStrategy == null || BodyBuildingStrategy == null || SourceBuildingStrategy == null)
            {
                throw new InvalidOperationException("Both the header, source and body building strategies must be available before calling Build.");
            }

            var data = (TDataType)incomingData;

            var header = HeaderBuildingStrategy.ConstructControl(data);
            var body = BodyBuildingStrategy.ConstructControl(data);
            var source = SourceBuildingStrategy.ConstructControl(data);

            var container = InversionOfControlHelper.Container;

            var newControl = container.Resolve<IClipboardItemControl<TControlType>>();

            newControl.Header = header;
            newControl.Body = body;
            newControl.Source = source;

            if (BackgroundImageBuildingStrategy != null)
            {
                var backgroundImage = BackgroundImageBuildingStrategy.ConstructControl(data);
                newControl.BackgroundImage = backgroundImage;
            }

            return newControl;
        }
        
        public virtual bool CanBuild(IClipboardData data)
        {
            return data != null && data.GetType() == typeof(TDataType);
        }
    }
}
