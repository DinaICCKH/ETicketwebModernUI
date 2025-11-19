namespace ETicketNewUI.Models
{
    public class BranchDto
    {
        public int RowNo { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = null!;
        public string ShortName { get; set; } = null!;
    }
}
