using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MappAI.Services
{
    public class WeatherApiService
    {
        private readonly HttpClient _httpClient;

        public WeatherApiService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<double> GetRealTimeWeatherRiskAsync()
        {
            try
            {
        
                string url = "https://api.open-meteo.com/v1/forecast?latitude=41.042&longitude=29.008&current=precipitation,weather_code";
                var response = await _httpClient.GetStringAsync(url);
                
                using JsonDocument doc = JsonDocument.Parse(response);
                var current = doc.RootElement.GetProperty("current");
                double precipitation = current.GetProperty("precipitation").GetDouble();
                int weatherCode = current.GetProperty("weather_code").GetInt32();

                if (weatherCode >= 95) return 1.0; 
                if (weatherCode >= 71) return 0.8; 
                if (weatherCode >= 51 || precipitation > 0) return 0.6; 
                if (weatherCode >= 45) return 0.5; 
                
                return 0.1; 
            }
            catch
            {
                return 0.2; 
            }
        }
    }
}