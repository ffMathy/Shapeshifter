namespace Shapeshifter.Core.Strategies
{
    interface IImageConstructionStrategy<TDataType>
    {
        byte[] ConstructImage(TDataType data);
    }
}
