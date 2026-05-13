using FEx.OneDrv.Abstractions;
using StrongInject;
using System.Diagnostics.CodeAnalysis;

namespace FEx.OneDrv.DI;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IFExOneDrvContainer :
    IContainer<IOneDriveClient>,
    IContainer<IOneDriveAuthService>,
    IContainer<IOneDriveItemEnumerator>,
    IContainer<IOneDriveThumbnailService>
{
}
