using BgituGradesLoader.Compass.Objects;
using BgituGradesLoader.Database.Tables;
using DocumentFormat.OpenXml.Spreadsheet;

namespace BgituGradesLoader.Table
{
    public class TableCalendar
    {
        private readonly int _groupCourse;
        private readonly List<Row> _rows;
        private readonly List<char> _calendar;

        private readonly List<string> _mergedCells;
        private readonly SharedStringTable _stringTable;

        public int GroupCourse => _groupCourse;
        public int Rows => _rows.Count;

        public TableCalendar(int groupCourse, List<string> mergedCells, SharedStringTable stringTable)
        {
            _groupCourse = groupCourse;
            _rows = [];
            _calendar = [];

            _mergedCells = mergedCells;
            _stringTable = stringTable;
        }

        public void AddRow(Row row)
        {
            _rows.Add(row);
        }

        public bool HaveDiplom()
        {
            return _calendar.Contains(TableUtils.DIPLOM_DAY);
        }

        public void CompileCalendar()
        {
            List<List<Cell>> rawCalendar = [];

            foreach (Row row in _rows)
            {
                List<Cell> cells = [.. row.Elements<Cell>()];
                rawCalendar.Add(cells);
            }

            if (rawCalendar.Count == 0)
                return;

            rawCalendar.Add(rawCalendar[^1]);
            for (int j = 0; j < rawCalendar[0].Count; j++)
            {
                Cell cell = rawCalendar[0][j];
                if (cell.CellReference == null || cell.CellReference.Value == null)
                    continue;

                if (_mergedCells.Contains(cell.CellReference.Value))
                {
                    for (int i = 0; i < rawCalendar.Count; i++)
                        rawCalendar[i][j] = cell;
                }
            }

            for (int j = 1; j < rawCalendar[0].Count - 1; j++)
            {
                for (int i = 0; i < rawCalendar.Count; i++)
                {
                    string cellText = TableUtils.GetTextFromCell(rawCalendar[i][j], _stringTable);
                    _calendar.Add(cellText[0]);
                }
            }
        }

        public DatabaseGroup GetGroupData(CompassConfig config)
        {
            DateTime aproximateWeek = config.AproximateWeek;
            DateTime summerStart = TableUtils.LEARNING_START_SUMMER;
            DateTime winterStart = TableUtils.LEARNING_START_WINTER;

            int aproximateWeekNum = config.AproximateWeekNumber;
            int daysBetweenSemesters = (winterStart - summerStart).Days;
            int startDay = 0;
            int year = aproximateWeek.Year;
            if (aproximateWeek.Month < 7)
            {
                startDay = daysBetweenSemesters;
                year--;
            }

            char emptyCell = Convert.ToChar(TableUtils.EMPTY_CELL_PLACEHOLDER);
            DateTime globalStart = new(year, summerStart.Month, summerStart.Day);
            DateTime learnStart = DateTime.MinValue;
            DateTime learnEnd = DateTime.MinValue;
            int learnStartNumber = -1;

            for (int days = startDay; days < _calendar.Count; days++)
            {
                bool isWorkDay = _calendar[days] == emptyCell || TableUtils.WORK_DAYS.Contains(_calendar[days]);

                if (learnStart == DateTime.MinValue)
                {
                    if (!isWorkDay)
                        continue;
                    learnStart = globalStart.AddDays(days);
                    int approximateDays = (learnStart - aproximateWeek).Days % 14;
                    if (approximateDays < 7)
                    {
                        learnStartNumber = aproximateWeekNum;
                    }
                    else
                    {
                        if (aproximateWeekNum == 2)
                            learnStartNumber = 1;
                        else
                            learnStartNumber = 2;
                    }
                }
                else if (learnEnd == DateTime.MinValue)
                {
                    if (isWorkDay)
                        continue;
                    learnEnd = globalStart.AddDays(days);
                    break;
                }
            }

            string learnStartStr = learnStart.ToString("yyyy-MM-dd");
            string learnEndStr = learnEnd.ToString("yyyy-MM-dd");
            return new(learnStartStr, learnEndStr, learnStartNumber);
        }
    }
}
