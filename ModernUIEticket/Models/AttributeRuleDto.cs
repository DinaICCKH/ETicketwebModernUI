namespace ETicketNewUI.Models
{
    public class AttributeRuleDto
    {
        public int? RowNo { get; set; }           // nullable int
        public int? AttributeId { get; set; }     // nullable int
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? CreatedDate { get; set; }
        public string? UpdatedDate { get; set; }
        public string? CreateBy { get; set; }
        public string? UpdateBy { get; set; }
        public string? Status { get; set; }
        public string? AType { get; set; }
        public string? LinkTran { get; set; }
    }
}
