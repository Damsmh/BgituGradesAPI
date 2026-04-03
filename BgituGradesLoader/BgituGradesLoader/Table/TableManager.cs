using BgituGradesLoader.Compass;
using BgituGradesLoader.Compass.Objects;
using BgituGradesLoader.Database.Tables;
using BgituGradesLoader.Save;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace BgituGradesLoader.Table
{
    public class TableManager
    {
        private readonly SaveManager _saveManager;
        private readonly Dictionary<string, DatabaseGroup> _groupsData;

        private TableData? _nowTableData;
        private CompassConfig? _config;

        private const string FILE_PATH = "table.xlsx";

        public TableManager(SaveManager saveManager)
        {
            _saveManager = saveManager;
            _groupsData = [];
        }

        public DatabaseGroup? GetGroupData(string name)
        {
            name = GroupNameUtils.SimplyfyGroupName(name);
            if (_groupsData.TryGetValue(name, out DatabaseGroup? value))
                return value;

            name = GroupNameUtils.AddMasterToGroupName(name);
            if (_groupsData.TryGetValue(name, out DatabaseGroup? masterValue))
                return masterValue;

            return null;
        }

        public async Task GenerateGroupsData()
        {
            await LoadTable();
            await LoadCompassConfig();

            ScrapDataFromTable();
            DeleteTable();
        }

        private async Task LoadCompassConfig()
        {
            _config = await CompassManager.GetConfig();
        }

        private async Task LoadTable()
        {
            string? url = _saveManager.SaveData.TableLink.Data;
            if (url == null)
                return;

            HttpRequestMessage request = new(HttpMethod.Get, url);
            HttpClient client = new();
            HttpResponseMessage response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return;

            File.WriteAllBytes(FILE_PATH, await response.Content.ReadAsByteArrayAsync());
        }

        private static void DeleteTable()
        {
            if (!File.Exists(FILE_PATH))
                return;

            File.Delete(FILE_PATH);
        }

        private void ScrapDataFromTable()
        {
            using SpreadsheetDocument doc = SpreadsheetDocument.Open(FILE_PATH, false);
            WorkbookPart? workbookPart = doc.WorkbookPart;
            if (workbookPart == null || workbookPart.Workbook == null)
                return;

            SharedStringTablePart sstpart = workbookPart.GetPartsOfType<SharedStringTablePart>().First();
            SharedStringTable? sst = sstpart.SharedStringTable;
            Sheet sheet = workbookPart.Workbook.Descendants<Sheet>().First();
            if (sheet == null || sheet.Id == null || sst == null)
                return;

            WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
            if (worksheetPart.Worksheet == null)
                return;

            SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();
            List<string> mergedCells = TableUtils.GenerateMergedCellsList(worksheetPart.Worksheet);

            foreach (Row row in sheetData.Elements<Row>())
            {
                IEnumerable<Cell> cells = row.Elements<Cell>();
                if (!cells.Any())
                    continue;

                string firstCell = TableUtils.GetTextFromCell(cells.First(), sst);
                if (firstCell.StartsWith("Календарный учебный график"))
                {
                    CompileCurrentTableData();
                    _nowTableData = new(mergedCells, sst);
                }
                else
                {
                    _nowTableData?.AddRow(row);
                    if (_nowTableData?.NowState == TableState.ForceEnd)
                    {
                        CompileCurrentTableData();
                        _nowTableData = null;
                    }
                }
            }
        }

        private void CompileCurrentTableData()
        {
            if (_nowTableData == null || _config == null)
                return;

            List<DatabaseGroup> groups = _nowTableData.CompileTable(_config);
            foreach (DatabaseGroup group in groups)
            {
                if (group.Name == null)
                    continue;

                _groupsData[group.Name] = group;
            }
        }
    }
}
