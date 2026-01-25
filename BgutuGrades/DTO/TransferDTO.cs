namespace BgutuGrades.DTO
{
    public class TransferDTO
    {
        public int Id { get; set; }
        public DateOnly OriginalDate { get; set; }
        public DateOnly NewDate { get; set; }
        public int DisciplineId { get; set; }
        public int GroupId { get; set; }
    }
}
