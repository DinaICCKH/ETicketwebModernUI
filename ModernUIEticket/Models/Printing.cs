namespace ETicketNewUI.Models
{
    public class AttendanceFormData
    {
        // Header fields
        public int AttendanceID { get; set; }
        public int ProjectID { get; set; }
        public string CompanyName { get; set; }
        public DateTime TrainingDate { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public string Status { get; set; }
        public string QrCodeLink { get; set; }
        public string QrCodeImagePath { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Remark { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string CreatedBy { get; set; }
        public string UserFullName { get; set; }
        public string UserName { get; set; }

        // Detail fields (just add multiple rows if needed)
        public int Visorder { get; set; }
        public string ParticipantName { get; set; }
        public string PicturePath { get; set; }
        public DateTime PictureTakenDate { get; set; }
        public decimal DetailLatitude { get; set; }
        public decimal DetailLongitude { get; set; }
        public string DeviceName { get; set; }
        public string Position { get; set; }
        public string DetailRemark { get; set; }
    }

}
