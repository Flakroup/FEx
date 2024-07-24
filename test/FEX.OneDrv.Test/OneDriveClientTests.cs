using FEx.OneDrv;
using System.Threading.Tasks;
using Xunit;

namespace FEX.OneDrv.Test;

public class OneDriveClientTests
{
    private readonly OneDriveClient _sut;

    public OneDriveClientTests()
    {
        _sut = new OneDriveClient();
    }

    [Fact]
    public async Task ListDrivesTest()
    {
        await _sut.ListDrivesAsync();
    }
}