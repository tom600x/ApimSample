# Azure API Management Sample with OAuth 2.0

This sample demonstrates two approaches to securing APIs with OAuth 2.0 in Azure API Management and accessing them from an MVC client application.

## Solution Structure

- **ApimSample.Api**: .NET Core 8.0 Web API secured with built-in OAuth 2.0 (Direct Auth)
- **ApimSample.ApimSecuredApi**: .NET Core 8.0 Web API with security handled by APIM (APIM Auth)
- **ApimSample.MvcClient**: .NET Core 8.0 MVC client application that calls both APIs through Azure API Management

## Prerequisites

- Azure subscription
- Azure CLI or Azure Portal access
- .NET 8.0 SDK

## Setup Instructions

### 1. Create Azure AD App Registrations

#### API Registration

1. Sign in to the [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** > **App registrations** > **New registration**
3. Enter the following details:
   - **Name**: `ApimSample.Api`
   - **Supported account types**: Accounts in this organizational directory only (Single tenant)
   - **Redirect URI**: (Leave blank for now)
4. Click **Register**
5. Note down the **Application (client) ID** and **Directory (tenant) ID**
6. Go to **Expose an API** > **Add a scope**:
   - **Application ID URI**: Click **Set** to use the default `api://{clientId}`
   - **Scope name**: `weather.read`
   - **Admin consent display name**: `Read Weather Data`
   - **Admin consent description**: `Allows reading weather forecast data`
   - **State**: Enabled
7. Click **Add scope**

#### Client Registration (for Swagger UI)

1. Navigate to **Azure Active Directory** > **App registrations** > **New registration**
2. Enter the following details:
   - **Name**: `ApimSample.Swagger`
   - **Supported account types**: Accounts in this organizational directory only (Single tenant)
   - **Redirect URI**: Web > `https://your-api-url/swagger/oauth2-redirect.html` (replace with your actual API URL)
3. Click **Register**
4. Note down the **Application (client) ID**
5. Go to **Authentication**, and ensure **Implicit grant and hybrid flows** has **Access tokens** and **ID tokens** checked
6. Go to **API permissions** > **Add a permission**:
   - Select **My APIs** > **ApimSample.Api**
   - Select **Delegated permissions** > **weather.read**
   - Click **Add permissions**
7. Go to **Certificates & secrets** > **New client secret**:
   - Enter a description and select expiration
   - Note down the **Secret Value** (only visible once)

### 2. Configure the API Project

Update the `appsettings.json` in the API project:

```json
{
  "Authentication": {
    "Authority": "https://login.microsoftonline.com/YOUR_TENANT_ID",
    "Audience": "api://YOUR_API_CLIENT_ID",
    "SwaggerClientId": "YOUR_SWAGGER_CLIENT_ID"
  }
}
```

### 3. Create and Configure Azure API Management

1. In the Azure portal, create a new **API Management** service
2. After deployment, navigate to your API Management service

#### Configure OAuth 2.0 Service in API Management

1. Go to **Developer portal** > **OAuth 2.0 + OpenID Connect**
2. Click **Add** to create a new OAuth 2.0 service
3. Enter the following details:
   - **Display name**: `ApimSample OAuth` (required)
   - **Id**: `apimsample-oauth` (required - this is your OAuth server identifier)
   - **Description**: `Authorization server description`
   - **Client registration page URL**: `https://portal.azure.com` (required - cannot be left blank, use Azure portal URL as a placeholder)
   - **Authorization grant types**: `Authorization code`
   - **Authorization endpoint URL**: `https://login.microsoftonline.com/YOUR_TENANT_ID/oauth2/v2.0/authorize` (required)
   - **Token endpoint URL**: `https://login.microsoftonline.com/YOUR_TENANT_ID/oauth2/v2.0/token` (required)
   - **Default scope**: `api://YOUR_API_CLIENT_ID/weather.read` (the scope you created in your Azure AD app registration)
   - **Support state parameter**: Checked
   - **Client authentication methods**: `In the body` (the location where client credentials are sent)
   - **Access token sending method**: `Authorization header` (how the token is sent to your API)
4. Click **Create** to save the OAuth 2.0 service configuration

#### Configure Client Credentials

Once the OAuth 2.0 service is created, you'll need to add your client credentials:

1. Fill in the following details:
   - **Client ID**: `YOUR_CLIENT_ID` (from your Azure AD client app registration)
   - **Client secret**: `YOUR_CLIENT_SECRET` (from your Azure AD client app registration)
   - **Redirect URI**: The redirect URI configured in your Azure AD app registration (e.g., your MVC client callback URL)
   - **Authorization code grant flow**: URL provided by API Management (copy this for your client application)
   - **Implicit grant flow**: URL provided by API Management (if using implicit flow)

#### Configure Direct Auth API (API validates tokens)

1. Go to **APIs** > **Add API** > **OpenAPI**
2. Enter the Direct Auth API's Swagger URL (e.g., `https://your-api-url/swagger/v1/swagger.json`)
3. Set the **API URL suffix** to `direct-auth`
4. Click **Create**
5. Go to **APIs** > Select your Direct Auth API > **Settings**
6. Under **Security** section, for **User authorization**, select the radio button for **OAuth 2.0**
7. For the **OAuth 2.0 server** dropdown, select the OAuth 2.0 server you created earlier (`ApimSample OAuth`)
   - This dropdown will show the OAuth 2.0 servers you've configured in the Developer Portal section
   - If you don't see your server in the dropdown, go back to the Developer Portal section and make sure you've created the OAuth 2.0 server correctly
8. If needed, check the **Override scope** option to specify a custom scope
9. Click **Save**

#### Configure APIM Auth API (APIM validates tokens)

1. Go to **APIs** > **Add API** > **OpenAPI**
2. Enter the APIM Auth API's Swagger URL (e.g., `https://your-apim-secured-api-url/swagger/v1/swagger.json`)
3. Set the **API URL suffix** to `apim-auth`
4. Click **Create**

5. Configure OAuth 2.0 validation using an inbound policy:
   - Go to **APIs** > Select the APIM Auth API > **Inbound processing**
   - Click the **</>** (code editor) button
   - Add the following policy inside the `<inbound>` section:

```xml
<validate-jwt header-name="Authorization" failed-validation-httpcode="401" failed-validation-error-message="Unauthorized. Access token is missing or invalid.">
    <openid-config url="https://login.microsoftonline.com/YOUR_TENANT_ID/v2.0/.well-known/openid-configuration" />
    <audiences>
        <audience>api://YOUR_API_CLIENT_ID</audience>
    </audiences>
    <required-claims>
        <claim name="scp" match="any">
            <value>weather.read</value>
        </claim>
    </required-claims>
</validate-jwt>
```

   - Replace `YOUR_TENANT_ID` with your Azure AD tenant ID
   - Replace `YOUR_API_CLIENT_ID` with the Application (client) ID of your API app registration
   - Note: The policy uses the v2.0 endpoint for OpenID configuration

6. Click **Save**

#### Create a Subscription Key for the MVC Client

1. Go to **Subscriptions** > **Add Subscription**
2. Enter a name and select a scope
3. Note down the **Primary Key** and **Secondary Key**

### 4. Configure the MVC Client

Update the `appsettings.json` in the MVC client project:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://YOUR_APIM_NAME.azure-api.net",
    "ApiKey": "YOUR_SUBSCRIPTION_KEY"
  }
}
```

### 5. Detailed OAuth 2.0 Client Configuration

When setting up your MVC client to work with the OAuth 2.0 protected API, you'll need the following information from your API Management OAuth 2.0 service:

#### OAuth 2.0 Configuration Details

Based on the OAuth 2.0 service you've set up in API Management, make note of the following values:

1. **Client ID**: This is the Application (client) ID from your Azure AD client app registration (e.g., `a3384a94-0145-4fb6-a5c6-634b2bba2397`)

2. **Client Secret**: The secret value generated in the Azure AD app registration

3. **Authorization Endpoint URL**: The URL for authorization requests
   - Example: `https://login.microsoftonline.com/YOUR_TENANT_ID/oauth2/v2.0/authorize`

