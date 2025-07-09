# Azure API Management Sample with OAuth 2.0

This sample demonstrates two approaches to securing APIs with OAuth 2.0 in Azure API Management and accessing them from an MVC client application.

## Solution Structure

- **ApimSample.Api**: .NET Core 8.0 Web API secured with built-in OAuth 2.0 (Direct Auth)
- **ApimSample.ApimSecuredApi**: .NET Core 8.0 Web API with security handled by APIM (APIM Auth)
- **ApimSample.MvcClient**: .NET Core 8.0 MVC client application that calls both APIs through Azure API Management

## Authentication Approaches

This sample demonstrates two distinct approaches for implementing OAuth 2.0 authentication with Azure API Management:

### 1. Direct Auth (ApimSample.Api)

In this approach:
- The API has built-in authentication middleware to validate JWT tokens
- API Management passes the OAuth token through to the API 
- The API itself performs token validation
- Appropriate for scenarios where you need fine-grained control over authentication within your API

```
┌─────────┐     Authorization    ┌─────────┐    JWT Token    ┌────────────┐    JWT Token    ┌──────────┐
│  User   │ ─────────────────────> Azure AD <────────────────┤ API Client │────────────────> API Mgmt  │
└─────────┘                      └─────────┘                 └────────────┘                 └─────┬────┘
                                                                                                 │
                                                                                                 │ JWT Token
                                                                                                 │ (pass-through)
                                                                                                 │
                                                                                                 ▼
                                                                                            ┌──────────┐
                                                                                            │  API     │
                                                                                            │(validates│
                                                                                            │  token)  │
                                                                                            └──────────┘
```

### 2. APIM Auth (ApimSample.ApimSecuredApi)

In this approach:
- The API has no built-in authentication logic
- API Management validates the JWT tokens using policies
- The API receives pre-validated requests
- Appropriate for scenarios where you want to centralize authentication or have multiple APIs that need the same security

```
┌─────────┐     Authorization    ┌─────────┐    JWT Token    ┌────────────┐    JWT Token    ┌──────────┐
│  User   │ ─────────────────────> Azure AD <────────────────┤ API Client │────────────────> API Mgmt  │
└─────────┘                      └─────────┘                 └────────────┘                 │(validates│
                                                                                            │  token)  │
                                                                                            └─────┬────┘
                                                                                                  │
                                                                                                  │ Pre-validated
                                                                                                  │ request
                                                                                                  ▼
                                                                                            ┌──────────┐
                                                                                            │  API     │
                                                                                            │  (no     │
                                                                                            │ auth)    │
                                                                                            └──────────┘
```

## Prerequisites

- Azure subscription
- Azure CLI or Azure Portal access
- .NET 8.0 SDK

## Setup Instructions

### 1. Create Azure AD App Registrations

You'll need to create three app registrations:

#### A. Direct Auth API Registration (ApimSample.Api)

1. Navigate to **Azure Active Directory** > **App registrations** > **New registration**
2. Enter details:
   - **Name**: `ApimSample.Api`
   - **Supported account types**: Accounts in this organizational directory only (Single tenant)
3. Click **Register** and note the **Client ID** and **Tenant ID**
4. Go to **Expose an API** > **Add a scope**:
   - Set Application ID URI to default `api://{clientId}`
   - **Scope name**: `weather.read`
   - **Admin consent display name**: `Read Weather Data`
   - **Admin consent description**: `Allows reading weather forecast data`
   - **State**: Enabled

#### B. APIM Auth API Registration (ApimSample.ApimSecuredApi)

1. Navigate to **Azure Active Directory** > **App registrations** > **New registration**
2. Enter details:
   - **Name**: `ApimSample.ApimSecuredApi`
   - **Supported account types**: Accounts in this organizational directory only (Single tenant)
3. Click **Register** and note the **Client ID** and **Tenant ID**
4. Go to **Expose an API** > **Add a scope**:
   - Set Application ID URI to default `api://{clientId}`
   - **Scope name**: `weather.read`
   - **Admin consent display name**: `Read Weather Data` 
   - **Admin consent description**: `Allows reading weather forecast data`
   - **State**: Enabled

#### C. Client Application Registration (for Swagger UI testing)

1. Navigate to **Azure Active Directory** > **App registrations** > **New registration**
2. Enter details:
   - **Name**: `ApimSample.Swagger`
   - **Supported account types**: Accounts in this organizational directory only (Single tenant)
   - **Redirect URI**: Web > `https://localhost:XXXX/swagger/oauth2-redirect.html` (replace XXXX with your local port)
3. Click **Register** and note the **Client ID**
4. Go to **Authentication** and enable **Implicit grant** for **Access tokens** and **ID tokens**
5. Go to **API permissions** > **Add a permission**:
   - Select **My APIs** > **ApimSample.Api**
   - Select **weather.read**
   - Click **Add permissions**
6. Repeat for the APIM Auth API:
   - Select **My APIs** > **ApimSample.ApimSecuredApi**
   - Select **weather.read**
   - Click **Add permissions**
7. Go to **Certificates & secrets** > **New client secret**:
   - Add a description and select expiration
   - Note down the secret value (visible only once)

### 2. Configure API Projects

#### A. Configure Direct Auth API (ApimSample.Api)

1. Update `appsettings.json`:
```json
{
  "Authentication": {
    "Authority": "https://login.microsoftonline.com/YOUR_TENANT_ID",
    "Audience": "api://YOUR_DIRECT_AUTH_CLIENT_ID",
    "SwaggerClientId": "YOUR_SWAGGER_CLIENT_ID"
  }
}
```

