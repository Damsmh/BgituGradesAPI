using Newtonsoft.Json;

namespace BgituGradesLoader.Compass.Objects
{
    [Serializable]
    public class CompassConfig
    {
        [JsonProperty] private DateTime termStartDate;


        public DateTime AproximateWeek => termStartDate;
        public int AproximateWeekNumber
        {
            get
            {
                int weekNum = (DateTime.Now - termStartDate).Days / 7 + 1;

                if (weekNum % 2 == 0)
                    return 2;
                return 1;
            }
        }
    }
}