4. **Token Endpoint URL**: The URL for obtaining tokens
   - Example: `https://login.microsoftonline.com/YOUR_TENANT_ID/oauth2/v2.0/token`

5. **Default Scope**: The scope required for accessing the API
   - Example: `api://YOUR_API_CLIENT_ID/weather.read` or a custom scope like `api://71efe159-bcc2-4797-8d33-84fb2ad8c069/weather.read`

6. **Redirect URI**: The URI where users will be redirected after authentication
   - Configure this in your Azure AD app registration and in your MVC client

#### Integrating OAuth 2.0 in Your MVC Client

Add the following to your `Startup.cs` or `Program.cs` file:

```csharp
// In Program.cs
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect(options =>
{
    options.ClientId = Configuration["Authentication:ClientId"];
    options.ClientSecret = Configuration["Authentication:ClientSecret"];
    options.Authority = Configuration["Authentication:Authority"];
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("api://YOUR_API_CLIENT_ID/weather.read"); // Add your API scope
    options.GetClaimsFromUserInfoEndpoint = true;
});

// In your WeatherService.cs
public async Task<IEnumerable<WeatherForecast>> GetWeatherForecastAsync(string accessToken)
{
    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiSettings.ApiKey);
    
    var response = await _httpClient.GetAsync($"{_apiSettings.BaseUrl}/weather");
    // Process response...
}
```

