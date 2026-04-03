using BgituGradesLoader.Compass.Objects;
using Newtonsoft.Json;

namespace BgituGradesLoader.Database.Tables
{
    [Serializable]
    public class DatabasePair
    {
        [JsonProperty] private int id;
        [JsonProperty] private string type;
        [JsonProperty] private int weekNumber;
        [JsonProperty] private int weekDay;
        [JsonProperty] private string startAt;
        [JsonProperty] private int disciplineId;
        [JsonProperty] private int groupId;

        [JsonConstructor]
        public DatabasePair(int id, string type, int weekNumber, int weekDay, string startAt, int disciplineId, int groupId)
        {
            this.id = id;
            this.type = type;
            this.weekNumber = weekNumber;
            this.weekDay = weekDay;
            this.startAt = startAt;
            this.disciplineId = disciplineId;
            this.groupId = groupId;
        }

        public DatabasePair(CompassPair compassPair)
        {
            type = DatabaseUtils.GetPairType(compassPair.IsLecture);
            weekNumber = compassPair.WeekNumber;
            weekDay = compassPair.DayNumber;

            string plugDate = DateTime.Now.ToString("yyyy-MM-dd");
            startAt = $"{plugDate}T{compassPair.StartAt}Z";
        }

        public void SetDiscipline(DatabaseDiscipline discipline)
        {
            disciplineId = discipline.Id;
        }

        public void SetGroup(DatabaseGroup group)
        {
            groupId = group.Id;
        }
    }
}
