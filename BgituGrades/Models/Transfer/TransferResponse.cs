namespace BgituGrades.Models.Transfer
{
    public class TransferResponse
    {
        public int Id { get; set; }
        public DateOnly OriginalDate { get; set; }
        public DateOnly NewDate { get; set; }
        public int DisciplineId { get; set; }
        public int GroupId { get; set; }
    }
}
