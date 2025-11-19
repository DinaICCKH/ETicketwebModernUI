namespace ETicketNewUI.Models
{
    public class AttendanceHeaderResult
    {
        public int AttendanceID { get; set; }
        public int ProjectID { get; set; }
        public string CompanyName { get; set; }

        public string TrainingDate { get; set; }     // string instead of DateTime
        public string StartDateTime { get; set; }    // string instead of DateTime
        public string EndDateTime { get; set; }      // string instead of DateTime

        public string Status { get; set; }
        public string QrCodeLink { get; set; }
        public string QrCodeImagePath { get; set; }

        public string CreatedBy { get; set; }
        public string UserName { get; set; }

        public string CreatedDate { get; set; }      // string instead of DateTime
        public string UpdatedBy { get; set; }
        public string Remark { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
}
