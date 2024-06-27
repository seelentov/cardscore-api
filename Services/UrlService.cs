using System.Web;

namespace cardscore_api.Services
{
    public class UrlService
    {
        public UrlService() { }
        public string NormalizeUrl(string url)
        {
            url = url.Replace("%2F", "/");

            return url;
        }

        public string Encode(string url)
        {
            return HttpUtility.UrlPathEncode(url).ToUpper();
        }
    }
}
