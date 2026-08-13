using FEx.Flurlx.Abstractions.Interfaces;
using FEx.Flurlx.Models;
using FEx.Flurlx.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FEx.Sample.Avalonia.Services;

/// <summary>
/// API client for JSONPlaceholder (https://jsonplaceholder.typicode.com).
/// Demonstrates FEx.Flurlx usage with Polly resilience policies.
/// </summary>
public class JsonPlaceholderApi : FlurlApiBase
{
    public JsonPlaceholderApi(IFlurlConfigurator flurlConfigurator)
        : base(flurlConfigurator)
    {
    }

    /// <summary>
    /// Gets a list of users from JSONPlaceholder API.
    /// </summary>
    public async Task<List<User>> GetUsersAsync() =>
        await GetResponseAsync<List<User>, object>("/users", method: RequestMethod.GET);

    /// <summary>
    /// Gets a list of posts from JSONPlaceholder API.
    /// </summary>
    public async Task<List<Post>> GetPostsAsync() =>
        await GetResponseAsync<List<Post>, object>("/posts", method: RequestMethod.GET);

    /// <summary>
    /// Gets posts for a specific user.
    /// </summary>
    public async Task<List<Post>> GetUserPostsAsync(int userId) =>
        await GetResponseAsync<List<Post>, object>($"/posts?userId={userId}", method: RequestMethod.GET);
}

/// <summary>
/// User model from JSONPlaceholder.
/// </summary>
public record User
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

/// <summary>
/// Post model from JSONPlaceholder.
/// </summary>
public record Post
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
}