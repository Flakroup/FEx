using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Abstractions.Models;
using FEx.MVVM.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Xunit;

namespace FEx.Imaging.Windows.Tests;

public sealed class LazyImagesHandlerTests : ImagingTestBase
{
    private readonly IFilesCacheService _cache = Substitute.For<IFilesCacheService>();

    private LazyImagesHandler CreateSut() => new(_cache);

    private readonly SemaphoreSlim _release = new(0);

    private void ReturnsImageAfter() =>
        _cache.GetImageAsync(Arg.Any<Uri>(),
                Arg.Any<WidthAndHeight?>(),
                Arg.Any<WebRequestParams?>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<HttpResponseMessage?>())
            .Returns(async _ =>
            {
                await _release.WaitAsync();

                return null;
            });

    [Fact]
    public void Constructor_ExposesTheCacheAndStartsEmpty()
    {
        var sut = CreateSut();

        sut.Cache.ShouldBeSameAs(_cache);
        sut.ImagesTasks.Count.ShouldBe(0);
    }

    [Fact]
    public async Task AddEnsureImageTask_RunsTheCallbacksAroundTheImageRequest()
    {
        var sut = CreateSut();
        var calls = new List<string>();
        ReturnsImageAfter();

        var added = sut.AddEnsureImageTask("Avatar",
            ImageUrl,
            _ => calls.Add("set"),
            beforeAction: () => calls.Add("before"),
            afterAction: () => calls.Add("after"));
        _release.Release();
        var succeeded = await sut.GetImageTaskAsync("Avatar");

        added.ShouldBeTrue();
        succeeded.ShouldBeTrue();
        calls.ShouldBe(["before", "set", "after"]);
        _ = _cache.Received(1).GetImageAsync(ImageUrl, forceLoad: true);
    }

    [Fact]
    public async Task AddEnsureImageTask_ForwardsTheRequestOptions()
    {
        var sut = CreateSut();
        var size = new WidthAndHeight(10, 20);
        var pars = new WebRequestParams();
        _cache.GetImageAsync(Arg.Any<Uri>(),
                Arg.Any<WidthAndHeight?>(),
                Arg.Any<WebRequestParams?>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<HttpResponseMessage?>())
            .Returns(Task.FromResult<BitmapImage?>(null));

        sut.AddEnsureImageTask("Avatar", ImageUrl, _ => { }, size, pars: pars, refresh: true, forceLoad: false, forceMemoryStream: true);
        await sut.GetImageTaskAsync("Avatar");

        _ = _cache.Received(1).GetImageAsync(ImageUrl, size, pars, true, false, true);
    }

    [Fact]
    public async Task AddEnsureImageTask_SameKeyWhileRunning_IsRejected_AndAcceptedAfterwards()
    {
        var sut = CreateSut();
        ReturnsImageAfter();

        sut.AddEnsureImageTask("Avatar", ImageUrl, _ => { }).ShouldBeTrue();
        sut.AddEnsureImageTask("Avatar", ImageUrl, _ => { }).ShouldBeFalse();
        sut.AddEnsureImageTask("Banner", ImageUrl, _ => { }).ShouldBeTrue();

        _release.Release(2);
        await sut.GetImageTaskAsync("Avatar");
        await sut.GetImageTaskAsync("Banner");

        sut.AddEnsureImageTask("Avatar", ImageUrl, _ => { }).ShouldBeTrue();
    }

    [Fact]
    public async Task AnyOtherImageTaskIsRunning_IgnoresTheAskingKeyAndFinishedTasks()
    {
        var sut = CreateSut();
        ReturnsImageAfter();
        sut.AddEnsureImageTask("Avatar", ImageUrl, _ => { });

        sut.AnyOtherImageTaskIsRunning("Avatar").ShouldBeFalse();
        sut.AnyOtherImageTaskIsRunning("Banner").ShouldBeTrue();

        _release.Release();
        await sut.GetImageTaskAsync("Avatar");

        sut.AnyOtherImageTaskIsRunning("Banner").ShouldBeFalse();
    }

    [Fact]
    public async Task AddEnsureImageTask_ServerAnswersWithAnErrorStatus_ReportsFailureQuietly()
    {
        var sut = CreateSut();
        var afterCalls = 0;
        _cache.GetImageAsync(Arg.Any<Uri>(),
                Arg.Any<WidthAndHeight?>(),
                Arg.Any<WebRequestParams?>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<HttpResponseMessage?>())
            .ThrowsAsync(new HttpStatusException(HttpStatusCode.NotFound, ImageUrl, "Not Found"));

        sut.AddEnsureImageTask("Avatar", ImageUrl, _ => Assert.Fail("no image expected"), afterAction: () => afterCalls++);
        var succeeded = await sut.GetImageTaskAsync("Avatar");

        succeeded.ShouldBeFalse();
        afterCalls.ShouldBe(1);
        ExceptionHandler.DidNotReceiveWithAnyArgs().Handle(default!);
    }

    [Fact]
    public async Task AddEnsureImageTask_AnyOtherFailure_IsHandledAndReportedAsFailure()
    {
        var sut = CreateSut();
        var failure = new InvalidOperationException("decoder blew up");
        _cache.GetImageAsync(Arg.Any<Uri>(),
                Arg.Any<WidthAndHeight?>(),
                Arg.Any<WebRequestParams?>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<HttpResponseMessage?>())
            .ThrowsAsync(failure);

        sut.AddEnsureImageTask("Avatar", ImageUrl, _ => { });
        var succeeded = await sut.GetImageTaskAsync("Avatar");

        succeeded.ShouldBeFalse();
        ExceptionHandler.Received(1).Handle(failure, Arg.Any<IExceptionHandlerOptions?>());
    }
}
