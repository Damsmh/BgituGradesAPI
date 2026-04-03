using BgituGradesLoader.Compass.Objects;
using BgituGradesLoader.Database.Tables;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text.RegularExpressions;

namespace BgituGradesLoader.Table
{
    public class TableData
    {
        private readonly List<string> _mergedCellsList;
        private readonly SharedStringTable _stringTable;

        private readonly List<string> _directions;
        private readonly List<string> _profiles;
        private List<string> _groupNames;
        private readonly List<TableCalendar> _calendars;

        private TableState _nowState;
        private DirectionType _directionType;

        public TableState NowState => _nowState;

        public TableData(List<string> mergedCellsList, SharedStringTable stringTable)
        {
            _mergedCellsList = mergedCellsList;
            _stringTable = stringTable;

            _directions = [];
            _profiles = [];
            _calendars = [];
            _groupNames = [];

            _nowState = TableState.CollectGroupNames;
            _directionType = DirectionType.Bachelor;
        }

        public void AddRow(Row row)
        {
            Cell firstCell = row.Elements<Cell>().First();
            string cellText = TableUtils.GetTextFromCell(firstCell, _stringTable);

            switch (_nowState)
            {
                case TableState.CollectGroupNames:
                    CollectGroupNameFromCell(cellText);
                    break;
                case TableState.WaitForCalendars:
                    if (cellText != TableUtils.EMPTY_CELL_PLACEHOLDER)
                    {
                        _nowState = TableState.CollectCalendars;
                        CollectCalendarFromRow(row, cellText);
                    }
                    break;
                case TableState.CollectCalendars:
                    CollectCalendarFromRow(row, cellText);
                    break;
            }
        }

        private void CollectGroupNameFromCell(string cell)
        {
            if (cell == TableUtils.EMPTY_CELL_PLACEHOLDER)
            {
                GenerateGroupNamesFromProfilesAndDirections();
                _nowState = TableState.WaitForCalendars;
                return;
            }

            if (cell.Contains("очная ускоренная форма"))
                _directionType = DirectionType.Accelerated;
            if (cell.Contains("(9 кл.)"))
                _directionType = DirectionType.SPO9;
            if (cell.Contains("(11 кл.)"))
                _directionType = DirectionType.SPO11;

            Regex regex = new(@"профиль\s+\""([^\""]+)\""");
            MatchCollection matches = regex.Matches(cell);
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    string pureName = match.Value.Split("\"")[1];
                    _profiles.Add(pureName);
                }
            }

            Regex regexBrackets = new(@"\(([^\""]+)\)");
            string nameWithoutBrackets = regexBrackets.Replace(cell, "").Trim();

            if (cell.StartsWith("направление подготовки"))
            {
                string directionName = string.Join(" ", nameWithoutBrackets.Split().Skip(3));
                _directions.Add(directionName);
            }

            if (cell.StartsWith("специальность"))
            {
                string directionName = string.Join(" ", nameWithoutBrackets.Split().Skip(2));
                _directions.Add(directionName);
            }
        }

        private void GenerateGroupNamesFromProfilesAndDirections()
        {
            if (_profiles.Count > 0)
                _groupNames = _profiles;
            else
                _groupNames = _directions;
        }

        public void CollectCalendarFromRow(Row row, string firstCellText)
        {
            Regex regex = new(@"\d курс(\w*)");
            MatchCollection matches = regex.Matches(firstCellText);

            if (matches.Count > 0)
            {
                int courseNumber = Convert.ToInt32(firstCellText.Split()[0]);
                TableCalendar calendar = new(courseNumber, _mergedCellsList, _stringTable);
                _calendars.Add(calendar);
            }

            TableCalendar lastCalendar = _calendars[^1];
            if (lastCalendar.Rows == 6)
            {
                _nowState = TableState.ForceEnd;
            }
            else
            {
                lastCalendar.AddRow(row);
            }
        }

        public List<DatabaseGroup> CompileTable(CompassConfig config)
        {
            List<DatabaseGroup> groups = [];
            foreach (var calendar in _calendars)
            {
                calendar.CompileCalendar();
                if (calendar.HaveDiplom() && _directionType == DirectionType.Bachelor && _calendars[^1].GroupCourse == 2)
                    _directionType = DirectionType.MasterDegree;
            }

            foreach (var calendar in _calendars)
            {
                DatabaseGroup group = calendar.GetGroupData(config);
                foreach (var groupName in _groupNames)
                {
                    string shortName = GroupNameUtils.GenerateGroupName(groupName, _directionType, calendar.GroupCourse);
                    DatabaseGroup groupCopy = group.Copy();
                    groupCopy.SetName(shortName);
                    groups.Add(groupCopy);
                }
            }

            return groups;
        }
    }

    public enum TableState
    {
        CollectGroupNames,
        WaitForCalendars,
        CollectCalendars,
        ForceEnd
    }

    public enum DirectionType
    {
        Bachelor,
        MasterDegree,
        Accelerated,
        SPO11,
        SPO9
    }
}
