using Microsoft.Extensions.Configuration;
using CIAM_Admin_Console.Models;
using CIAM_Admin_Console.Services;

namespace CIAM_Admin_Console;

class Program
{
    private static IGraphService? _graphService;
    private static AzureAdSettings? _azureAdSettings;

    static async Task Main(string[] args)
    {
        Console.WriteLine("====================================");
        Console.WriteLine("   CIAM Admin Console");
        Console.WriteLine("   Entra External ID Management");
        Console.WriteLine("====================================\n");

        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .Build();

        var azureAdSettings = new AzureAdSettings();
        configuration.GetSection("AzureAd").Bind(azureAdSettings);
        _azureAdSettings = azureAdSettings;

        // Validate configuration
        if (string.IsNullOrEmpty(azureAdSettings.TenantId) || 
            string.IsNullOrEmpty(azureAdSettings.ClientId) || 
            string.IsNullOrEmpty(azureAdSettings.ClientSecret))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: Azure AD configuration is missing!");
            Console.WriteLine("Please configure TenantId, ClientId, and ClientSecret in appsettings.json");
            Console.ResetColor();
            return;
        }

        // Initialize Graph Service
        _graphService = new GraphService(azureAdSettings);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✓ Connected to Entra External ID");
        Console.ResetColor();
        Console.WriteLine($"Tenant: {azureAdSettings.TenantId}\n");

        // Main menu loop
        bool exit = false;
        while (!exit)
        {
            DisplayMenu();
            var choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await CreateUserAsync();
                        break;
                    case "2":
                        await ListUsersAsync();
                        break;
                    case "3":
                        await GetUserAsync();
                        break;
                    case "4":
                        await CreateOidcAppAsync();
                        break;
                    case "5":
                        exit = true;
                        Console.WriteLine("\nGoodbye!");
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("\nInvalid option. Please try again.");
                        Console.ResetColor();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nError: {ex.Message}");
                Console.ResetColor();
            }

            if (!exit)
            {
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                Console.Clear();
            }
        }
    }

    static void DisplayMenu()
    {
        Console.WriteLine("\n╔══════════════════════════════════════╗");
        Console.WriteLine("║         MAIN MENU                    ║");
        Console.WriteLine("╠══════════════════════════════════════╣");
        Console.WriteLine("║  1. Create a new Identity            ║");
        Console.WriteLine("║  2. List users in directory          ║");
        Console.WriteLine("║  3. Read user information            ║");
        Console.WriteLine("║  4. Create OIDC application          ║");
        Console.WriteLine("║  5. Exit                             ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
        Console.Write("\nSelect an option (1-5): ");
    }

    static async Task CreateUserAsync()
    {
        Console.WriteLine("\n--- Create New Identity ---");

        Console.Write("Display Name: ");
        var displayName = Console.ReadLine() ?? "";

        Console.Write("Mail Nickname (e.g., john.doe): ");
        var mailNickname = Console.ReadLine() ?? "";

        Console.Write("Email (e.g., name@domain.com): ");
        var userPrincipalName = Console.ReadLine() ?? "";

        Console.Write("Password: ");
        var password = ReadPassword();

        Console.WriteLine("\nCreating user...");
        var user = await _graphService!.CreateUserAsync(displayName, mailNickname, userPrincipalName, password);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n✓ User created successfully!");
        Console.ResetColor();
        Console.WriteLine($"  User ID: {user.Id}");
        Console.WriteLine($"  Display Name: {user.DisplayName}");
        Console.WriteLine($"  UPN: {user.UserPrincipalName}");
    }

    static async Task ListUsersAsync()
    {
        Console.WriteLine("\n--- List Users ---");
        Console.Write("Number of users to retrieve (default 10): ");
        var topInput = Console.ReadLine();
        int top = string.IsNullOrEmpty(topInput) ? 10 : int.Parse(topInput);

        Console.WriteLine($"\nRetrieving top {top} users...");
        var users = await _graphService!.ListUsersAsync(top);

        Console.WriteLine($"\nFound {users.Count} user(s):\n");
        Console.WriteLine("┌──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ Display Name                     │ User Principal Name                                  │ Email                                                │ Enabled │");
        Console.WriteLine("├──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤");

        foreach (var user in users)
        {
            var displayName = (user.DisplayName ?? "").PadRight(32).Substring(0, 32);
            var upn = (user.UserPrincipalName ?? "").PadRight(52).Substring(0, 52);
            var email = (user.Identities?.FirstOrDefault()?.IssuerAssignedId ?? "").PadRight(52).Substring(0, 52);
            var enabled = user.AccountEnabled == true ? "Yes" : "No ";

            Console.WriteLine($"│ {displayName} │ {upn} │ {email} │ {enabled}     │");
        }

        Console.WriteLine("└──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘");
    }

    static async Task GetUserAsync()
    {
        Console.WriteLine("\n--- Read User Information ---");
        Console.Write("Enter User ID or User Principal Name: ");
        var userIdentifier = Console.ReadLine() ?? "";

        Console.WriteLine("\nRetrieving user information...");
        var user = await _graphService!.GetUserAsync(userIdentifier);

        if (user == null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n⚠ User not found.");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n--- User Details ---");
        Console.ResetColor();
        Console.WriteLine($"ID:                  {user.Id}");
        Console.WriteLine($"Display Name:        {user.DisplayName}");
        Console.WriteLine($"User Principal Name: {user.UserPrincipalName}");
        Console.WriteLine($"Email:               {user.Mail ?? "N/A"}");
        Console.WriteLine($"Job Title:           {user.JobTitle ?? "N/A"}");
        Console.WriteLine($"Department:          {user.Department ?? "N/A"}");
        Console.WriteLine($"Account Enabled:     {(user.AccountEnabled == true ? "Yes" : "No")}");
        Console.WriteLine($"Created:             {user.CreatedDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}");
    }

    static async Task CreateOidcAppAsync()
    {
        Console.WriteLine("\n--- Create OIDC Application ---");

        Console.Write("Application Display Name: ");
        var displayName = Console.ReadLine() ?? "";

        Console.WriteLine("\nEnter redirect URIs (one per line, press Enter on empty line to finish):");
        var redirectUris = new List<string>();
        while (true)
        {
            Console.Write($"  Redirect URI #{redirectUris.Count + 1}: ");
            var uri = Console.ReadLine();
            if (string.IsNullOrEmpty(uri))
                break;
            redirectUris.Add(uri);
        }

        if (redirectUris.Count == 0)
        {
            redirectUris.Add("http://localhost:3000/callback");
            Console.WriteLine("  (Using default: http://localhost:3000/callback)");
        }

        Console.WriteLine("\nCreating OIDC application...");
        var app = await _graphService!.CreateOidcApplicationAsync(displayName, redirectUris);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n✓ OIDC Application created successfully!");
        Console.ResetColor();
        Console.WriteLine($"  Application ID: {app.AppId}");
        Console.WriteLine($"  Object ID:      {app.Id}");
        Console.WriteLine($"  Display Name:   {app.DisplayName}");
        Console.WriteLine($"\n  Redirect URIs:");
        foreach (var uri in redirectUris)
        {
            Console.WriteLine($"    - {uri}");
        }
        Console.WriteLine($"\n  Note: You may need to create a client secret in the Azure Portal.");
    }

    static string ReadPassword()
    {
        var password = "";
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
                break;
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password.Substring(0, password.Length - 1);
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password += key.KeyChar;
                Console.Write("*");
            }
        }
        Console.WriteLine();
        return password;
    }
}
