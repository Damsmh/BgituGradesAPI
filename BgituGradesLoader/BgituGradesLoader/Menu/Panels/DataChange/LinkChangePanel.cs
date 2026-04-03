using BgituGradesLoader.Save;
using BgituGradesLoader.Save.Data;

namespace BgituGradesLoader.Menu.Panels.DataChange
{
    public class LinkChangePanel(SaveManager saveManager) : DataChangePanel<string>(saveManager)
    {
        public override string Title => "Обновить ссылку на календарный учебный график";

        private const string LINK_START = "https://bgitu.ru/";

        protected override SaveDataField<string> GetDataFieldFromSaveManager()
        {
            return _saveManager.SaveData.TableLink;
        }

        protected override string GetNewDataFromUser()
        {
            Console.WriteLine("Введите новую ссылку на календарный учебный график: ");
            string? link = Console.ReadLine();
            link ??= String.Empty;
            link = LINK_START + link;
            return link;
        }
    }
}
