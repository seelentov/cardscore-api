using System.Net;
using System.Security.Authentication;
using System.Text;

namespace cardscore_api.Services
{
    public class FetchService
    {
        private readonly HttpClient _client;

        public FetchService()
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler();

            _client = new HttpClient(httpClientHandler);
        }

        public async Task<string> GetAsync(string uri)
        {
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:127.0) Gecko/20100101 Firefox/127.0");
            _client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:127.0) Gecko/20100101 Firefox/127.0");
            _client.DefaultRequestHeaders.Add("Accept", "*/*");

            using HttpResponseMessage response = await _client.GetAsync(uri);

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> PostAsync(string uri, string data, string contentType)
        {
            using HttpContent content = new StringContent(data, Encoding.UTF8, contentType);

            HttpRequestMessage requestMessage = new HttpRequestMessage()
            {
                Content = content,
                Method = HttpMethod.Post,
                RequestUri = new Uri(uri)
            };

            using HttpResponseMessage response = await _client.SendAsync(requestMessage);

            return await response.Content.ReadAsStringAsync();
        }
    }


}
