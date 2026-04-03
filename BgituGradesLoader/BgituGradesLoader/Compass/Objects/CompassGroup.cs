using Newtonsoft.Json;
using System.Text;

namespace BgituGradesLoader.Compass.Objects
{
    [Serializable]
    public class CompassGroup
    {
        [JsonProperty] private readonly int id;
        [JsonProperty] private readonly string? name;

        public int Id => id;
        public string Name
        {
            get
            {
                if (name == null)
                    return string.Empty;
                return name;
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new();
            builder.AppendLine($"ID: {id}");
            builder.AppendLine($"Название: {name}");
            builder.AppendLine();
            return builder.ToString();
        }
    }
}
