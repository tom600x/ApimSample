using System.Text.Json;
using ApimSample.MvcClient.Models;

namespace ApimSample.MvcClient.Services;

public interface IWeatherService
{
    Task<WeatherForecastViewModel> GetWeatherForecastAsync(string apiSource);
}

public class WeatherService : IWeatherService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<WeatherService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<WeatherForecastViewModel> GetWeatherForecastAsync(string apiSource)
    {
        var viewModel = new WeatherForecastViewModel { ApiSource = apiSource };
        
        try
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            
            // Add APIM subscription key to header
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _configuration["ApiSettings:ApiKey"]);
            
            // Set the base URL based on which API we're targeting
            string endpoint;
            
            if (apiSource == ApiSource.DirectAuth)
            {
                endpoint = "/direct-auth/weatherforecast";
            }
            else // ApiSource.ApimAuth
            {
                endpoint = "/apim-auth/weatherforecast";
            }
            
            var response = await client.GetAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var weatherData = JsonSerializer.Deserialize<IEnumerable<WeatherForecast>>(content, options);
                viewModel.Forecasts = weatherData ?? Enumerable.Empty<WeatherForecast>();
                viewModel.Success = true;
            }
            else
            {
                _logger.LogError("API request to {ApiSource} failed with status code {StatusCode}", 
                    apiSource, response.StatusCode);
                viewModel.Success = false;
                viewModel.ErrorMessage = $"API returned status code: {(int)response.StatusCode} - {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weather forecast data from {ApiSource}", apiSource);
            viewModel.Success = false;
            viewModel.ErrorMessage = $"Error: {ex.Message}";
        }
        
        return viewModel;
    }
}
