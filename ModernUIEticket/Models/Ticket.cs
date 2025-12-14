using System;
using System.Numerics;

namespace ETicketNewUI.Models
{
  


    public class FiltterProjectTracking
    {
        public string ID { get; set; }       // NVARCHAR(100)
        public string Name { get; set; }        // NVARCHAR(50)
    }


    public class ProjectTrackingReportParam
    {
        public string FromDate { get; set; }      // NVARCHAR(50)
        public string ToDate { get; set; }        // NVARCHAR(50)
        public string Project { get; set; }       // NVARCHAR(100)
        public string Status { get; set; }        // NVARCHAR(50)
        public string ProjectID { get; set; }        // NVARCHAR(50)
    }


    public class ProjectTrackingReport
    {
        public int ID { get; set; }
        public string ProjectCode { get; set; }
        public string RemarkHeader { get; set; }

        // Formatted Dates (string because SQL returns text)
        public string ProjectCreateDate { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }

        public string ProjectManager { get; set; }
        public string ProjectTeam { get; set; }
        public string ProjectStatus { get; set; }

        public int LineNum { get; set; }
        public string StageNo { get; set; }
        public string HandleBy { get; set; }
        public string StaffName { get; set; }

        // Stage formatted dates
        public string FromDate { get; set; }
        public string ToDate { get; set; }

        public string Remark { get; set; }
        public string RowStatus { get; set; }

        // Output Filters
        public string PFromDate { get; set; }
        public string PToDate { get; set; }
        public string PProject { get; set; }
        public string PStatus { get; set; }
    }


    public class ProjectTracking
    {
        public int ID { get; set; }
        public string Mode { get; set; } = "Add"; // Add / Update / Delete

        public string? ProjectCode { get; set; }
        public string? ProjectName { get; set; }
        public string? ProjectDescription { get; set; }

        public string? ProjectManager { get; set; }
        public string? ProjectTeam { get; set; }

        public DateTime? ProjectCreateDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string? ProjectStatus { get; set; }

        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        public DateTime? CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }

        public string? Status { get; set; }
        public string? Remark { get; set; }

