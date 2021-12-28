using System.Collections;
using System.Runtime.Serialization;

namespace FEx.Logging.Abstractions;

public interface ILoggerState : IDictionary<string, object>, IDictionary, IReadOnlyDictionary<string, object>, ISerializable, IDeserializationCallback
{
    void AddOrUpdateLabel(string key, object value);
    void RemoveLabel(string key);
}