using BgituGradesLoader.Database;
using BgituGradesLoader.Database.Tables;
using BgituGradesLoader.Save.Data;

namespace BgituGradesLoader.Save
{
    public class SaveManager
    {
        private const string FILE_NAME = "data";

        private readonly FileManager<SaveData> _fileManager;
        private readonly SaveData _data;

        public SaveData SaveData => _data;

        public SaveManager()
        {
            _fileManager = new(FILE_NAME);

            SaveData? data = _fileManager.Load();
            data ??= new();
            _data = data;
        }

        public void Save()
        {
            _fileManager.Save(_data);
        }

        public static async Task<SaveManager> CreateFromApiAsync()
        {
            var manager = new SaveManager();
            string? tableLink = await DatabaseManager.GetTableLink();
            if (tableLink != null)
            {
                manager._data.TableLink.Data = tableLink;
                manager._data.TableLink.LastChange = DateTime.UtcNow;
            }
            return manager;
        }
    }
}
