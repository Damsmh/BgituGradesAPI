using Newtonsoft.Json;
using System.Text;

namespace BgituGradesLoader.Database.Tables
{
    [Serializable]
    public class DatabaseGroup
    {
        [JsonProperty] private readonly int id;
        [JsonProperty] private string? name;
        [JsonProperty] private readonly string studyStartDate;
        [JsonProperty] private readonly string studyEndDate;
        [JsonProperty] private readonly int startWeekNumber;

        public int Id => id;
        public string? Name => name;

        public DatabaseGroup(string startDate, string endDate, int startWeekNumber)
        {
            this.studyStartDate = startDate;
            this.studyEndDate = endDate;
            this.startWeekNumber = startWeekNumber;
        }

        public DatabaseGroup Copy()
        {
            DatabaseGroup copy = new(studyStartDate, studyEndDate, startWeekNumber);
            return copy;
        }

        public void SetName(string newName)
        {
            name = newName;
        }

        public override string ToString()
        {
            StringBuilder builder = new();
            builder.AppendLine($"Группа: {name}");
            builder.AppendLine($"Начало занятий: {studyStartDate}");
            builder.AppendLine($"Номер недели начала занятий: {startWeekNumber}");
            builder.AppendLine($"Конец занятий: {studyEndDate}");
            return builder.ToString();
        }
    }
}
