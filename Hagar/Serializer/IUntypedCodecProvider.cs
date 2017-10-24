using System;
using Hagar.Codec;

namespace Hagar.Serializer
{
    public interface IUntypedCodecProvider
    {
        IFieldCodec<object> GetCodec(Type fieldType);
        IFieldCodec<object> TryGetCodec(Type fieldType);
    }

    public interface ITypedCodecProvider
    {
        IFieldCodec<TField> GetCodec<TField>();
        IFieldCodec<TField> TryGetCodec<TField>();
    }

    public interface IWrappedCodec
    {
        object InnerCodec { get; }
    }

    public interface IMultiCodec : IFieldCodec<object>
    {
        bool IsSupportedType(Type type);
    }
}