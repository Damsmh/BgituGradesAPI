using System.Text;
using System.Text.RegularExpressions;

namespace BgituGradesLoader.Table
{
    public static class GroupNameUtils
    {
        private static readonly Dictionary<string, string> _groupNamesExceptions = new() {
            { "Программная инженерия", "ПРИ" },
            { "Садоводство", "САД" },
            { "Экономика", "ЭКОН" },
            { "Природообустройство и водопользование", "ПТ" },
            { "Теплогазоснабжение и вентиляция", "ТГСВ" },
            { "Автомобильные дороги и аэродромы", "АД" },
            { "Строительство", "СТР" },
            { "Энерго- и ресурсосберегающие процессы в химической технологии, нефтехимии и биотехнологии", "ЭРСП" },
            { "Садово-парковое и ландшафтное строительство", "СПС" },
            { "Менеджмент", "МН" },
            { "Технология деревообработки, проектирование мебели и интерьеров", "ТД" },
            { "Машины и технологии лесопромышленных производств и транспортных процессов", "МЛП" },
            { "Производство и применение строительных материалов, изделий и конструкций", "ПСК" },
            { "Строительство и эксплуатация автомобильных дорог, аэродромов и городских путей сообщения", "АД" },
            { "Строительство и эксплуатация автомобильных дорог и аэродромов", "АД" },
            { "Лесное дело", "ЛХ" },
            { "Техническое обслуживание и ремонт автотранспортных средств", "ТО" },
            { "Техническое обслуживание и ремонт двигателей, систем и агрегатов автомобилей", "ТО" },
            { "Технологические машины и оборудование", "ММ"},
        };

        private static readonly Dictionary<string, string> _groupNamesMasterExceptions = new() {
            { "Лесное дело", "ЛД" },
            { "Технологические машины и оборудование", "ТМО"}
        };

        public static string GenerateGroupName(string groupName, DirectionType directionType, int course)
        {
            StringBuilder result = new();
            result.Append(ReduceGroupName(groupName, directionType));
            switch (directionType)
            {
                case DirectionType.MasterDegree: result.Append('м'); break;
                case DirectionType.Accelerated: result.Append('у'); break;
                case DirectionType.SPO11: case DirectionType.SPO9: result.Append("(СПО)"); break;
            }

            result.Append('-');
            result.Append(course);

            switch (directionType)
            {
                case DirectionType.SPO9: result.Append("09"); break;
                case DirectionType.SPO11: result.Append("11"); break;
                default: result.Append("01"); break;
            }

            return result.ToString();
        }

        public static string SimplyfyGroupName(string groupName)
        {
            Regex regex = new(@"\((а|б|А|Б)\)");
            groupName = regex.Replace(groupName, "");

            string[] groupParts = groupName.Split("-");
            bool isSpecial = groupParts[0].EndsWith('м') || groupParts[0].EndsWith('у');
            groupParts[0] = groupParts[0].ToUpper();
            if (isSpecial)
            {
                char lastSymbol = groupParts[0][^1];
                groupParts[0] = groupParts[0][..^1] + lastSymbol.ToString().ToLower();
            }
            if (groupParts.Count() == 3)
            {
                groupParts = groupParts[..^1];
            }
            groupName = string.Join("-", groupParts);

            if (!groupName.EndsWith("01") && !groupName.EndsWith("09") && !groupName.EndsWith("11"))
            {
                groupName = groupName[..^2];
                groupName += "01";
            }

            return groupName;
        }

        private static string ReduceGroupName(string groupName, DirectionType directionType)
        {
            if (directionType == DirectionType.MasterDegree && _groupNamesMasterExceptions.TryGetValue(groupName, out string? masterVersion))
                return masterVersion;

            if (_groupNamesExceptions.TryGetValue(groupName, out string? shortVersion))
                return shortVersion;

            StringBuilder result = new();
            groupName = groupName.Replace(",", "").ToUpper();
            foreach (var word in groupName.Split())
            {
                if (word == "И" || word == "ПО")
                    continue;

                result.Append(word[0]);
            }
            return result.ToString();
        }

        public static string AddMasterToGroupName(string name)
        {
            string[] groupParts = name.Split("-");
            groupParts[0] = groupParts[0] + "м";
            return string.Join("-", groupParts);
        }
    }
}
