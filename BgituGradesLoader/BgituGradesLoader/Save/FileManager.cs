using Newtonsoft.Json;

namespace BgituGradesLoader.Save
{
    public class FileManager<TData> where TData : class
    {
        private readonly string _filePath;
        private const string _fileExtension = "nyt";

        public FileManager(string fileName)
        {
            _filePath = "/" + string.Concat(fileName, ".", _fileExtension);
        }

        public void Save(TData data)
        {
            File.WriteAllText(_filePath, JsonConvert.SerializeObject(data));
        }

        public TData? Load()
        {
            if (!IsFileExists())
                return null;
            return JsonConvert.DeserializeObject<TData>(File.ReadAllText(_filePath));
        }

        public void Delete()
        {
            if (!IsFileExists())
                return;

            File.Delete(_filePath);
        }

        public bool IsFileExists()
        {
            return File.Exists(_filePath);
        }
    }
}
