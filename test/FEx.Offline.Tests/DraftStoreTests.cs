using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Offline.Tests;

/// <summary>Autosave: a half-typed draft survives; submitting clears it; junk never crashes.</summary>
public sealed class DraftStoreTests
{
    [Fact]
    public async Task SavedDraft_RoundTrips()
    {
        DraftStore drafts = new(new InMemoryKeyValueStore());

        await drafts.SaveAsync("new-inquiry", new InquiryDraft("Jan", "Kowalski", "jan@k.pl"));
        var loaded = await drafts.LoadAsync<InquiryDraft>("new-inquiry");

        loaded.ShouldBe(new("Jan", "Kowalski", "jan@k.pl"));
    }

    [Fact]
    public async Task SavingAgain_OverwritesThePreviousDraft()
    {
        DraftStore drafts = new(new InMemoryKeyValueStore());

        await drafts.SaveAsync("new-inquiry", new InquiryDraft("Jan", "K", null));
        await drafts.SaveAsync("new-inquiry", new InquiryDraft("Janina", "Kowalska", "j@k.pl"));

        (await drafts.LoadAsync<InquiryDraft>("new-inquiry"))!.FirstName.ShouldBe("Janina");
    }

    [Fact]
    public async Task ClearedDraft_IsGone()
    {
        DraftStore drafts = new(new InMemoryKeyValueStore());
        await drafts.SaveAsync("new-inquiry", new InquiryDraft("Jan", "K", null));

        await drafts.ClearAsync("new-inquiry");

        (await drafts.LoadAsync<InquiryDraft>("new-inquiry")).ShouldBeNull();
    }

    [Fact]
    public async Task UnparsableStoredDraft_LoadsAsNothing_AndIsRemoved()
    {
        InMemoryKeyValueStore store = new();
        await store.SetAsync("draft:new-inquiry", "{not json");
        DraftStore drafts = new(store);

        (await drafts.LoadAsync<InquiryDraft>("new-inquiry")).ShouldBeNull();
        (await store.GetAsync("draft:new-inquiry")).ShouldBeNull();
    }

    private sealed record InquiryDraft(string FirstName, string LastName, string? Email);
}