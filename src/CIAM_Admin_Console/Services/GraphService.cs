using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using CIAM_Admin_Console.Models;

namespace CIAM_Admin_Console.Services;

public class GraphService : IGraphService
{
    private readonly GraphServiceClient _graphClient;
    private readonly AzureAdSettings _settings;

    public GraphService(AzureAdSettings settings)
    {
        _settings = settings;

        // Use ClientSecretCredential for app-only authentication
        var credential = new ClientSecretCredential(
            settings.TenantId,
            settings.ClientId,
            settings.ClientSecret
        );

        _graphClient = new GraphServiceClient(credential);
    }

    public async Task<User> CreateUserAsync(string displayName, string mailNickname, string userPrincipalName, string password)
    {
        var requestBody = new User
        {
            AccountEnabled = true,
            DisplayName = displayName,
            MailNickname = mailNickname,

            // Note: UserPrincipalName is set via Identities for email sign-in
            // UserPrincipalName = userPrincipalName,

            Identities = new List<ObjectIdentity>
            {
                new ObjectIdentity
                {
                    SignInType = "emailAddress",
                    Issuer = _settings.TenantName,
                    IssuerAssignedId = userPrincipalName
                }
            },
            PasswordProfile = new PasswordProfile
            {
                ForceChangePasswordNextSignIn = false,
                Password = password
            }
        };

        Console.WriteLine("requestBody: " + requestBody);

        var result = await _graphClient.Users.PostAsync(requestBody);
        return result ?? throw new Exception("Failed to create user");
    }

    public async Task<List<User>> ListUsersAsync(int top = 10)
    {
        var result = await _graphClient.Users
            .GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Top = top;
                requestConfiguration.QueryParameters.Select = new[] { "id", "displayName", "userPrincipalName", "mail", "jobTitle", "department", "accountEnabled, identities" };
                requestConfiguration.QueryParameters.Orderby = new[] { "displayName" };
            });

        return result?.Value?.ToList() ?? new List<User>();
    }

    public async Task<User?> GetUserAsync(string userIdOrPrincipalName)
    {
        try
        {
            var user = await _graphClient.Users[userIdOrPrincipalName]
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Select = new[] { "id", "displayName", "userPrincipalName", "mail", "jobTitle", "department", "accountEnabled", "createdDateTime" };
                });

            return user;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<Application> CreateOidcApplicationAsync(string displayName, List<string> redirectUris)
    {
        var requestBody = new Application
        {
            DisplayName = displayName,
            SignInAudience = "AzureADandPersonalMicrosoftAccount",
            Web = new WebApplication
            {
                RedirectUris = redirectUris,
                ImplicitGrantSettings = new ImplicitGrantSettings
                {
                    EnableIdTokenIssuance = true,
                    EnableAccessTokenIssuance = false
                }
            },
            RequiredResourceAccess = new List<RequiredResourceAccess>
            {
                new RequiredResourceAccess
                {
                    ResourceAppId = "00000003-0000-0000-c000-000000000000", // Microsoft Graph
                    ResourceAccess = new List<ResourceAccess>
                    {
                        new ResourceAccess
                        {
                            Id = Guid.Parse("e1fe6dd8-ba31-4d61-89e7-88639da4683d"), // User.Read
                            Type = "Scope"
                        }
                    }
                }
            }
        };

        var result = await _graphClient.Applications.PostAsync(requestBody);
        return result ?? throw new Exception("Failed to create application");
    }
}
