namespace BgituGrades.DTO
{
    public class TablePreview
    {
        public byte[] ExcelBytes { get; set; }
        public ReportPreviewDto Preview { get; set; }
    }
    public class ReportPreviewDto
    {
        public List<string> Headers { get; set; } = new List<string>();
        public List<PreviewRow> Rows { get; set; } = new List<PreviewRow>();
    }

    public class PreviewRow
    {
        public bool IsGroupHeader { get; set; }
        public List<string> Cells { get; set; } = new List<string>();
    }
}
