namespace ETicketNewUI.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class AttendanceHeader
    {
        [Key]
        public int AttendanceID { get; set; }

        public int ProjectID { get; set; }
        [Required]
        public DateTime TrainingDate { get; set; }
        [Required]
        public DateTime StartDateTime { get; set; }
        [Required]
        public DateTime EndDateTime { get; set; }

        [Required, MaxLength(50)]
        public string Status { get; set; }

        public string QrCodeLink { get; set; }
        public string QrCodeImagePath { get; set; }

        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        public string Remark { get; set; }
        // Map coordinates
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }

    public class AttendanceDetail
    {
        [Key]
        public int DetailID { get; set; }

        public int AttendanceID { get; set; }   // no FK constraint

        [Required]
        public string ParticipantName { get; set; }
   
        public string PicturePath { get; set; }
        public DateTime PictureTakenDate { get; set; }
     
        public decimal Latitude { get; set; }
  
        public decimal Longitude { get; set; }

        public string DeviceName { get; set; }
        public string? Remark { get; set; }      // optional
        public string Position { get; set; }
    }

}
