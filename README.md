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

#### Configure Direct Auth API (API validates tokens)

1. Go to **APIs** > **Add API** > **OpenAPI**
2. Enter the Direct Auth API's Swagger URL (e.g., `https://your-api-url/swagger/v1/swagger.json`)
3. Set the **API URL suffix** to `direct-auth`
4. Click **Create**
5. Go to **APIs** > Select your Direct Auth API > **Settings**
6. Under **Security**, select **OAuth 2.0**
7. Create or select an OAuth 2.0 server:
   - **Display name**: `ApimSample OAuth`
   - **Client registration page URL**: (Leave blank for this demo)
   - **Authorization endpoint URL**: `https://login.microsoftonline.com/YOUR_TENANT_ID/oauth2/authorize`
   - **Token endpoint URL**: `https://login.microsoftonline.com/YOUR_TENANT_ID/oauth2/token`
   - **Default scope**: `api://YOUR_API_CLIENT_ID/weather.read`
8. Click **Save**
9. On the API's **Settings** page, select the OAuth 2.0 server you created and check the appropriate scopes

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
    <openid-config url="https://login.microsoftonline.com/YOUR_TENANT_ID/.well-known/openid-configuration" />
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

## Running the Solution

1. First, publish both APIs to Azure:
   ```
   cd ApimSample.Api
   dotnet publish -c Release
   
   cd ../ApimSample.ApimSecuredApi
   dotnet publish -c Release
   ```

2. Deploy both published APIs to Azure App Service (through Visual Studio, Azure CLI, or other methods)

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
