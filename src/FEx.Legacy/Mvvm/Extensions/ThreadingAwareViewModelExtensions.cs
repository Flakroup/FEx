using FEx.Legacy.Mvvm.Abstractions.Interfaces;

namespace FEx.Legacy.Mvvm.Extensions;

public static class ThreadingAwareViewModelExtensions
{
    public static void PostMainJob(this IThreadingAwareViewModel viewModel) =>
        viewModel.PostMainJob(true);
}
