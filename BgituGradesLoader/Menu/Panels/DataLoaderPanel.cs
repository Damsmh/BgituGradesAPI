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

            var tasks = groups.Select(async group => (group, pairs: await CompassManager.GetGroupPairs(group.Id)));
            IEnumerable<(CompassGroup group, List<CompassPair> pairs)> results = await Task.WhenAll(tasks);
            Console.WriteLine("Группы и их расписание получено...");
            List<int> toRemove = [];
            foreach (var (group, groupPairs) in results)
            {
                if (groupPairs is null || groupPairs.Count == 0)
                    continue;

                if (groupPairs[0].IsPlug())
                {
                    toRemove.Add(groups.IndexOf(group));
                    continue;
                }

                pairs.AddRange(groupPairs);
                foreach (CompassPair pair in groupPairs)
                {
                    disciplinesNames.Add(pair.DisciplineName);
                    pair.SetGroupName(group.Name);
                }
            }

            foreach (int idx in toRemove.OrderByDescending(x => x))
                groups.RemoveAt(idx);

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
            List<DatabaseGroup> groupsToAdd = [];
            foreach (CompassGroup group in groups)
            {
                DatabaseGroup? databaseGroup = _tableManager.GetGroupData(group.Name);
                if (databaseGroup == null)
                {
                    Console.WriteLine($"Не удалось получить информацию о группе {group.Name}");
                    continue;
                }

                databaseGroup.SetName(group.Name);
                groupsToAdd.Add(databaseGroup);
            }

            List<DatabaseGroup> addedGroups = await DatabaseManager.AddGroups(groupsToAdd);
            foreach (DatabaseGroup group in addedGroups)
            {
                groupsDictionary.TryAdd(group.Name, group);
            }

            Console.WriteLine("Добавляем дисциплины...");
            List<string> normalizedKeys = normalizedDisciplineNames.Keys.ToList();
            List<DatabaseDiscipline> disciplinesToAdd = normalizedKeys
                .Select(key => new DatabaseDiscipline(normalizedDisciplineNames[key]))
                .ToList();

            List<DatabaseDiscipline> addedDisciplines = await DatabaseManager.AddDisciplines(disciplinesToAdd);

            var disciplinesDictionary = new Dictionary<string, DatabaseDiscipline>();
            foreach (var disc in addedDisciplines)
            {
                string key = disc.Name.NormalizeDisciplineForFiltering();
                disciplinesDictionary[key] = disc;
            }

            Console.WriteLine("Добавляем пары...");
            List<DatabasePair> pairsToAdd = [];
            foreach (CompassPair pair in pairs)
            {
                DatabasePair databasePair = new(pair);

                string normalizedDiscipline = pair.DisciplineName.NormalizeDisciplineForFiltering();
                if (!disciplinesDictionary.TryGetValue(normalizedDiscipline, out DatabaseDiscipline? discipline))
                {
                    continue;
                }
                databasePair.SetDiscipline(discipline);

                if (!groupsDictionary.TryGetValue(pair.GroupName, out DatabaseGroup? group))
                {
                    Console.WriteLine($"Не найдена группа: '{pair.GroupName}'");
                    continue;
                }
                databasePair.SetGroup(group);

                pairsToAdd.Add(databasePair);
            }
            await DatabaseManager.AddPairs(pairsToAdd);
            Console.WriteLine("Готово!");
        }
    }
}
