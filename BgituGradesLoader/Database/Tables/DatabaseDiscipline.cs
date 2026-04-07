using Newtonsoft.Json;

namespace BgituGradesLoader.Database.Tables
{
    [Serializable]
    public class DatabaseDiscipline
    {
        [JsonProperty] private readonly int id;
        [JsonProperty] private readonly string? name;

        public int Id => id;
        public string? Name => name;

        public DatabaseDiscipline(string? name)
        {
            this.name = DatabaseUtils.NormalizeDisciplineForDatabase(name);
        }
    }
}
