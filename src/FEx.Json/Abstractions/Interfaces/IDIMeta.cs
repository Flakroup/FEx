using System;

namespace FEx.Json.Abstractions.Interfaces;

public interface IDIMeta
{
    bool IsRegistred(Type t);
    Type RegistredTypeFor(Type t);
}