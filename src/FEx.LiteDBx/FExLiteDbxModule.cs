using FEx.LiteDBx.Abstractions.Interfaces;
using FEx.LiteDBx.Services;
using StrongInject;

namespace FEx.LiteDBx;

[Register(typeof(LiteDBService), typeof(ILiteDBService))]
[Register(typeof(DatabaseProvider), Scope.SingleInstance, typeof(IDatabaseProvider))]
[Register(typeof(DatabaseFilePathResolver), typeof(IDatabaseFilePathResolver))]
[Register(typeof(LiteDbFileLocalStorageService), typeof(IFileLocalStorageService))]
public class FExLiteDbxModule
{
    [Instance]
    public static IClearCache ClearCacheInstance => null;
}