2. Make sure `Program.cs` uses these configuration values:
```csharp
options.Authority = builder.Configuration["Authentication:Authority"];
options.Audience = builder.Configuration["Authentication:Audience"];
```

3. Update Swagger OAuth configuration to use these values:
```csharp
// Replace hardcoded client ID with configuration
string apiClientId = builder.Configuration["Authentication:Audience"]?.Replace("api://", "");
c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
{
    // ...other properties...
    Flows = new OpenApiOAuthFlows
    {
        Implicit = new OpenApiOAuthFlow
        {
            // ...other properties...
            Scopes = new Dictionary<string, string>
            {
                { $"api://{apiClientId}/weather.read", "Read weather data" }
            }
        }
    }
});
```

#### B. Configure APIM Auth API (ApimSample.ApimSecuredApi)

No special configuration needed in the API itself as authentication will be handled by APIM policies.

### 3. Create and Configure Azure API Management

1. In the Azure portal, create a new **API Management** service
2. After deployment, navigate to your API Management service

### 4. Configure OAuth 2.0 Services in API Management

You'll need to create TWO OAuth 2.0 services:

#### A. OAuth 2.0 Service for Direct Auth API

1. Go to **Developer portal** > **OAuth 2.0 + OpenID Connect**
2. Click **Add** and enter:
   - **Display name**: `Direct Auth OAuth` 
   - **Id**: `direct-auth-oauth`
   - **Client registration page URL**: `https://portal.azure.com` (placeholder)
   - **Authorization grant types**: `Authorization code`
   - **Authorization endpoint URL**: `https://login.microsoftonline.com/YOUR_TENANT_ID/oauth2/v2.0/authorize`
   - **Token endpoint URL**: `https://login.microsoftonline.com/YOUR_TENANT_ID/oauth2/v2.0/token`
   - **Default scope**: `api://YOUR_DIRECT_AUTH_CLIENT_ID/weather.read`
   - **Client authentication methods**: `In the body`
   - **Client ID**: Your Swagger client ID
   - **Client secret**: Your Swagger client secret

#### B. OAuth 2.0 Service for APIM Auth API

1. Go to **Developer portal** > **OAuth 2.0 + OpenID Connect**
2. Click **Add** and enter:
   - **Display name**: `APIM Auth OAuth`
   - **Id**: `apim-auth-oauth`
   - **Client registration page URL**: `https://portal.azure.com` (placeholder)
   - **Authorization grant types**: `Authorization code`
   - **Authorization endpoint URL**: `https://login.microsoftonline.com/YOUR_TENANT_ID/oauth2/v2.0/authorize`
   - **Token endpoint URL**: `https://login.microsoftonline.com/YOUR_TENANT_ID/oauth2/v2.0/token`
   - **Default scope**: `api://YOUR_APIM_AUTH_CLIENT_ID/weather.read`
   - **Client authentication methods**: `In the body`
   - **Client ID**: Your Swagger client ID
   - **Client secret**: Your Swagger client secret

### 5. Import and Configure APIs in API Management

#### A. Import Direct Auth API

1. Go to **APIs** > **Add API** > **OpenAPI**
2. Enter the Swagger URL of your Direct Auth API
3. Set the **API URL suffix** to `direct-auth`
4. Click **Create**
5. Go to **Settings** and under **Security** section:
   - Select **OAuth 2.0** for User authorization
   - Select the **Direct Auth OAuth** server you created
   - Leave other settings as default (this API validates tokens itself)
6. Click **Save**

#### B. Import APIM Auth API

1. Go to **APIs** > **Add API** > **OpenAPI**
2. Enter the Swagger URL of your APIM Auth API
3. Set the **API URL suffix** to `apim-auth`
4. Click **Create**
5. Go to **All operations** > **Policies**
6. Add the JWT validation policy in the `<inbound>` section after `<base />`:
```xml
<validate-jwt header-name="Authorization" failed-validation-httpcode="401" failed-validation-error-message="Unauthorized. Access token is missing or invalid.">
    <openid-config url="https://login.microsoftonline.com/YOUR_TENANT_ID/v2.0/.well-known/openid-configuration" />
    <audiences>
        <audience>api://YOUR_APIM_AUTH_CLIENT_ID</audience>
    </audiences>
    <required-claims>
        <claim name="scp" match="any">
            <value>weather.read</value>
        </claim>
    </required-claims>
</validate-jwt>
```
7. Click **Save**

### 6. Create a Subscription for MVC Client

1. Go to **Subscriptions** > **Add Subscription**
2. Enter a name and select the appropriate scope (API or Product)
3. Note the **Primary Key** for use in your MVC client

### 7. Configure MVC Client

1. Update `appsettings.json`:
```json
{
  "ApiSettings": {
    "BaseUrl": "https://YOUR_APIM_NAME.azure-api.net",
    "ApiKey": "YOUR_SUBSCRIPTION_KEY"
  }
}
```

### 8. Deploy the APIs to Azure

1. Publish both API projects to Azure App Service
2. Update the Swagger OAuth 2.0 redirect URLs in Azure AD to include the deployed URLs
3. Test the APIs through both direct access and via API Management

## Running the Solution

