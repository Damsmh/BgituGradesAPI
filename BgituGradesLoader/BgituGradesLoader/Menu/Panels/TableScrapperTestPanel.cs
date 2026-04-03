using BgituGradesLoader.Compass;
using BgituGradesLoader.Compass.Objects;
using BgituGradesLoader.Database.Tables;
using BgituGradesLoader.Table;

namespace BgituGradesLoader.Menu.Panels
{
    public class TableScrapperTestPanel : ConsolePanel
    {
        private readonly TableManager _tableManager;

        public TableScrapperTestPanel(TableManager tableManager)
        {
            _tableManager = tableManager;
        }

        public override string Title => "Протестировать скраппинг таблицы";

        public async override Task Run()
        {
            Console.WriteLine("Загружаем информацию из таблицы...");
            await _tableManager.GenerateGroupsData();

            Console.WriteLine("Получаем список групп из Компаса...");
            List<CompassGroup> groups = await CompassManager.GetGroups();

            Console.WriteLine("Группы, для которых не было найдено информации:");
            foreach (CompassGroup group in groups)
            {
                DatabaseGroup? groupData = _tableManager.GetGroupData(group.Name);
                if (groupData == null)
                    Console.WriteLine($"{group.Name}");
            }
        }
    }
}
