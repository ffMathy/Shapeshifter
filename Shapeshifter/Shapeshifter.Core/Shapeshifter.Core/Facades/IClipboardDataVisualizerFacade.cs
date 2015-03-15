namespace Shapeshifter.Core.Facades
{
    interface IClipboardDataVisualizerFacade<TControlType, TDataType> 
        where TDataType : IClipboardData
    {
        TControlType CreateControl(TDataType data);
    }
}
