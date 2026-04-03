using Newtonsoft.Json;

namespace BgituGradesLoader.Save.Data
{
    [Serializable]
    public class SaveData
    {
        [JsonProperty] public SaveDataField<string> TableLink;

        public SaveData()
        {
            TableLink = new();
        }
    }
}
