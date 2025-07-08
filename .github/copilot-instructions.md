<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

# Copilot Instructions for ApimSample Project

This project demonstrates integration between a .NET Core 8.0 API protected by OAuth 2.0 and an MVC client application, using Azure API Management.

## Project Structure
- **ApimSample.Api**: A .NET Core 8.0 API secured with OAuth 2.0, no top-level statements
- **ApimSample.MvcClient**: A .NET Core 8.0 MVC application that calls the API through Azure API Management
- **README.md**: Detailed setup instructions for Azure AD app registrations and API Management

## Development Guidelines
- Follow secure coding practices when modifying the authentication flow
- Use built-in ASP.NET Core authentication/authorization mechanisms
- Avoid hardcoding sensitive information like connection strings or tokens
- Use environment variables or the Azure Key Vault for sensitive configuration
- Keep OAuth 2.0 and API Management configuration details up to date in documentation
- Prefer dependency injection and interface-based programming for testability