Update your `appsettings.json` with these settings:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://YOUR_APIM_NAME.azure-api.net",
    "ApiKey": "YOUR_SUBSCRIPTION_KEY"
  },
  "Authentication": {
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET",
    "Authority": "https://login.microsoftonline.com/YOUR_TENANT_ID"
  }
}
```

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

#### Testing OAuth 2.0 Configuration

To verify your OAuth 2.0 setup is working correctly:

1. Use the API Management Developer Portal's built-in console:
   - Navigate to your API in the Developer Portal
   - Click "Try it"
   - Select your OAuth 2.0 server from the authorization dropdown
   - Follow the authentication flow

2. Use Postman:
   - Create a new request to your API endpoint
   - Under the Authorization tab, select "OAuth 2.0"
   - Configure the OAuth 2.0 settings with your client ID, secret, and endpoints
   - Request a new token and send your API request

### 9. Important Notes on OAuth 2.0 Configuration Flow

When setting up OAuth 2.0 in Azure API Management, it's important to understand the relationship between different configuration sections:

1. **Developer Portal OAuth 2.0 Setup → API Security Settings**
   
   You must first configure the OAuth 2.0 server in the Developer Portal section before you can use it in your API security settings:
   
   - First: Configure OAuth 2.0 in **Developer portal > OAuth 2.0 + OpenID Connect**
   - Then: Use that OAuth 2.0 server in **APIs > [Your API] > Settings > Security > User authorization > OAuth 2.0**

   ![API Security Settings - OAuth 2.0 Server Selection](images/api_oauth_security.png)

   If your OAuth 2.0 server doesn't appear in the dropdown when configuring API security, it means either:
   - The OAuth 2.0 server wasn't properly created in the Developer Portal
   - There was an error in the OAuth 2.0 server configuration (check for any validation errors)
   - You need to refresh the page to see the newly created OAuth 2.0 server

2. **OAuth 2.0 Server Configuration → JWT Validation Policy**

   When using APIM to validate tokens (APIM Auth approach), ensure the JWT validation policy matches the OAuth 2.0 server configuration:
   - The audience in the JWT validation policy should match the client ID in your Azure AD app registration
   - The required claims should match the scopes you've defined in your OAuth 2.0 server
   - Use the correct version (v1.0 or v2.0) consistently across all configuration points

### 10. Deploying from Different IDEs

#### Deploying from Visual Studio

Visual Studio provides integrated deployment tools for Azure:

1. **Right-click on the Project** in Solution Explorer and select **Publish**
2. Select **Azure** as the publish target
3. Choose the appropriate Azure target:
   - **Azure App Service** for the API projects
   - You can create a new App Service or select an existing one
4. Configure the App Service details:
   - Choose your subscription
   - Select a resource group or create a new one
   - Name your App Service
   - Choose a hosting plan
5. Configure **Advanced** settings:
   - For the API projects, ensure the "Configuration" is set to "Release"
   - Make sure "Deploy as a self-contained application" is checked if needed
6. Click **Save** and then **Publish**
7. Visual Studio will build and deploy your application
8. After deployment, Visual Studio will open a browser to your newly deployed API
9. Verify the API is working by navigating to the Swagger endpoint (e.g., `/swagger/index.html`)

#### Deploying from Visual Studio Code

If you're using VS Code, you can deploy using the Azure App Service extension:

1. **Install the Azure Extensions**:
   - Open the Extensions panel (Ctrl+Shift+X)
   - Search for "Azure Tools" or "Azure App Service" and install it
   - Sign in to your Azure account through VS Code

2. **Build your application**:
   ```powershell
   dotnet publish -c Release
   ```

3. **Deploy from VS Code**:
   - Open the Azure extension panel (look for the Azure icon in the Activity Bar)
   - Navigate to the App Service section
   - Right-click on your subscription and select "Create new Web App..."
   - Follow the prompts to create a new App Service:
     - Enter a globally unique name
     - Select .NET 8 runtime
     - Choose a location
     - Select a pricing tier
   - Once created, right-click on the new App Service and select "Deploy to Web App..."
   - Choose the folder containing your published files (usually under `bin/Release/net8.0/publish`)
   - Confirm the deployment

4. **Alternatively, use Azure CLI from VS Code Terminal**:
   ```powershell
   # Login to Azure
   az login

   # Create a resource group if you don't have one
   az group create --name YourResourceGroup --location westus2

   # Create an App Service Plan
   az appservice plan create --name YourPlan --resource-group YourResourceGroup --sku B1

   # Create the Web App
   az webapp create --name YourApiName --resource-group YourResourceGroup --plan YourPlan --runtime "dotnet:8"

   # Deploy from a published folder
   cd YourProject
   dotnet publish -c Release
   az webapp deploy --resource-group YourResourceGroup --name YourApiName --src-path bin/Release/net8.0/publish --type zip
   ```

5. **Verify your deployment** by navigating to `https://your-api-name.azurewebsites.net/swagger`

