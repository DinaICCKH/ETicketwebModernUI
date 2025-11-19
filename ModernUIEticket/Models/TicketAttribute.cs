namespace ETicketNewUI.Models
{
    public class TicketAttribute
    {
        public int? RowNo { get; set; }          // Row number from SP
        public int? AttributeId { get; set; }    // Attribute unique identifier
        public string? Code { get; set; }        // Code of the attribute
        public string? Description { get; set; } // Description of the attribute
        public string? CreatedDate { get; set; } // Formatted created date from SP
        public string? UpdatedDate { get; set; } // Formatted updated date from SP
        public string? CreateBy { get; set; }    // Created by user
        public string? UpdateBy { get; set; }    // Updated by user
        public string? Status { get; set; }      // Active status 'A' or other
        public string? AType { get; set; }       // Type: Module, Priority, ProblemType, Type
        public string? LinkTran { get; set; }    // 'Y' if linked to ticket/client, 'N' otherwise
        public string? SecretCode { get; set; }  // Empty string from SP, can be null
    }
}
