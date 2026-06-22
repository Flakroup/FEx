using FEx.EFCore.Interfaces;
using FEx.Imaging.Windows.Model;
using FEx.Legacy.Imaging.Abstractions.Interfaces;
using StrongInject;
using System.Diagnostics.CodeAnalysis;

namespace FEx.Imaging.Windows;

[SuppressMessage("ReSharper", "PossibleInterfaceMemberAmbiguity")]
public interface IWindowsImagingServicesContainer : IContainer<IFilesCacheServiceConfig>, IContainer<IIndexEntryConfig>,
    IContainer<IFilesCacheService>, IContainer<IEFCoreDatabaseBackedService<FilesCacheContext>>,
    IContainer<IndexEntriesCache>, IContainer<ICachedImageStorage>, IContainer<IFilesCacheServiceConfigurator>
{
}