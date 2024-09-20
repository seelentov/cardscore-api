using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using WebDriverManager.DriverConfigs.Impl;
using static WebDriverManager.DriverManager;
using OpenQA.Selenium.Firefox;


namespace cardscore_api.Services
{
    public class SeleniumService
    {
        private readonly ChromeOptions _driverOptions;
        public SeleniumService()
        {
            ChromeOptions options = new ChromeOptions();

            /*            options.AddUserProfilePreference("profile.default_content_settings.images", 2); // Блокирует изображения
                        options.AddUserProfilePreference("profile.default_content_settings.stylesheets", 2); // Блокирует стили
                        options.AddUserProfilePreference("profile.managed_default_content_settings.images", 2); // Блокирует изображения
                        options.AddUserProfilePreference("profile.managed_default_content_settings.stylesheets", 2); // Блокирует стили
                        options.AddUserProfilePreference("cache.disk_cache_size", 0); // Отключает кэширование на диске
                        options.AddUserProfilePreference("cache.memory_cache_size", 0); // Отключает кэширование в памяти
                        options.AddUserProfilePreference("cache.enable", false); // Отключает кэширование
                        options.AddUserProfilePreference("extensions.enabled", false); // Отключает расширения
                        options.AddUserProfilePreference("privacy.clear_browsing_data_on_exit", true); // Очистка данных при выходе
                          options.AddArgument("--disable-features=NetworkService");
                          options.AddArgument("--disable-extensions");
              options.AddArgument("--disable-features=NetworkService");

                        options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36");
            options.AddArgument("--headless=new");
            options.AddArgument("disable-gpu");
              options.AddArgument("no-sandbox");

                        options.AddArgument("--no-sandbox");

                        options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--window-size=1920x1080");

              */


            options.AddArgument("--headless");


            _driverOptions = options;
            _driverOptions.PageLoadStrategy = PageLoadStrategy.Eager;

        }
        public ChromeDriver GetDriver()
        {
            return new ChromeDriver(_driverOptions);
        }

        public FirefoxDriver GetFirefox()
        {
            return new FirefoxDriver();
        }
    }
}