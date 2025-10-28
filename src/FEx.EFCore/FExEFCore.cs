using FEx.Agnostics.Abstractions;
using FEx.EFCore.Interfaces;
using System.Threading.Tasks;

namespace FEx.EFCore;

public class FExEFCore : FExInitializable
{
    private readonly ISqlDbHelper _sqlDbHelper;

    public FExEFCore(ISqlDbHelper sqlDbHelper)
    {
        _sqlDbHelper = sqlDbHelper;
    }

    public async Task CompleteInitializationAsync() => await _sqlDbHelper.InitializeAsync();

    protected override void OnInitialize()
    {
        // Synchronous initialization if needed
    }
}