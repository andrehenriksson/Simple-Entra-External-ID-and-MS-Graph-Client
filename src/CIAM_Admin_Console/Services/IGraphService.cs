using Microsoft.Graph.Models;

namespace CIAM_Admin_Console.Services;

public interface IGraphService
{
    Task<User> CreateUserAsync(string displayName, string mailNickname, string userPrincipalName, string password);
    Task<List<User>> ListUsersAsync(int top = 10);
    Task<User?> GetUserAsync(string userIdOrPrincipalName);
    Task<Application> CreateOidcApplicationAsync(string displayName, List<string> redirectUris);
}
