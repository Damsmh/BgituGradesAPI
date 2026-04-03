using Newtonsoft.Json;

namespace BgituGradesLoader.Save.Data
{
    [Serializable]
    public class SaveDataField<T>
    {
        [JsonProperty] public T? Data { get; set; }
        [JsonProperty] public DateTime LastChange { get; set; }
    }
}
