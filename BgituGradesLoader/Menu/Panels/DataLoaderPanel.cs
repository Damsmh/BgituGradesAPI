using BgituGradesLoader.Compass;
using BgituGradesLoader.Compass.Objects;
using BgituGradesLoader.Database;
using BgituGradesLoader.Database.Tables;
using BgituGradesLoader.Save;
using BgituGradesLoader.Table;

namespace BgituGradesLoader.Menu.Panels
{
    public class DataLoaderPanel : ConsolePanel
    {
        private readonly SaveManager _saveManager;
        private readonly TableManager _tableManager;

        public DataLoaderPanel(SaveManager saveManager, TableManager tableManager)
        {
            _saveManager = saveManager;
            _tableManager = tableManager;
        }

        public override string Title => "Перенести данные с БГИТУ Компаса в БД";

        public static async Task RunHeadless(SaveManager saveManager, TableManager tableManager)
        {
            DataLoaderPanel panel = new(saveManager, tableManager);
            await panel.Run();
        }

        public async override Task Run()
        {
            Console.WriteLine("Загружаем информацию из таблицы...");
            await _tableManager.GenerateGroupsData();

            Console.WriteLine("Получаем список групп из Компаса...");
            List<CompassGroup> groups = await CompassManager.GetGroups();

            Console.WriteLine("Создаём список пар и дисциплин...");
            HashSet<string> disciplinesNames = [];
            List<CompassPair> pairs = [];

            for (int i = 0; i < groups.Count; i++)
            {
                CompassGroup group = groups[i];
                List<CompassPair>? groupPairs = await CompassManager.GetGroupPairs(group.Id);
                if (groupPairs is null || groupPairs.Count == 0)
                    continue;

                if (groupPairs[0].IsPlug())
                {
                    groups.RemoveAt(i);
                    i--;
                    continue;
                }

                pairs.AddRange(groupPairs);
                foreach (CompassPair pair in groupPairs)
                {
                    disciplinesNames.Add(pair.DisciplineName);
                    pair.SetGroupName(group.Name);
                }
            }

            Console.WriteLine("Нормализуем названия дисциплин...");
            Dictionary<string, string> normalizedDisciplineNames = [];

            foreach (string discipline in disciplinesNames)
            {
                string normalizedDiscipline = discipline.NormalizeDisciplineForFiltering();
                if (normalizedDisciplineNames.TryGetValue(normalizedDiscipline, out string? bestDisciplineName))
                {
                    if (bestDisciplineName.CountExtraSymbols() > normalizedDiscipline.CountExtraSymbols())
                        normalizedDisciplineNames[normalizedDiscipline] = discipline;
                }
                else
                {
                    normalizedDisciplineNames[normalizedDiscipline] = discipline;
                }
            }

            Console.WriteLine("Очищаем базу данных...");
            await DatabaseManager.NukeDatabase();

            Console.WriteLine("Добавляем группы...");
            Dictionary<string, DatabaseGroup> groupsDictionary = [];
            foreach (CompassGroup group in groups)
            {
                DatabaseGroup? databaseGroup = _tableManager.GetGroupData(group.Name);
                if (databaseGroup == null)
                {
                    Console.WriteLine($"Не удалось получить информацию о группе {group.Name}");
                    continue;
                }

                databaseGroup.SetName(group.Name);
                databaseGroup = await DatabaseManager.AddGroup(databaseGroup);
                groupsDictionary.Add(group.Name, databaseGroup);
            }

            Console.WriteLine("Добавляем дисциплины...");
            Dictionary<string, DatabaseDiscipline> disciplinesDictionary = [];
            foreach (var disciplineNames in normalizedDisciplineNames)
            {
                DatabaseDiscipline databaseDiscipline = new(disciplineNames.Value);
                databaseDiscipline = await DatabaseManager.AddDiscipline(databaseDiscipline);
                disciplinesDictionary.Add(disciplineNames.Key, databaseDiscipline);
            }

            Console.WriteLine("Добавляем пары...");
            foreach (CompassPair pair in pairs)
            {
                DatabasePair databasePair = new(pair);

                string normalizedDiscipline = pair.DisciplineName.NormalizeDisciplineForFiltering();
                databasePair.SetDiscipline(disciplinesDictionary[normalizedDiscipline]);

                DatabaseGroup group = groupsDictionary[pair.GroupName];
                databasePair.SetGroup(group);

                await DatabaseManager.AddPair(databasePair);
            }

            Console.WriteLine("Готово!");
        }
    }
}
