using BgituGradesLoader.Compass.Objects;
using Newtonsoft.Json;
using System.Net;
using System.Web;

namespace BgituGradesLoader.Compass
{
    public static class CompassManager
    {
        private const string API_LINK = "https://api-ssl.bgitu-compass.ru/";
        private const string GROUPS_LINK = API_LINK + "groups";
        private const string SCHEDULE_LINK = API_LINK + "v3/lessons";
        private const string CONFIG_LINK = API_LINK + "remoteConfig";

        private static readonly HttpClient _httpClient = new();
        public static async Task<List<CompassPair>?> GetGroupPairs(int groupID)
        {
            UriBuilder uriBuilder = new(SCHEDULE_LINK);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["groupId"] = groupID.ToString();
            uriBuilder.Query = query.ToString();
            string url = uriBuilder.ToString();

            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (response.StatusCode != HttpStatusCode.OK)
                return null;

            string content = await response.Content.ReadAsStringAsync();
            return GetPairsFromContent(content);
        }

        private static List<CompassPair>? GetPairsFromContent(string content)
        {
            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<CompassPair>>>>(content);
            List<CompassPair> result = [];

            if (dictionary == null)
                return result;

            List<string> weeks = [.. dictionary.Keys];
            if (weeks.Count == 0)
                return result;
            List<string> days = [.. dictionary[weeks[0]].Keys];

            for (int weekNum = 0; weekNum < weeks.Count; weekNum++)
            {
                string? week = weeks[weekNum];
                for (int dayNum = 0; dayNum < days.Count; dayNum++)
                {
                    string? day = days[dayNum];
                    foreach (CompassPair pair in dictionary[week][day])
                    {
                        pair.SetWeekAndDay(weekNum + 1, dayNum + 1);
                        result.Add(pair);
                    }
                }
            }

            return result;
        }

        public static async Task<List<CompassGroup>> GetGroups()
        {
            HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync(GROUPS_LINK);

            if (response.StatusCode != HttpStatusCode.OK)
                return [];

            string content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<CompassGroup>>(content);
            result ??= [];
            return result;
        }

        public static async Task<CompassConfig?> GetConfig()
        {
            HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync(CONFIG_LINK);

            string content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<CompassConfig>(content);
            return result;
        }
    }
}