        // Detail list (Stage / Tasks)
        public List<ProjectTrackingDetail> RowList { get; set; } = new();
    }

    public class ProjectTrackingDetail
    {
        public int? ID { get; set; }
        public int? RowNo { get; set; }

        public string? StageNo { get; set; }
        public string? HandleBy { get; set; }
        public string? SupportBy { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public string? Remark { get; set; }
        public string? RowStatus { get; set; }
    }



    public class ProjectTracking1
    {
        public int? ID { get; set; }
        public int? RowNo { get; set; }

        public string? StageNo { get; set; }
        public string? HandleBy { get; set; }
        public string? SupportBy { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public string? Remark { get; set; }
        public string? RowStatus { get; set; }
    }

    public class ProjectTrackingList
    {
        public int? RowNo { get; set; }
        public int? ID { get; set; }

        // Project Info
        public string? ProjectCode { get; set; }
        public string? ProjectName { get; set; }
        public string? ProjectDescription { get; set; }
        public string? ProjectManager { get; set; }
        public string? ProjectTeam { get; set; }
        public string? ProjectStatus { get; set; }

        // Dates (formatted as strings from SQL)
        public string? ProjectCreateDate { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }

        // Audit Info
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreateDate { get; set; }
        public string? UpdateDate { get; set; }
    }

    public class ApprovalHeaderRaw
    {
        public int? ID { get; set; }
        public int? CompanyId { get; set; }
        public string? Status { get; set; }
        public string? Remark { get; set; }
        public string? CreateBy { get; set; }
        public string? UpdateBy { get; set; }
    }

    public class ApprovalDetailRaw
    {
        public int? ID { get; set; }
        public int? LineNum { get; set; }
        public int? UserId { get; set; }
        public string? FullStaffName { get; set; }
        public int? SubcompanyId { get; set; }
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Remark { get; set; }
    }


    public class UserforAssignApproval
    {
        public int ID { get; set; }
        public string FullStaffName { get; set; }
    }

    public class ApprovalHeader
    {
        public string? Mode { get; set; }          // Add / Update / Delete
        public int? ID { get; set; }
        public int? CompanyId { get; set; }
        public string? Status { get; set; }
        public string? Remark { get; set; }
        public string? CreateBy { get; set; }
        public string? UpdateBy { get; set; }

        // Child list for detail records
        public List<ApprovalDetail>? RowList { get; set; }
    }

    public class ApprovalDetail
    {
        public int? ID { get; set; }
        public int? LineNum { get; set; }
        public int? UserId { get; set; }
        public int? SubcompanyId { get; set; }
        public string? Status { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? Remark { get; set; }
    }

    public class ApprovalModel
    {
        public int? ID { get; set; }
        public int? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string? Status { get; set; }
        public string? Remark { get; set; }
        public string? CreateBy { get; set; }
        public string? UpdateBy { get; set; }
        public string? CreateDate { get; set; }
        public string? UpdateDate { get; set; }
        public string? LinkTran { get; set; }
    }


    public class Staff
    {
        public string? Mode { get; set; }                // Add, Update, Delete
        public string? SecretCode { get; set; }         // Authorization code
        public int? StaffId { get; set; }               // Primary key
        public string? FirstName { get; set; }          // Max 50 chars
        public string? LastName { get; set; }           // Max 50 chars
        public string? Gender { get; set; }             // 'M'/'F'
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePicture { get; set; }     // File path or URL
        public string? WorkExperience { get; set; }     // JSON or free text
        public string? Position { get; set; }           // Job title
        public DateTime? HireDate { get; set; }         // Stored as DATETIME
        public DateTime? CreatedDate { get; set; }      // Stored as DATETIME
        public DateTime? UpdatedDate { get; set; }      // Stored as DATETIME
        public string? CreateBy { get; set; }           // User who created
        public string? UpdateBy { get; set; }           // User who updated
        public string? Active { get; set; }             // 'Y'/'N'
        public int? UserID { get; set; }                // Related user ID
        public string? Alert { get; set; }              // Alert or notification flag
    }


    public class UserStaff
    {
        public int? UserId { get; set; }
        public string? UserName { get; set; }
    }

    public class StaffResult
    {
        public string? SecretCode { get; set; }
        public int? StaffId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Gender { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePicture { get; set; }
        public string? WorkExperience { get; set; }
        public string? Position { get; set; }        // From T2.Description
        public string? HireDate { get; set; }        // Stored as string like "01-JAN-2025"
        public string? CreatedDate { get; set; }     // Stored as string like "01-JAN-2025"
        public string? UpdatedDate { get; set; }     // Stored as string like "01-JAN-2025"
        public string? CreateBy { get; set; }
        public string? UpdateBy { get; set; }
        public string? Active { get; set; }
        public int? UserID { get; set; }
        public string? UserName { get; set; }
        public string? LinkTran { get; set; }        // 'Y'/'N'
        public string? Alert { get; set; }
        public string? PositionID { get; set; }         // Original numeric position ID
    }

    public class ProjectAssignHeaderByID
    {
        public int? ID { get; set; }
        public int? ClientID { get; set; }
        public string? CompanyName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string? CreateBy { get; set; }
        public string? UpdateBy { get; set; }
        public string? Status { get; set; }
        public string? Remark { get; set; }
    }

    public class ProjectAssignRowByID
    {
        public int? ID { get; set; }
        public int? RowNo { get; set; }
        public int? StaffID { get; set; }
        public string? StaffName { get; set; }
        public string? Remark { get; set; }
        public string? Role { get; set; }
        public string? RoleName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? ClientID { get; set; }
        public string? ClientName { get; set; }
        public string? Status { get; set; }
    }

    public class ProjectAvailable
    {
        public int? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string? ShortName { get; set; }
    }

    public class ProjectAssignHeader
    {
        public int? ID { get; set; }
        public string? SecretCode { get; set; }
        public string? Mode { get; set; } // Add / Update
        public int? ClientID { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? CreateBy { get; set; }
        public string? UpdateBy { get; set; }
        public string? Status { get; set; }
        public string? Remark { get; set; }

        // 🔹 Child rows (staff assignments)
        public List<ProjectAssignRow> RowList { get; set; } = new();
    }

    public class ProjectAssignRow
    {
        public int? ID { get; set; }
        public int? RowNo { get; set; }
        public int? StaffID { get; set; }
        public string? Remark { get; set; }
        public string? Role { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? ClientID { get; set; }
        public string? Status { get; set; }
    }

    public class ProjectAssignResult
    {
        public int? RowNo { get; set; }
        public int? ID { get; set; }
        public int? ClientID { get; set; }
        public string CompanyName { get; set; }
        public string Status { get; set; }
        public string Remark { get; set; }
        public string CreatedDate { get; set; }   // Stored procedure formats as dd-MMM-yyyy
        public string UpdatedDate { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string CreateBy { get; set; }
        public string UpdateBy { get; set; }
    }


    public class Client
    {
        public string? Mode { get; set; }
        public string? SecretCode { get; set; }
        public int? CompanyId { get; set; }
        public string? ContractNo { get; set; }
        public string? CompanyName { get; set; }
        public string? Address1 { get; set; }
        public string? ContactNumber { get; set; }
        public string? Email { get; set; }
        public DateTime? JoinDate { get; set; }
        public string? Website { get; set; }
        public string? KeyCode { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? CreateBy { get; set; }
        public string? UpdateBy { get; set; }
        public string? Active { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? TerminationDate { get; set; }
        public string? CompanyType { get; set; }
        public string? MainCompany { get; set; }
        public string? ClientGroup { get; set; }
        public string? ShortName { get; set; }
    }

    public class ClientDto
    {
        public int? CompanyId { get; set; }
        public string? ContractNo { get; set; }
        public string? CompanyName { get; set; }
        public string? Address1 { get; set; }
        public string? ContactNumber { get; set; }
        public string? Email { get; set; }

        public string? JoinDate { get; set; }
        public string? Website { get; set; }
        public string? KeyCode { get; set; }
        public string? CreatedDate { get; set; }
        public string? UpdatedDate { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public string? TerminationDate { get; set; }

        public string? CreateBy { get; set; }
        public string? UpdateBy { get; set; }

        public string? Active { get; set; }
        public string? CompanyType { get; set; }
        public string? MainCompany { get; set; }

        public string? ClientGroup { get; set; }
        public string? ClientGroupDes { get; set; }

        // LinkTran is 'Y' or 'N' in SQL, nullable
        public string? LinkTran { get; set; }
        public int? MainCompanyID { get; set; }
        public string? ShortName { get; set; }
    }

    public class AdvertisementListDto
    {
        public int? ID { get; set; }
        public string? Picture { get; set; }
        public string? TextDes { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DateTime? CreateDate { get; set; }
        public int DayAgo { get; set; }
    }

    public class Advertisement
    {
        public string Mode { get; set; }          // Add / Update / Delete
        public int? ID { get; set; }              // Required for Update/Delete
        public string Picture { get; set; }
        public string ShowPictureStatus { get; set; } // "Y" or "N"
        public string TextDes { get; set; }
        public string ShowTextDesStatus { get; set; } // "Y" or "N"
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Status { get; set; }        // "A" / "I"
        public string? CreateBy { get; set; }
        public string? UpdateBy { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
    }

    public class AdvertisementDto
    {
        public int? ID { get; set; }
        public string? Picture { get; set; }
        public string? ShowPictureStatus { get; set; }
        public string? TextDes { get; set; }
        public string? ShowTextDesStatus { get; set; }
        public string? FromDate { get; set; }  // formatted as DD-MMM-YYYY
        public string? ToDate { get; set; }    // formatted as DD-MMM-YYYY
        public string? Status { get; set; }
        public string? CreateBy { get; set; }
        public string? UpdateBy { get; set; }
        public string? CreateDate { get; set; } // formatted as DD-MMM-YYYY
        public string? UpdateDate { get; set; } // formatted as DD-MMM-YYYY
    }

    public class Ticket
    {
        public int TicketId { get; set; }
        public string RefID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public DateTime? DeadlineDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public string CreateBy { get; set; }
        public string? UpdateBy { get; set; }
        public string? Attachment { get; set; }
        public string? ProblemType { get; set; }
        public string? SubProblemType { get; set; }
        public string? Module { get; set; }
        public string? Branch { get; set; }
        public DateTime? LastFollowup { get; set; }
        public string? RejectReason { get; set; }
        public DateTime? AcceptDate { get; set; }
        public string? Project { get; set; }
    }

    public class ReportTrackingParameterResult
    {
        public string Type { get; set; }
        public string ID { get; set; }
        public string Name { get; set; }
    }

    public class TicketTrackingDto
    {
        public int? TicketId { get; set; }
        public string? RefID { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? CreatedDate { get; set; } // keep as string if you want formatted date
        public string? UpdatedDate { get; set; } // keep as string if you want formatted date
        public string? DeadlineDate { get; set; }
        public string? CloseDate { get; set; }
        public int? OverDate { get; set; }
        public string? CreateBy { get; set; }
        public string? UpdateBy { get; set; }
        public string? Attachment { get; set; }
        public string? ProblemType { get; set; }
        public string? Module { get; set; }
        public string? Branch { get; set; }
        public string? SubProblemType { get; set; }
        public string? Followup { get; set; }
        public string? CancelStatus { get; set; }
        public string? CreateByID { get; set; }
        public string? CreateByName { get; set; }
        public string? PriorityID { get; set; }
        public string? ProblemTypeID { get; set; }
        public string? ModuleID { get; set; }
        public string? BranchID { get; set; }
        public string? SubProblemTypeID { get; set; }
        public string? Project { get; set; }
    }

    public class TicketAssignListDto
    {
        public int? TicketId { get; set; }
        public string? RefID { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }

        public string? Status { get; set; }
        public string? Priority { get; set; }

        public string? CreatedDate { get; set; }
        public string? UpdatedDate { get; set; }
        public string? DeadlineDate { get; set; }
        public string? CloseDate { get; set; }

        public int? OverDate { get; set; }

        public string? UpdateBy { get; set; }
        public string? Attachment { get; set; }

        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Telephone { get; set; }

        public string? CompanyName { get; set; }

        public string? ProblemType { get; set; }
        public string? Module { get; set; }
        public string? SubProblemType { get; set; }

        public string? Branch { get; set; }

        public string? AssignByName { get; set; }
        public string? HandleBy { get; set; }
        public string? HandleByDes { get; set; }

        public string? Remark { get; set; }

        public string? ProjectManager { get; set; }
    }

    // Main assignment
    public class TicketAssignHeader
    {
        public string? Mode { get; set; }
        public string? SecretCode { get; set; }
        public int? Id { get; set; }
        public string? TicketRef { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? CreateBy { get; set; }
        public string? UpdateBy { get; set; }
        public string? Status { get; set; }
        public string? Progress { get; set; }
        public string? Remark { get; set; }

        // Optional: include the row list as a collection
        public List<TicketAssignRow>? RowList { get; set; }
    }

    // Staff assignment / Row list
    public class TicketAssignRow
    {
        public int? Id { get; set; }
        public int? LineNum { get; set; }
        public int? StaffId { get; set; }
        public int? Supporter { get; set; }
        public int? TransferTo { get; set; }
        public DateTime? AssignDate { get; set; }
        public DateTime? DeadlineDate { get; set; }
        public DateTime? TransferDate { get; set; }
        public string? Status { get; set; }
        public string? Remark { get; set; }
        public string? ProgessStatus { get; set; }
        public string? IssueLevel { get; set; }
    }


    public class StaffListAssign
    {
        public int? ID { get; set; }
        public int? CompanyID { get; set; }
        public string? FromDate { get; set; }  // e.g., "1-SEP-2025"
        public string? ToDate { get; set; }    // e.g., "5-SEP-2025"
        public int? StaffID { get; set; }
        public string? FromDateRow { get; set; } // e.g., "1-SEP-2025"
        public string? ToDateRow { get; set; }   // e.g., "5-SEP-2025"
        public string? Role { get; set; }
        public int? BranchID { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PositionID { get; set; }
        public string? PositionName { get; set; }
        public string? FullStaffName { get; set; } // e.g., "John Doe (Manager)"
    }


    public class TicketAssignHeaderV2
    {
        public int? Id { get; set; }               // nvarchar
        public string? TicketRef { get; set; }        // nvarchar
        public DateTime? CreatedDate { get; set; }    // nullable datetime
        public DateTime? UpdatedDate { get; set; }    // nullable datetime
        public string? CreateBy { get; set; }         // nvarchar
        public string? UpdateBy { get; set; }         // nvarchar
        public string? Status { get; set; }           // nvarchar
        public string? Progress { get; set; }         // nvarchar
        public string? Remark { get; set; }           // nvarchar
    }

    public class TicketAssignRowV2
    {
        public int? Id { get; set; }               // nvarchar(255)
        public int? LineNum { get; set; }             // nullable int
        public int? StaffId { get; set; }
        public string? StaffName { get; set; }    // nullable int
        public int? Supporter { get; set; }
        public string? SupporterName { get; set; } // nullable int
        public int? TransferTo { get; set; }
        public string? TransferName { get; set; }    // nullable int
        public DateTime? AssignDate { get; set; }     // nullable datetime
        public DateTime? DeadlineDate { get; set; }   // nullable datetime
        public DateTime? TransferDate { get; set; }   // nullable datetime
        public string? Status { get; set; }           // nullable string
        public string? Remark { get; set; }           // nullable string
        public string? ProgessStatus { get; set; }    // nullable string
        public string? IssueLevel { get; set; }       // nullable string
    }

    public class TicketChatHistoryModerator
    {
        public string? Type { get; set; }             // Conversation / Followup / Assignment / Rejection / Confirmation
        public string? RefID { get; set; }           // Ticket reference or ID
        public string? CreateBy { get; set; }        // UserId who created the record
        public string? UserFullName { get; set; }    // Full name of the user
        public string? Description { get; set; }     // Remark / FeedbackText / Assignment info
        public string? CreatedDateTime { get; set; } // Formatted date string from SQL
        public string? UserType { get; set; }        // Type of user (Moderator, Customer, Staff, etc.)
    }

    public class Feedback
    {
        public int? FeedbackId { get; set; }
        public string SecretCode { get; set; }

        public string TicketRef { get; set; }
        public int? Rating { get; set; }
        public string FeedbackText { get; set; }
        public string Type { get; set; }

        public string CreateBy { get; set; }
        public string UpdateBy { get; set; }

        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class MyTicketDto
    {
        public int? TicketId { get; set; }
        public string? RefID { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? CreatedDate { get; set; }
        public string? UpdatedDate { get; set; }
        public string? DeadlineDate { get; set; }
        public string? CloseDate { get; set; }
        public int? OverDate { get; set; }
        public string? UpdateBy { get; set; }
        public string? Attachment { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? Telephone { get; set; }
        public string? CompanyName { get; set; }
        public string? ProblemType { get; set; }
        public string? Module { get; set; }
        public string? SubProblemType { get; set; }
        public int? AssignID { get; set; }
        public string? AssignDate { get; set; }
        public string? DeadlineDateAssign { get; set; }
        public string? Remark { get; set; }
        public string? AssignBy { get; set; }
        public int? AssignIDLine { get; set; }
        public string? ProgessStatus { get; set; }
        public string? AssignmentDetails { get; set; }
    }
    public class Ticket1
    {
        public string SecretCode { get; set; }
        public string Mode { get; set; }
        public int AssignID { get; set; }
        public int AssignIDLine { get; set; }
        public string RefID { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreateBy { get; set; }
        public string Status { get; set; }
        public string Remark { get; set; }
        public string Attachment { get; set; }
        public string Hide { get; set; }
        public string Comment { get; set; } // <--- Add this
    }

    public class TicketCompleteFeedBack
    {
        public int? RowNo { get; set; }
        public string? RefID { get; set; }
        public string? Title { get; set; }
        public int? HandleByID { get; set; }
        public string? HandleBy { get; set; }
        public int? AssignID { get; set; }
        public int? AssignIDLine { get; set; }
        public string? DevStatus { get; set; }
        public string? DevRemark { get; set; }
        public string? DevAttachment { get; set; }
        public string? DevCreateDate { get; set; }   // formatted datetime string
    }

    // Represents #TempTicket (Header)
    public class TicketClientHeader
    {
        public string SecretCode { get; set; }
        public string Mode { get; set; }
        public string RefID { get; set; }

        // Row list (detail)
        public List<TicketClientRow> RowList { get; set; } = new List<TicketClientRow>();
    }

    // Represents #TempTicket1 (Row / Detail)
    public class TicketClientRow
    {
        public int AssignID { get; set; }
        public int AssignIDLine { get; set; }
        public string RefID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreateBy { get; set; }
        public string Status { get; set; }
        public string Remark { get; set; }
        public string Attachment { get; set; }
        public string Hide { get; set; } // "N" or "Y"
    }

    public class AlertDto
    {
        public int? ID { get; set; }
        public string? Type { get; set; }
        public string? UserID { get; set; }
        public string? Description { get; set; }
        public string? Link { get; set; }
        public string? CreatedDateTime { get; set; } // formatted as "dd-MMM-yyyy hh:mm AM/PM"
    }

    public class ClientTicketSummaryDto
    {
        // Monthly Ticket Counts
        public int Jan { get; set; }
        public int Feb { get; set; }
        public int Mar { get; set; }
        public int Apr { get; set; }
        public int May { get; set; }
        public int Jun { get; set; }
        public int Jul { get; set; }
        public int Aug { get; set; }
        public int Sep { get; set; }
        public int Oct { get; set; }
        public int Nov { get; set; }
        public int Dec { get; set; }

        // Ticket Status Counts
        public int TotalTicket { get; set; }
        public int Request { get; set; }
        public int Pending { get; set; }
        public int Progress { get; set; }
        public int Checking { get; set; }
        public int Feedback { get; set; }
        public int Done { get; set; }
        public int Cancel { get; set; }
        public int Reject { get; set; }
        public int Draf { get; set; }
    }

    public class ClientTicketReportDto
    {
        public int? TicketId { get; set; }
        public string? RefID { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? CreatedDate { get; set; }
        public string? UpdatedDate { get; set; }
        public string? DeadlineDate { get; set; }
        public string? CloseDate { get; set; }
        public int? OverDate { get; set; }
        public string? CreateBy { get; set; }
        public string? UpdateBy { get; set; }
        public string? Attachment { get; set; }
        public string? ProblemType { get; set; }
        public string? Module { get; set; }
        public string? Branch { get; set; }
        public string? SubProblemType { get; set; }
        public string? Followup { get; set; }
        public string? CancelStatus { get; set; }
        public string? CreateByID { get; set; }
        public string? CreateByName { get; set; }
        public string? PFromDate { get; set; }
        public string? PToDate { get; set; }
        public string? PUser { get; set; }
        public string? PCreateBy { get; set; }
        public string? PProblemType { get; set; }
        public string? PModule { get; set; }
        public string? PStatus { get; set; }
        public string? PEntryPrimary { get; set; }
        public string? PUserLoginName { get; set; }
        public string? AssignBy { get; set; }
        public string? AssignTo { get; set; }
    }

    public class TaskTrackingByStaffSummaryDto
    {
        public int AssignTo { get; set; }
        public string AssignToName { get; set; }
        public string Position { get; set; }

        public int Pending { get; set; }
        public int Progress { get; set; }
        public int Checking { get; set; }
        public int Feedback { get; set; }
        public int Done { get; set; }
        public int Total { get; set; }
    }

    public class BranchMainDto
    {
        public int RowNo { get; set; }           // Row number
        public int CompanyId { get; set; }       // Company ID
        public string CompanyName { get; set; }  // Full company name
        public string ShortName { get; set; }    // Short name of company
    }

    public class TaskTrackingByStaffDto
    {
        public string Type { get; set; }
        public string AssigBy { get; set; }
        public string AssigByName { get; set; }
        public string AssignTo { get; set; }
        public string AssignToName { get; set; }
        public string RequestBy { get; set; }
        public string RequestByName { get; set; }
        public DateTime? RequestDate { get; set; }
        public DateTime? AssignDate { get; set; }
        public DateTime? DeadlineDate { get; set; }
        public string Remark { get; set; }
        public string ProgessStatus { get; set; }
        public string Status { get; set; }
        public string IssueLevel { get; set; }
        public string BranchID { get; set; }
        public string BranchName { get; set; }
        public string MainCompany { get; set; }
        public string MainCompanyName { get; set; }
        public string Title { get; set; }
        public string Module { get; set; }
        public string Problem { get; set; }
        public string SubProblem { get; set; }
        public string Priority { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? Over { get; set; }
        public DateTime? UpdateDate { get; set; }
        public int? OverOfDone { get; set; }
        public int? Id { get; set; }
        public int? LineNum { get; set; }
        public string TicketRef { get; set; }
        public DateTime? FinalUpdateDoneDate { get; set; }
    }

    public class AssignTicketSummaryDto
    {
        // Monthly ticket counts
        public int Jan { get; set; }
        public int Feb { get; set; }
        public int Mar { get; set; }
        public int Apr { get; set; }
        public int May { get; set; }
        public int Jun { get; set; }
        public int Jul { get; set; }
        public int Aug { get; set; }
        public int Sep { get; set; }
        public int Oct { get; set; }
        public int Nov { get; set; }
        public int Dec { get; set; }

        // Status counts
        public int TotalTicket { get; set; }
        public int Progress { get; set; }
        public int Done { get; set; }
        public int FeedBack { get; set; }
        public int Checking { get; set; }
        public int Reject { get; set; }
    }

    public class AssignTicketBySupporterSummaryDto
    {
        public int RowNo { get; set; }
        public string Type { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AssignBy { get; set; } = string.Empty;

        public int START { get; set; }
        public int PROGRESS { get; set; }
        public int CHECKING { get; set; }
        public int FEEDBACK { get; set; }
        public int DONE { get; set; }
        public int NON { get; set; }
        public int Total { get; set; }
    }

    public class AssignTicketByCompanySummaryDto
    {
        public int TotalTicket { get; set; }
        public string CompanyName { get; set; } = string.Empty;
    }


    public class RecurringTask
    {
        public int TaskID { get; set; }

        public int ProjectID { get; set; }

        public int AssignToUserID { get; set; }

        public int? HandleByUserID { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public string Frequency { get; set; } = string.Empty;

        public string ExecutionTime { get; set; } = string.Empty;

        public string? ExecutionDayForWeekly { get; set; }

        public string? ExecutionDayForMonthly { get; set; }

        public string? ExecutationMonthForYearly { get; set; }

        public int? Deadline { get; set; }

        public string Status { get; set; } = "Active";

        public string? Remark { get; set; }

        public int CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class RecurringTaskResult
    {
        public int? TaskID { get; set; }
        public int? ProjectID { get; set; }
        public string? CompanyName { get; set; }

        public int? AssignToUserID { get; set; }
        public string? AssignToUserName { get; set; }

        public int? HandleByUserID { get; set; }
        public string? HandleByUserName { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public string? Frequency { get; set; }

        // Always displayed regardless of frequency
        public string? ExecutionTime { get; set; }

        public int? CreatedBy { get; set; }
        public string? CreateByName { get; set; }

        // For weekly execution
        public string? ExecutionDayForWeekly { get; set; }

        // For monthly execution
        public string? ExecutionDayForMonthly { get; set; }

        // For yearly execution
        public string? ExecutationMonthForYearly { get; set; }

        public int? Deadline { get; set; }

        public string? Status { get; set; }
        public string? Remark { get; set; }

        public DateTime CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class user
    {
        public string Mode { get; set; }            // Add, Update, Delete
        public string SecretCode { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string ConfirmPassword { get; set; }
        public string UStatus { get; set; }
        public string ULock { get; set; }
        public string CompanyId { get; set; }
        public string CreateDate { get; set; }
        public string LastLoginDate { get; set; }
        public string UpdateDate { get; set; }
        public string Telephone { get; set; }
        public string CreateBy { get; set; }
        public string UpdateBy { get; set; }
        public string Type { get; set; }
        public string FullAuthorization { get; set; } // "Y" or "N"
        public string ResetPassword { get; set; }     // "Y" or "N"
        public string UserFullName { get; set; }

        // Child table: branch assignment
        public List<user1> SelectedBranches { get; set; } = new List<user1>();
    }

    public class user1
    {
        public string LineNum { get; set; }
        public string UserId { get; set; }
        public string CompanyId { get; set; }
        public string Name { get; set; }
        public string Assign { get; set; } // "Y" or "N"
    }


    public class userList
    {
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? CreateDate { get; set; }  // formatted string: "16-SEP-2025"
        public string? LastLoginDate { get; set; } // formatted string
        public string? ULock { get; set; }
        public int? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string? Telephone { get; set; }
        public string? UStatus { get; set; }
        public string? FullAuthorization { get; set; }
        public string? Type { get; set; }
        public string? LinkTran { get; set; }  // 'Y' or 'N'
        public string? UserFullName { get; set; }
    }

    public class CompanyBranch
    {
        public int RowNo { get; set; }           // Row number generated by ROW_NUMBER()
        public int CompanyId { get; set; }    // Client ID
        public string CompanyName { get; set; }  // Client name
        public string ShortName { get; set; }    // Short name
    }

    public class UserBranch
    {
        public int? UserId { get; set; }
        public int? LineNum { get; set; }
        public string? Name { get; set; } = string.Empty;
        public string? Assign { get; set; } = "N"; // default N
        public int? CompanyId { get; set; }
    }


}
