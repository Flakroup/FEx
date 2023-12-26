using FEx.LiteDbx.Abstractions.Interfaces;
using FEx.LiteDbx.Services;
using StrongInject;

namespace FEx.LiteDbx;

[Register(typeof(LiteDBService), typeof(ILiteDBService))]
[Register(typeof(DatabaseProvider), Scope.SingleInstance, typeof(IDatabaseProvider))]
[Register(typeof(DatabaseFilePathResolver), typeof(IDatabaseFilePathResolver))]
[Register(typeof(LiteDbFileLocalStorageService), typeof(IFileLocalStorageService))]
public class FExLiteDbxModule
{
    [Instance]
    public static IClearCache ClearCacheInstance => null;
}