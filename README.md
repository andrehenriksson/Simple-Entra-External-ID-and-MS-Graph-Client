# Simple Entra External ID and MS Graph Client

A console application for managing Microsoft Entra External ID (formerly Azure AD B2C) identities and applications.

## Features

1. **Create New Identity** - Create new users in your Entra External ID tenant
2. **List Users** - View users in your directory with pagination
3. **Read User Information** - Get detailed information about specific users
4. **Create OIDC Application** - Register new OpenID Connect applications

## Prerequisites

- .NET 9.0 SDK
- Azure subscription with Entra External ID tenant
- App registration with the following Microsoft Graph API permissions:
  - `User.ReadWrite.All` (Application permission)
  - `Application.ReadWrite.All` (Application permission)

## Configuration

1. Update `appsettings.json` with your Azure AD credentials:

```json
{
  "AzureAd": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

## Running the Application

```bash
cd CIAM_Admin_Console
dotnet run
```

## Technical Details

### APIs and SDKs Used

- **Microsoft Graph API** - Unified API for Microsoft 365 and Azure AD operations
- **Microsoft.Graph SDK (v5.x)** - Official .NET SDK for Microsoft Graph
- **Azure.Identity** - Modern authentication library for Azure services
- **Client Credentials Flow** - App-only authentication with service principal

### Project Structure

```
CIAM_Admin_Console/
├── Program.cs              # Entry point and menu system
├── Services/
│   ├── IGraphService.cs    # Service interface
│   └── GraphService.cs     # Graph API implementation
├── Models/
│   ├── AzureAdSettings.cs  # Configuration model
│   └── UserInfo.cs         # User data model
└── appsettings.json        # Configuration file
```

## Permissions Required

The app registration needs the following **Application** permissions (not delegated):

- Microsoft Graph API:
  - `User.ReadWrite.All` - To create and read users
  - `Application.ReadWrite.All` - To create OIDC applications

Remember to grant admin consent for these permissions in the Azure Portal.
