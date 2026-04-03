using DocumentFormat.OpenXml.Spreadsheet;

namespace BgituGradesLoader.Table
{
    public static class TableUtils
    {
        public const string EMPTY_CELL_PLACEHOLDER = "E";
        public const char DIPLOM_DAY = 'д';
        public static readonly List<char> WORK_DAYS = ['=', '*', 'А'];

        public static readonly DateTime LEARNING_START_SUMMER = new(1, 9, 1);
        public static readonly DateTime LEARNING_START_WINTER = new(2, 1, 10);

        public static string GetTextFromCell(Cell cell, SharedStringTable stringTable)
        {
            if (cell == null)
                return EMPTY_CELL_PLACEHOLDER;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                if (stringTable != null && int.TryParse(cell.InnerText, out int index))
                {
                    if (index >= 0 && index < stringTable.Elements<SharedStringItem>().Count())
                    {
                        return stringTable.ElementAt(index).InnerText;
                    }
                }
                return cell.InnerText;
            }

            if (cell.CellValue != null)
                return cell.CellValue.Text;

            return EMPTY_CELL_PLACEHOLDER;
        }

        public static List<string> GenerateMergedCellsList(Worksheet worksheet)
        {
            MergeCells? mergeCells = worksheet.Elements<MergeCells>().FirstOrDefault();
            List<string> mergedCells = [];

            if (mergeCells == null)
                return mergedCells;

            foreach (MergeCell mergeCell in mergeCells.Elements<MergeCell>())
            {
                if (mergeCell.Reference == null || mergeCell.Reference.Value == null)
                    continue;

                string startCell = mergeCell.Reference.Value.Split(":")[0];
                mergedCells.Add(startCell);
            }

            return mergedCells;
        }
    }
}
