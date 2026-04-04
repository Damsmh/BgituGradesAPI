using BgituGradesLoader.Compass;
using BgituGradesLoader.Compass.Objects;
using BgituGradesLoader.Database;
using BgituGradesLoader.Database.Tables;
using BgituGradesLoader.Save;
using BgituGradesLoader.Table;
using System.Text.RegularExpressions;

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

        private static class GroupCourseParser
        {
            private static readonly Regex CourseRegex =
                new(@"-(\d)\d{2}", RegexOptions.Compiled);

            public static int Parse(string? name)
            {
                var match = CourseRegex.Match(name!);
                return int.Parse(match.Groups[1].Value);
            }
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
            List<DatabaseGroup> databaseGroups = [];
            foreach (CompassGroup group in groups)
            {
                DatabaseGroup? databaseGroup = _tableManager.GetGroupData(group.Name);
                if (databaseGroup == null)
                {
                    Console.WriteLine($"Не удалось получить информацию о группе {group.Name}");
                    continue;
                }
                databaseGroup.SetName(group.Name);
                databaseGroup.SetCourseNumber(GroupCourseParser.Parse(group.Name));
                databaseGroups.Add(databaseGroup);
            }

            Console.WriteLine("Добавляем дисциплины...");
            List<DatabaseDiscipline> databaseDisciplines = normalizedDisciplineNames
                .Select(d => new DatabaseDiscipline(d.Value)).ToList();

            Dictionary<string, DatabaseGroup> groupsByName = databaseGroups
                .ToDictionary(g => g.Name!);
            Dictionary<string, DatabaseDiscipline> disciplinesByNormalized = databaseDisciplines
                .Zip(normalizedDisciplineNames.Keys, (d, k) => (k, d))
                .ToDictionary(x => x.k, x => x.d);

            Console.WriteLine("Добавляем пары...");
            List<DatabasePair> databasePairs = [];
            foreach (CompassPair pair in pairs)
            {
                DatabasePair databasePair = new(pair);

                string normalizedDiscipline = pair.DisciplineName.NormalizeDisciplineForFiltering();
                databasePair.SetDiscipline(disciplinesByNormalized[normalizedDiscipline]);

                DatabaseGroup group = groupsByName[pair.GroupName];
                databasePair.SetGroup(group);

                databasePairs.Add(databasePair);
            }

            await DatabaseManager.AddAllSchedule(databaseGroups, databaseDisciplines, databasePairs);

            Console.WriteLine("Готово!");
        }
    }
}
