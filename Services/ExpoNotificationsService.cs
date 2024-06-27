using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace cardscore_api.Services
{
    public class ExpoNotificationsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _url = "https://exp.host/--/api/v2/push/send";
        private readonly ErrorsService _errorsService;
        public ExpoNotificationsService(ErrorsService errorsService)
        {
            _httpClient = new HttpClient();
            _errorsService = errorsService;
        }

        public async Task ThrowNotification(string title, string body, string token)
        {
            var message = new
            {
                to = token,
                title,
                body,
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(message);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_url, content);

            if (!response.IsSuccessStatusCode)
            {
                _errorsService.CreateErrorFile(response);
            }

        }
    }
}