Remember to update your application settings after deployment to include all the required configuration values for OAuth 2.0, such as tenant ID, client ID, and client secret.

### 11. Configuring Azure App Service Settings

After deploying your APIs to Azure App Service, you need to configure the application settings to match your environment:

#### Setting Configuration Values in Azure Portal

1. Go to the Azure Portal and navigate to your App Service
2. Go to **Settings** > **Configuration**
3. Under the **Application settings** tab, add the following key-value pairs:
   
   **For ApimSample.Api**:
   - `Authentication:Authority`: `https://login.microsoftonline.com/YOUR_TENANT_ID`
   - `Authentication:Audience`: `api://YOUR_API_CLIENT_ID`
   - `Authentication:SwaggerClientId`: `YOUR_SWAGGER_CLIENT_ID`
   
   **For ApimSample.MvcClient** (if deployed to Azure):
   - `ApiSettings:BaseUrl`: `https://YOUR_APIM_NAME.azure-api.net`
   - `ApiSettings:ApiKey`: `YOUR_SUBSCRIPTION_KEY`
   - `Authentication:ClientId`: `YOUR_CLIENT_ID`
   - `Authentication:ClientSecret`: `YOUR_CLIENT_SECRET`
   - `Authentication:Authority`: `https://login.microsoftonline.com/YOUR_TENANT_ID`

4. Click **Save** to apply the settings

#### Setting Configuration Values with Visual Studio

If you're deploying from Visual Studio:

1. In the **Publish** wizard, after selecting your target, click on **Edit** next to the App Service configuration
2. Go to the **Settings** tab
3. Expand the **File Publish Options** section
4. Check **Remove additional files at destination** if you want a clean deployment
5. Expand your project section (e.g., ApimSample.Api)
6. Add your application settings in the key-value editor
7. Save your settings and continue with the publish process

#### Setting Configuration Values with Azure CLI

You can also set application settings using Azure CLI:

```powershell
# For ApimSample.Api
az webapp config appsettings set --resource-group YourResourceGroup --name YourApiAppName --settings Authentication:Authority="https://login.microsoftonline.com/YOUR_TENANT_ID" Authentication:Audience="api://YOUR_API_CLIENT_ID" Authentication:SwaggerClientId="YOUR_SWAGGER_CLIENT_ID"

# For ApimSample.MvcClient (if deployed to Azure)
az webapp config appsettings set --resource-group YourResourceGroup --name YourMvcAppName --settings ApiSettings:BaseUrl="https://YOUR_APIM_NAME.azure-api.net" ApiSettings:ApiKey="YOUR_SUBSCRIPTION_KEY" Authentication:ClientId="YOUR_CLIENT_ID" Authentication:ClientSecret="YOUR_CLIENT_SECRET" Authentication:Authority="https://login.microsoftonline.com/YOUR_TENANT_ID"
```

#### Using Azure Key Vault for Sensitive Settings

For production environments, consider storing sensitive settings like client secrets in Azure Key Vault:

1. Create a Key Vault in the Azure Portal
2. Add your secrets to the Key Vault
3. Configure your App Service to use a Managed Identity
4. Grant the Managed Identity permission to access secrets in Key Vault
5. Use Key Vault references in your application settings:
   - `@Microsoft.KeyVault(SecretUri=https://YourKeyVault.vault.azure.net/secrets/YourSecret)`
