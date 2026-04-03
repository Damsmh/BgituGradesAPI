using Newtonsoft.Json;
using System.Text;

namespace BgituGradesLoader.Compass.Objects
{
    [Serializable]
    public class CompassPair
    {
        [JsonProperty] private readonly string? subjectName;
        [JsonProperty] private readonly string? startAt;
        [JsonProperty] private readonly bool isLecture;

        private int _weekNumber;
        private int _dayNumber;
        private string? _groupName;

        private const string PLUG_DISCIPLINE = "Выбранная группа не существует. Выберите новую.";

        public bool IsLecture => isLecture;
        public int WeekNumber => _weekNumber;
        public int DayNumber => _dayNumber;

        public string GroupName
        {
            get
            {
                if (_groupName == null)
                    return string.Empty;
                return _groupName;
            }
        }

        public string DisciplineName
        {
            get
            {
                if (subjectName == null)
                    return string.Empty;
                return subjectName;
            }
        }

        public string StartAt
        {
            get
            {
                if (startAt == null)
                    return string.Empty;
                return startAt;
            }
        }

        public void SetWeekAndDay(int weekNumber, int dayNumber)
        {
            _weekNumber = weekNumber;
            _dayNumber = dayNumber;
        }

        public void SetGroupName(string groupName)
        {
            _groupName = groupName;
        }

        public override string ToString()
        {
            StringBuilder builder = new();
            builder.AppendLine($"Предмет: {subjectName}");
            builder.AppendLine($"Начинается: {startAt}");
            builder.AppendLine($"Лекция: {isLecture}");
            builder.AppendLine($"Номер недели: {_weekNumber}");
            builder.AppendLine($"Номер дня: {_dayNumber}");
            builder.AppendLine();
            return builder.ToString();
        }

        public bool IsPlug()
        {
            return DisciplineName.Equals(PLUG_DISCIPLINE);
        }
    }
}
