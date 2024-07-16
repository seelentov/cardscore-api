using OpenQA.Selenium.Firefox;
using OpenQA.Selenium;

namespace cardscore_api.Services
{
    public class SeleniumService
    {
        private readonly FirefoxOptions _driverOptions;
        public SeleniumService()
        {
            FirefoxProfile profile = new FirefoxProfile();

            profile.SetPreference("browser.cache.disk.enable", false);
            profile.SetPreference("browser.cache.memory.enable", false);
            profile.SetPreference("browser.cache.offline.enable", false);
            profile.SetPreference("network.http.use-cache", false);
            profile.SetPreference("extensions.enabled", false);
            profile.SetPreference("browser.privatebrowsing.autostart", true);
            profile.SetPreference("permissions.default.stylesheet", 2);
            profile.SetPreference("permissions.default.image", 2);

            _driverOptions = new FirefoxOptions();
            _driverOptions.Profile = profile;
            _driverOptions.PageLoadStrategy = PageLoadStrategy.Eager;
            _driverOptions.AddArgument("--headless");
            _driverOptions.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:127.0) Gecko/20100101 Firefox/127.0");

        }
        public FirefoxDriver GetDriver()
        {
            return new FirefoxDriver(_driverOptions);
        }
    }
}
