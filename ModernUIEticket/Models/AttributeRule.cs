using System;
using System.ComponentModel.DataAnnotations;

namespace ETicketNewUI.Models
{
    public class AttributeRule
    {
        public int AttributeId { get; set; } // PK
        public string Type { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string CreateBy { get; set; }
        public string UpdateBy { get; set; }
        public string Active { get; set; }
    }
}