1. First, build and deploy both APIs to Azure:
   - For command line:
     ```powershell
     cd ApimSample.Api
     dotnet publish -c Release
     
     cd ../ApimSample.ApimSecuredApi
     dotnet publish -c Release
     ```
   - Then deploy the published files to Azure App Service
   - See [Deploying from Different IDEs](#10-deploying-from-different-ides) section for detailed instructions

2. Deploy both APIs using one of these methods:
   - Using Visual Studio's integrated Publish feature
   - Using VS Code with the Azure Extensions
   - Using Azure CLI or PowerShell commands

3. Run the MVC client locally:
   ```
   cd ApimSample.MvcClient
   dotnet run
   ```

4. Browse to `http://localhost:5000` to see the home page with options to test both APIs
5. Click on either "Test Direct OAuth API" or "Test APIM OAuth API" to see the respective implementations in action

## Important Security Notes

This demo uses OAuth 2.0 to secure the API, but the MVC client itself is not authenticated. In a production environment:

1. The MVC client should also be secured with authentication
2. Use client credentials or authorization code flow with PKCE for service-to-service authentication
3. Store sensitive information like subscription keys in Azure Key Vault
4. Use Managed Identities where possible
5. Enable CORS restrictions in API Management to limit which origins can call your API

## Additional Security with Azure App Service Networking

For the APIM Auth API (ApimSample.ApimSecuredApi), you can add an extra layer of security using Azure App Service networking features:

### Option 1: IP Restrictions

1. In the Azure portal, navigate to your App Service
2. Go to **Networking** > **Access Restrictions**
3. Add a rule to allow traffic only from your API Management service's IP address:
   - Click **Add rule**
   - **Name**: APIM Access
   - **Priority**: 100
   - **Action**: Allow
   - **Type**: IPv4
   - **IP Address Block**: [Your API Management's IP address]/32
   - Click **Save**
4. Add another rule to deny all other traffic:
   - Click **Add rule** 
   - **Name**: Deny All
   - **Priority**: 200
   - **Action**: Deny
   - **Type**: IPv4
   - **IP Address Block**: Any
   - Click **Save**

This ensures that the API can only be called from your API Management instance.

### Option 2: Service Endpoints

1. Go to your App Service > **Networking** > **VNet Integration**
2. Click **Click here to configure** to integrate your App Service with a VNet
3. Select an existing VNet or create a new one
4. Once integrated, go to **Networking** > **Access Restrictions**
5. Add a rule:
   - Click **Add rule**
   - **Name**: VNet Access
   - **Priority**: 100
   - **Action**: Allow
   - **Type**: Virtual Network
   - Select your VNet/Subnet where APIM is deployed
   - Click **Save**
6. Add a deny all rule as described in Option 1

### Option 3: Private Endpoints

For highest security:

1. Go to your App Service > **Networking** > **Private endpoints**
2. Click **Add**
3. Configure the private endpoint:
   - **Name**: ApimSample-PrivateEndpoint
   - **Subscription/Resource Group**: Select yours
   - **Region**: Same as your App Service
   - **Resource Type**: Microsoft.Web/sites
   - **Resource**: Your App Service
   - **Target sub-resource**: sites
   - **Virtual network/Subnet**: Select the VNet where your APIM is deployed
   - **Private DNS integration**: Yes
4. Click **OK**

5. Update your API Management instance:
   - Go to your API Management service
   - Under **Deployment & Infrastructure** > **Virtual Network**
   - Select **Internal** mode
   - Configure it to use the same VNet as your private endpoint

## Securing with Azure Front Door

You can further enhance security by putting Azure Front Door in front of API Management:

1. Create an Azure Front Door Premium instance:
   - In Azure Portal, search for "Front Door and CDN profiles"
   - Click **Create** > select **Azure Front Door Premium**
   - Configure basic settings and click **Next**

2. Add your API Management endpoint:
   - In the **Endpoint** section, click **Add an endpoint**
   - **Endpoint name**: apim-endpoint
   - **Origin type**: Custom
   - **Host name**: Your APIM gateway hostname (e.g., your-apim.azure-api.net)
   - Complete the wizard and click **Create**

3. Configure Web Application Firewall (WAF):
   - Go to your Front Door resource
   - Under **Security**, select **Web Application Firewall policies**
   - Create a new policy or select an existing one
   - Apply built-in rule sets like OWASP Core Rule Set
   - Configure custom rules as needed

4. Configure rate limiting and geo-filtering:
   - In your WAF policy, click **Custom rules**
   - Create rules to:
     - Limit request rates by IP address
     - Block traffic from specific countries
     - Block suspicious user agents

5. Update your MVC client to use the Azure Front Door endpoint instead of directly accessing API Management

This multi-layered approach significantly improves your API's security posture by combining:
- OAuth 2.0 for authentication and authorization
- API Management for access control and monitoring
- Azure App Service networking for network-level isolation
- Azure Front Door for edge protection against common web attacks

With this setup, your API is protected at multiple levels, making it much more difficult for unauthorized users to access.

### 6. Understanding OAuth 2.0 Configuration Options

The following details explain the key configuration options shown in the Azure API Management OAuth 2.0 setup screens:

#### Authorization Configuration

- **Client Authentication Methods**: Determines how the client credentials are sent to the authorization server
  - **In the body**: Client credentials are included in the request body (this is the most common approach)
  - Other options include using HTTP Basic authentication headers

- **Access Token Sending Method**: Determines how the token is sent to your API
  - **Authorization header**: The token is sent in the `Authorization: Bearer <token>` header
  - This is the industry standard approach for sending access tokens

- **Default Scope**: The scope required for accessing the API resources
  - Format: `api://YOUR_API_CLIENT_ID/SCOPE_NAME`
  - Example: `api://71efe159-bcc2-4797-8d33-84fb2ad8c069/weather.read`

#### Client Credentials

- **Client ID**: The Application ID from your Azure AD client app registration
- **Client Secret**: The secret value you generated in the Azure AD app
- **Redirect URI**: Where users will be redirected after authentication (must match your AD app configuration)

#### Authorization Code Grant Flow

This is the URL that initiates the OAuth 2.0 authorization code flow. Your client application will redirect users to this URL to begin the authentication process. The URL includes:

- Your client ID
- Redirect URI
- Response type (code)
- Scope

#### Token Handling

When your application receives the authorization code, it exchanges it for an access token by sending a request to the token endpoint. This access token is then used to authenticate requests to your API by including it in the Authorization header.

### 7. Testing the OAuth 2.0 Flow

To test your OAuth 2.0 flow:

1. Run your MVC client application
2. Click on a link that requires authentication
3. You'll be redirected to the Microsoft login page
4. After successful authentication, you'll be redirected back to your application
5. Your application will exchange the authorization code for an access token
6. The access token will be used to call the API through API Management
7. API Management will validate the token and forward the request to your API

This flow ensures that only authenticated users with the proper permissions can access your API resources.

### 8. Troubleshooting OAuth 2.0 Setup

#### Common Error Messages

##### "ClientRegistrationEndpoint should not be empty"

If you encounter an error message stating that "ClientRegistrationEndpoint should not be empty" when setting up the OAuth2 service in API Management:

![OAuth2 Error Message](images/oauth2_error.png)

**Solution:**
- The "Client registration page URL" field cannot be left blank, even if you don't have an actual registration page
- Enter the Azure Portal URL (`https://portal.azure.com`) or your own application's home page as a placeholder
- This URL is meant to direct users to a page where they can register their client applications, but for this demo, it's not actively used

##### Invalid Redirect URI

If your application receives an error about an invalid redirect URI:

**Solution:**
- Ensure that the redirect URI in your Azure AD app registration exactly matches the one used in your application
- Check for trailing slashes, protocol differences (http vs https), or port numbers
- For local development, you might need to add `https://localhost:xxxx/signin-oidc` to your app registration's redirect URIs

##### Token Validation Failures

If API Management is rejecting tokens with 401 Unauthorized errors:

**Solution:**
- Verify that your validate-jwt policy is using the correct OpenID configuration URL
- Ensure you're using the correct version (v1.0 or v2.0) in your authorization endpoints
- Check that your audience and required claims match what's in the token
- You can inspect tokens using [jwt.ms](https://jwt.ms) to debug claim issues

#### Troubleshooting JWT Validation Policy Errors

If you encounter errors when setting up the JWT validation policy like:
- "Policy section is not allowed in the specified scope" for elements like 'openid-config', 'audiences', or 'required-claims'

These issues typically occur for the following reasons:

1. **Wrong Scope Level**: 
   - The policy might be applied at the incorrect scope level (e.g., at the product level instead of the API operation level)
   - Solution: Make sure to apply the policy at the "All operations" level for your API

2. **XML Structure Issues**:
   - The policy XML elements must be properly nested within the `<validate-jwt>` element
   - Make sure your policy editor shows the full policy XML structure including the `<inbound>` section
   - Some policy editors only show the section you're modifying, which can cause confusion

3. **Syntax Issues**:
   - Make sure there are no extra spaces or special characters in your XML
   - Ensure all opening tags have matching closing tags
   - Check for proper nesting of XML elements

4. **Policy Editor Mode**:
   - If using the full XML editor view, ensure you include the `<base />` tag to inherit policies from parent scopes
   - If using the section editor view, you only need to add the `<validate-jwt>` element and its contents

Example of a correct policy structure:

```xml
<policies>
    <inbound>
        <base />
        <validate-jwt header-name="Authorization" failed-validation-httpcode="401" failed-validation-error-message="Unauthorized. Access token is missing or invalid.">
            <openid-config url="https://login.microsoftonline.com/60b20c52-6462-4ab8-b261-32d173b5e51c/v2.0/.well-known/openid-configuration" />
            <audiences>
                <audience>api://1efe159c-bcc2-4797-8d33-84fb2ad8c069</audience>
            </audiences>
            <required-claims>
                <claim name="scp" match="any">
                    <value>weather.read</value>
                </claim>
            </required-claims>
        </validate-jwt>
    </inbound>
    <backend>
        <base />
    </backend>
    <outbound>
        <base />
    </outbound>
    <on-error>
        <base />
    </on-error>
</policies>
```

## Common Setup Issues and Troubleshooting

### Hardcoded Client IDs in Code

If you examine the `Program.cs` file in the ApimSample.Api project, you might notice that there's a hardcoded client ID:

```csharp
Scopes = new Dictionary<string, string>
{
    { "api://1efe159c-bcc2-4797-8d33-84fb2ad8c069/weather.read", "Read weather data" }
}
```

**Solution**: Replace this with your actual client ID. The correct approach is to use configuration:

```csharp
Scopes = new Dictionary<string, string>
{
    { $"api://{builder.Configuration["Authentication:Audience"]}/weather.read", "Read weather data" }
}
```

### Swagger Authorization Error: "AADSTS500013: Resource identifier is not provided"

When clicking "Authorize" in Swagger UI, you might see this Azure AD error.

**Problem**: The scope in the Swagger UI OAuth configuration is incorrectly formatted or doesn't match your Azure AD app registration.

**Solution**: Ensure your `Program.cs` file has the correct audience and scope format:

```csharp
// Get client ID from configuration
string clientId = builder.Configuration["Authentication:Audience"]?.Replace("api://", "");

c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
{
    // ...other properties...
    Flows = new OpenApiOAuthFlows
    {
        Implicit = new OpenApiOAuthFlow
        {
            // ...other properties...
            Scopes = new Dictionary<string, string>
            {
                { $"api://{clientId}/weather.read", "Read weather data" }
            }
        }
    }
});
```

### APIM JWT Validation Policy Error: "Policy scope is not allowed in this section"

This error typically occurs when trying to add the JWT validation policy in the wrong section.

**Solution**: Ensure you're adding the policy at the API level's "All operations" scope. The full policy XML should include:

```xml
<policies>
    <inbound>
        <base />
        <validate-jwt>
            <!-- Policy details -->
        </validate-jwt>
    </inbound>
    <backend>
        <base />
    </backend>
    <outbound>
        <base />
    </outbound>
    <on-error>
        <base />
    </on-error>
</policies>
```

### API Returns 401 Unauthorized Even With Valid Token

**Possible causes**:

1. **Audience mismatch**: 
   - The token's audience doesn't match what the API expects
   - **Solution**: Ensure the `Audience` in appsettings.json matches your Azure AD app's Application ID URI

2. **Scope mismatch**:
   - Required scope in JWT validation policy doesn't match the scope in the token
   - **Solution**: Check the `scp` claim in the token (using [jwt.ms](https://jwt.ms)) and match it in the policy

3. **Authority mismatch**:
   - Using v1.0 endpoint in one place and v2.0 in another
   - **Solution**: Be consistent with endpoints. For v2.0:
     - Authority: `https://login.microsoftonline.com/YOUR_TENANT_ID/v2.0`
     - OpenID config: `https://login.microsoftonline.com/YOUR_TENANT_ID/v2.0/.well-known/openid-configuration`

### MVC Client Error: "Access is denied due to invalid credentials"

**Possible causes**:

1. **Missing Subscription Key**: 
   - **Solution**: Ensure the `Ocp-Apim-Subscription-Key` header is added to HTTP requests:

```csharp
client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _configuration["ApiSettings:ApiKey"]);
```

2. **Invalid Endpoint**:
   - **Solution**: Verify the API base URL and endpoint path:

```csharp
// For Direct Auth API
string endpoint = "/direct-auth/weatherforecast";

// For APIM Auth API
string endpoint = "/apim-auth/weatherforecast";
```

### "No OpenID Connect Discovery document found" Error

This occurs when the OpenID configuration URL in the JWT validation policy is incorrect.

**Solution**: 
1. Verify the tenant ID is correct
2. Ensure you're using the correct version endpoint (v1.0 vs v2.0)
3. Test the URL in a browser to confirm it returns JSON:

```
https://login.microsoftonline.com/YOUR_TENANT_ID/v2.0/.well-known/openid-configuration
```

## Implementation Code Examples

#### ApimSample.Api (Direct Auth) - Program.cs

```csharp
// Configure JWT Authentication in the API
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

// Configure Swagger with OAuth 2.0
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            Implicit = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{builder.Configuration["Authentication:Authority"]}/oauth2/authorize"),
                TokenUrl = new Uri($"{builder.Configuration["Authentication:Authority"]}/oauth2/token"),
                Scopes = new Dictionary<string, string>
                {
                    { $"api://{builder.Configuration["Authentication:Audience"]}/weather.read", "Read weather data" }
                }
            }
        }
    });
    
    // Ensure Swagger UI requires OAuth
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
            },
            new[] { "api://[YOUR-CLIENT-ID]/weather.read" }
        }
    });
});

// Ensure authentication middleware is added to the pipeline
app.UseAuthentication();
app.UseAuthorization();
```

#### ApimSample.Api (Direct Auth) - WeatherForecastController.cs

```csharp
[ApiController]
[Route("[controller]")]
[Authorize] // Controller requires authentication
public class WeatherForecastController : ControllerBase
{
    // Implementation
}
```

#### ApimSample.ApimSecuredApi (APIM Auth) - Program.cs

```csharp
// No authentication setup in the API
// Just normal controller configuration
builder.Services.AddControllers();

// Swagger only shows API Management subscription key
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("apiManagement", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "Ocp-Apim-Subscription-Key",
        Description = "API Management subscription key. Authentication is handled by Azure API Management."
    });
});

// No authentication middleware needed
// app.UseAuthentication(); - Not needed
```

#### ApimSample.ApimSecuredApi (APIM Auth) - WeatherForecastController.cs

```csharp
[ApiController]
[Route("[controller]")]
// No [Authorize] attribute needed - authentication handled by APIM
public class WeatherForecastController : ControllerBase
{
    // Implementation
}
```

#### APIM JWT Validation Policy (for ApimSample.ApimSecuredApi)

```xml
<validate-jwt header-name="Authorization" failed-validation-httpcode="401" failed-validation-error-message="Unauthorized. Access token is missing or invalid.">
    <openid-config url="https://login.microsoftonline.com/[YOUR-TENANT-ID]/v2.0/.well-known/openid-configuration" />
    <audiences>
        <audience>api://[YOUR-API-CLIENT-ID]</audience>
    </audiences>
    <required-claims>
        <claim name="scp" match="any">
            <value>weather.read</value>
        </claim>
    </required-claims>
</validate-jwt>
```

## Azure Deployment and Security Best Practices

When deploying this solution to production environments, consider the following best practices:

### Infrastructure as Code (IaC)

Use Infrastructure as Code to deploy your API Management instance and related resources:

#### Bicep Example for API Management with OAuth

```bicep
param location string = resourceGroup().location
param apiManagementName string
param publisherEmail string
param publisherName string

// API Management instance
resource apiManagement 'Microsoft.ApiManagement/service@2021-08-01' = {
  name: apiManagementName
  location: location
  sku: {
    name: 'Developer'
    capacity: 1
  }
  properties: {
    publisherEmail: publisherEmail
    publisherName: publisherName
  }
}

// Create the OAuth 2.0 server
resource oauthServer 'Microsoft.ApiManagement/service/authorizationServers@2021-08-01' = {
  parent: apiManagement
  name: 'apim-oauth'
  properties: {
    displayName: 'API OAuth Server'
    clientRegistrationEndpoint: 'https://portal.azure.com'
    authorizationEndpoint: 'https://login.microsoftonline.com/${tenant().tenantId}/oauth2/v2.0/authorize'
    tokenEndpoint: 'https://login.microsoftonline.com/${tenant().tenantId}/oauth2/v2.0/token'
    grantTypes: [
      'authorizationCode'
    ]
    clientAuthenticationMethod: [
      'Body'
    ]
    bearerTokenSendingMethods: [
      'authorizationHeader'
    ]
    defaultScope: 'api://${clientId}/weather.read'
    clientId: clientId
    clientSecret: clientSecret
  }
}

// Import an API with OAuth 2.0 protection
resource api 'Microsoft.ApiManagement/service/apis@2021-08-01' = {
  parent: apiManagement
  name: 'direct-auth-api'
  properties: {
    displayName: 'Direct Auth API'
    path: 'direct-auth'
    protocols: [
      'https'
    ]
    subscriptionRequired: true
    authenticationSettings: {
      oAuth2AuthenticationSettings: [
        {
          authorizationServerId: oauthServer.name
        }
      ]
      openidAuthenticationSettings: []
    }
  }
}
```

### Security Recommendations

1. **Use Managed Identities**:
   - Configure Managed Identities for API Management to access Azure Key Vault
   - Store client secrets and subscription keys in Key Vault
   - Example Key Vault integration:

```csharp
// In API project
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/"),
    new DefaultAzureCredential());
```

2. **Implement Token Validation Best Practices**:
   - Validate token issuer
   - Validate audience
   - Validate token lifetime
   - Validate required claims

3. **Network Security**:
   - Use Private Endpoints for API Management
   - Configure NSGs to restrict traffic
   - Deploy API Management in Internal mode for high-security scenarios

4. **APIM Policy Security**:
   - Use policies to enforce IP restrictions
   - Add rate limiting to prevent abuse
   - Implement request validation to prevent attacks
   - Example secure policy:

```xml
<policies>
    <inbound>
        <base />
        <ip-filter action="allow">
            <address-range from="20.193.15.0" to="20.193.15.255" />
        </ip-filter>
        <rate-limit calls="5" renewal-period="60" />
        <validate-jwt header-name="Authorization" failed-validation-httpcode="401">
            <!-- JWT validation details -->
        </validate-jwt>
    </inbound>
</policies>
```

### CI/CD Pipeline Recommendations

1. Use Azure DevOps or GitHub Actions to automate deployments
2. Use Service Principals with least-privilege access
3. Include API Management deployment in your CI/CD pipeline
4. Example GitHub Actions workflow:

```yaml
name: Deploy API Management

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v2
    
    - name: Login to Azure
      uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}
        
    - name: Deploy API Management
      uses: azure/arm-deploy@v1
      with:
        resourceGroupName: ${{ secrets.RESOURCE_GROUP }}
        template: ./infra/apim.bicep
        parameters: 'apiManagementName=${{ secrets.APIM_NAME }} publisherEmail=${{ secrets.PUBLISHER_EMAIL }} publisherName=${{ secrets.PUBLISHER_NAME }}'
```

### Monitoring and Logging

1. Enable Application Insights for API Management
2. Configure diagnostic settings to log all requests
3. Set up alerts for authentication failures
4. Monitor token validation errors

### Authentication Flow Security

1. Use authorization code flow with PKCE for public clients
2. Use client credentials flow for service-to-service
3. Implement token refresh handling
4. Store refresh tokens securely

### URL and Configuration Summary

The following tables summarize the key differences between the two API projects:

#### URLs and Endpoints

| Purpose | ApimSample.Api (Direct Auth) | ApimSample.ApimSecuredApi (APIM Auth) |
|---------|---------------|---------------------------|
| **API Base URL** | `https://[api-app-name].azurewebsites.net` | `https://[apimsecured-app-name].azurewebsites.net` |
| **Swagger URL** | `https://[api-app-name].azurewebsites.net/swagger` | `https://[apimsecured-app-name].azurewebsites.net/swagger` |
| **APIM URL** | `https://[your-apim-name].azure-api.net/direct-auth` | `https://[your-apim-name].azure-api.net/apim-auth` |
| **Swagger Redirect** | `https://[api-app-name].azurewebsites.net/swagger/oauth2-redirect.html` | N/A - Authentication handled by APIM |
| **Authority URL** | `https://login.microsoftonline.com/[DirectAuth-TenantID]` | `https://login.microsoftonline.com/[ApimAuth-TenantID]` |
| **OpenID Config** | `https://login.microsoftonline.com/[DirectAuth-TenantID]/v2.0/.well-known/openid-configuration` | `https://login.microsoftonline.com/[ApimAuth-TenantID]/v2.0/.well-known/openid-configuration` |
| **Token Endpoint** | `https://login.microsoftonline.com/[DirectAuth-TenantID]/oauth2/v2.0/token` | `https://login.microsoftonline.com/[ApimAuth-TenantID]/oauth2/v2.0/token` |
| **Authorization Endpoint** | `https://login.microsoftonline.com/[DirectAuth-TenantID]/oauth2/v2.0/authorize` | `https://login.microsoftonline.com/[ApimAuth-TenantID]/oauth2/v2.0/authorize` |

#### Configuration and Implementation Differences

| Feature | ApimSample.Api (Direct Auth) | ApimSample.ApimSecuredApi (APIM Auth) |
|---------|---------------|---------------------------|
| **Authentication Location** | Inside API code (middleware) | APIM policy only |
| **Token Validation** | ASP.NET Core JWT Bearer middleware | APIM validate-jwt policy |
| **Controller Attribute** | `[Authorize]` required | No attribute needed |
| **AppSettings Auth Config** | Required | Not required |
| **OAuth 2.0 Server in APIM** | Pass-through | Active validation |
| **Swagger Auth UI** | OAuth 2.0 flow | API Key only |
| **Error Responses** | Generated by API | Generated by APIM |
| **Scope** | `api://[DirectAuth-ClientID]/weather.read` | `api://[ApimAuth-ClientID]/weather.read` |
| **MVC Client Endpoint** | `/direct-auth/weatherforecast` | `/apim-auth/weatherforecast` |

#### App Registration IDs vs. URLs

It's important to keep track of which client IDs and URLs belong to which API:

1. **DirectAuth-ClientID**: The Application (client) ID from the ApimSample.Api app registration
   - Used in: 
     - API's appsettings.json as `Authentication:Audience` 
     - Swagger UI OAuth config
     - APIM OAuth 2.0 server definition for Direct Auth API

2. **ApimAuth-ClientID**: The Application (client) ID from the ApimSample.ApimSecuredApi app registration
   - Used in:
     - APIM JWT validation policy `<audience>` tag
     - APIM OAuth 2.0 server definition for APIM Auth API

3. **Swagger-ClientID**: The Application (client) ID from the client app registration
   - Used in:
     - API's appsettings.json as `Authentication:SwaggerClientId`
     - APIM OAuth 2.0 server client configuration

## Authentication Flow Comparison

### Direct Auth Flow (ApimSample.Api)

The Direct Auth approach follows this sequence:

1. **Client obtains token**:
   ```
   Client App → Azure AD → Client App (with token)
   ```

2. **Client calls API through APIM**:
   ```
   Client App → APIM (forwards token) → API → API validates token → API processes request
   ```

3. **Authentication responsibility**:
   - APIM: Forwards the token, doesn't validate it
   - API: Validates the token using ASP.NET Core JWT Bearer middleware

4. **Code dependencies**:
   - Requires Microsoft.AspNetCore.Authentication.JwtBearer package
   - Requires authentication configuration in appsettings.json
   - Requires [Authorize] attributes on controllers

### APIM Auth Flow (ApimSample.ApimSecuredApi)

The APIM Auth approach follows this sequence:

1. **Client obtains token**:
   ```
   Client App → Azure AD → Client App (with token)
   ```

2. **Client calls API through APIM**:
   ```
   Client App → APIM validates token → If valid → API → API processes request
                                     → If invalid → Error returned (API never called)
   ```

3. **Authentication responsibility**:
   - APIM: Validates the token using JWT validation policy
   - API: No authentication logic, receives only pre-validated requests

4. **Code dependencies**:
   - No authentication packages needed in the API project
   - No authentication configuration in appsettings.json
   - No [Authorize] attributes on controllers

### Side-by-Side Comparison

```
┌─────────────┐                  ┌─────────────┐                  ┌─────────────┐
│  Client App │                  │  Client App │                  │  Azure AD   │
└──────┬──────┘                  └──────┬──────┘                  └──────┬──────┘
       │                                │                                │
       │                                │      Authentication request    │
       │                                │────────────────────────────────>
       │                                │                                │
       │                                │      Token response            │
       │                                │<────────────────────────────────
       │                                │                                │
       │                                │                                │
┌──────┴──────┐                  ┌──────┴──────┐                  ┌──────┴──────┐
│  DIRECT AUTH│                  │  APIM AUTH  │                  │             │
└──────┬──────┘                  └──────┬──────┘                  │             │
       │                                │                                │
       │                                │                                │
       │      API request with token    │      API request with token    │
       │─────────────────────────────────────────────────────────────────>
       │                                │                                │
┌──────┴──────┐                  ┌──────┴──────┐                  ┌──────┴──────┐
│    APIM     │                  │    APIM     │                  │    APIM     │
│  (passes    │                  │  (validates │                  │ (validation │
│   token)    │                  │   token)    │                  │  fails)     │
└──────┬──────┘                  └──────┬──────┘                  └──────┬──────┘
       │                                │                                │
       │                                │                                │
       │      Forward request           │      Forward request           │      Return 401
       │      with token                │      (token validated)         │      Unauthorized
       ▼                                ▼                                ▼
┌─────────────┐                  ┌─────────────┐                  ┌─────────────┐
│     API     │                  │     API     │                  │  Client App │
│  (validates │                  │ (no auth    │                  │ (receives   │
│   token)    │                  │  logic)     │                  │  error)     │
└─────────────┘                  └─────────────┘                  └─────────────┘
```

### When to Choose Each Approach

#### Advantages of Direct Auth (ApimSample.Api)

1. **Fine-grained control**: API has full control over authentication and authorization logic
2. **Flexibility**: Can use the same authentication whether accessed directly or through APIM
3. **Advanced scenarios**: Supports complex authorization based on claims and policies
4. **Middleware features**: Takes advantage of ASP.NET Core authentication middleware capabilities
5. **Easier local testing**: Works locally without configuring APIM

#### Advantages of APIM Auth (ApimSample.ApimSecuredApi)

1. **Simplified API code**: No authentication code needed in the API itself
2. **Centralized security**: All security policies managed in one place (APIM)
3. **Consistent enforcement**: Same policies applied across multiple APIs
4. **Enhanced performance**: Authentication failures rejected at the gateway, saving backend resources
5. **Policy flexibility**: Easy to update security requirements without changing API code

## Quick Reference Cheat Sheet

### App Registration and Configuration Cheat Sheet

Use this quick reference to keep track of which values go where:

#### ApimSample.Api (Direct Auth)

| Setting | Value | Where to Use |
|---------|-------|--------------|
| **Azure AD App Name** | `ApimSample.Api` | App registration |
| **Client ID** | `[generated-id]` | Save as `DirectAuth-ClientID` |
| **Tenant ID** | `[your-tenant-id]` | Save as `DirectAuth-TenantID` |
| **Scope** | `api://[DirectAuth-ClientID]/weather.read` | APIM OAuth server, Swagger UI |
| **Authority** | `https://login.microsoftonline.com/[DirectAuth-TenantID]` | API appsettings.json |
| **Audience** | `api://[DirectAuth-ClientID]` | API appsettings.json |
| **APIM URL suffix** | `direct-auth` | APIM API configuration |

#### appsettings.json Example for Direct Auth API

```json
{
  "Authentication": {
    "Authority": "https://login.microsoftonline.com/60b20c52-6462-4ab8-b261-32d173b5e51c",
    "Audience": "api://1efe159c-bcc2-4797-8d33-84fb2ad8c069",
    "SwaggerClientId": "a3384a94-0145-4fb6-a5c6-634b2bba2397"
  }
}
```

#### ApimSample.ApimSecuredApi (APIM Auth)

| Setting | Value | Where to Use |
|---------|-------|--------------|
| **Azure AD App Name** | `ApimSample.ApimSecuredApi` | App registration |
| **Client ID** | `[generated-id]` | Save as `ApimAuth-ClientID` |
| **Tenant ID** | `[your-tenant-id]` | Save as `ApimAuth-TenantID` |
| **Scope** | `api://[ApimAuth-ClientID]/weather.read` | APIM OAuth server, JWT policy |
| **OpenID config URL** | `https://login.microsoftonline.com/[ApimAuth-TenantID]/v2.0/.well-known/openid-configuration` | JWT validation policy |
| **Audience** | `api://[ApimAuth-ClientID]` | JWT validation policy |
| **APIM URL suffix** | `apim-auth` | APIM API configuration |

#### JWT Validation Policy for APIM Auth API

```xml
<validate-jwt header-name="Authorization" failed-validation-httpcode="401" failed-validation-error-message="Unauthorized. Access token is missing or invalid.">
    <openid-config url="https://login.microsoftonline.com/60b20c52-6462-4ab8-b261-32d173b5e51c/v2.0/.well-known/openid-configuration" />
    <audiences>
        <audience>api://1efe159c-bcc2-4797-8d33-84fb2ad8c069</audience>
    </audiences>
    <required-claims>
        <claim name="scp" match="any">
            <value>weather.read</value>
        </claim>
    </required-claims>
</validate-jwt>
```

#### MVC Client (ApimSample.MvcClient)

| Setting | Value | Where to Use |
|---------|-------|--------------|
| **APIM URL** | `https://[your-apim-name].azure-api.net` | MVC client appsettings.json |
| **APIM Subscription Key** | `[your-subscription-key]` | MVC client appsettings.json |
| **Direct Auth Endpoint** | `/direct-auth/weatherforecast` | WeatherService.cs |
| **APIM Auth Endpoint** | `/apim-auth/weatherforecast` | WeatherService.cs |

#### appsettings.json Example for MVC Client

```json
{
  "ApiSettings": {
    "BaseUrl": "https://your-apim-name.azure-api.net",
    "ApiKey": "74d23bf46e2d44a3bc83e9e303412c0a"
  }
}
```

### Common Errors and Quick Fixes

| Error | Likely Cause | Quick Fix |
|-------|--------------|-----------|
| "Resource identifier is not provided" | Incorrect scope format in Swagger | Update scope to match Azure AD registration |
| "Invalid audience" | Token audience doesn't match API's expected audience | Ensure audience in token matches API's audience setting |
| "Policy scope is not allowed" | JWT policy added at wrong scope level | Apply JWT validation policy at "All operations" scope |
| "ClientRegistrationEndpoint should not be empty" | Missing client registration URL | Use `https://portal.azure.com` as placeholder |
| 401 Unauthorized from API | Token validation failure | Check issuer, audience, and scope match between token and API |
| 401 Unauthorized from APIM | Missing or invalid subscription key | Add `Ocp-Apim-Subscription-Key` header with proper key |
