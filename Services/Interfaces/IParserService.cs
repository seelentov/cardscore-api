using cardscore_api.Models;
using HtmlAgilityPack;

namespace cardscore_api.Services
{
    public interface IParserService
    {
        Task<League> GetLeagueDataByUrl(string url);
        int GetLeagueGamesCount(string url);
        int GetPageGameCount(string url);
        Task<LeagueIncludeGames> GetDataByUrl(string url);
        Task<List<Game>> ParsePage(string url, string h1Element);
        Task<Player> ParsePlayer(string url, string leagueName);
        Task<Game> ParseGameByPage(string url, string leagueName);
        Task<Game> ParseGame(HtmlNode htmlNode, string leagueName, bool parseActions = true, bool onlyActive = false);
        bool CanParseDDMM(string dateString);
        DateTime ParseDateTimeDDMM(string input);
        bool TryParseDateTime(string dateString);
        DateTime ParseDateTime(string input);
        string ClearString(string str);
        int ToInt(string str);


    }
}
