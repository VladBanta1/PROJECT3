using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Globalization;

namespace EatUp.Services
{
    public class GeocodingService
    {
        private readonly HttpClient _httpClient;

        public GeocodingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(double lat, double lon)?> GetCoordinatesAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null;

            var url =
                $"https://nominatim.openstreetmap.org/search" +
                $"?q={System.Net.WebUtility.UrlEncode(address)}" +
                $"&format=json&limit=1";

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("EatUpApp/1.0");

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            var data = JsonSerializer.Deserialize<JsonElement>(json);

            if (data.GetArrayLength() == 0)
                return null;

            var item = data[0];

           

            double lat = double.Parse(
                item.GetProperty("lat").GetString(),
                CultureInfo.InvariantCulture
            );

            double lon = double.Parse(
                item.GetProperty("lon").GetString(),
                CultureInfo.InvariantCulture
            );


            return (lat, lon);
        }
    }
}
