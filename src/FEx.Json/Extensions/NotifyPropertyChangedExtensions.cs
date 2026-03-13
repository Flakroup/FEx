using FEx.Agnostics.Abstractions.Interfaces;
using Newtonsoft.Json;
using System;
using System.Runtime.CompilerServices;

namespace FEx.Json.Extensions;

public static class NotifyPropertyChangedExtensions
{
    public static bool SetPropertyAndDeserializeIt<TEntity>(this IFExNotifyPropertyChanged obj,
                                                            ref string backingField,
                                                            string jsonValue,
                                                            Action<TEntity> onDeserializedAction = null,
                                                            JsonSerializerSettings settings = null,
                                                            [CallerMemberName] string propertyName = null) =>
        obj.SetProperty(ref backingField,
            jsonValue.TrimJsonString(),
            onDeserializedAction is not null
                ? v => onDeserializedAction(v.FromJson<TEntity>(settings))
                : null,
            propertyName);
}