using Microsoft.EntityFrameworkCore;

namespace ETicketNewUI.Models
{
    public class TicketDbContext : DbContext
    {
        public TicketDbContext(DbContextOptions<TicketDbContext> options) :base(options) { }

        public DbSet<Ticket> Tickets { get; set; }

        public DbSet<AttributeRuleDto> AttributeList { get; set; }
        public DbSet<AttributeRule> AttributeRules { get; set; }
        public DbSet<TicketAttribute> Attributes { get; set; }
        public DbSet<LoginResultDto> LoginResults { get; set; }
        public DbSet<BranchDto> Branches { get; set; }
        public DbSet<SpResult> SpResult { get; set; }

        // ===================== Attendance Module =====================
        public DbSet<AttendanceHeader> AttendanceHeaders { get; set; }
        public DbSet<AttendanceDetail> AttendanceDetails { get; set; }

        // Result model for stored procedure (no PK)
        public DbSet<AttendanceHeaderResult> AttendanceHeaderResults { get; set; }
        public DbSet<ReportTrackingParameterResult> ReportTrackingResults { get; set; }
        public DbSet<TicketTrackingDto> ICC_GET_TICKET_TrackingListV2Results { get; set; }
        public DbSet<TicketAssignListDto> ICC_GET_TICKET_AssignListV2Results { get; set; }

        public DbSet<TicketAssignHeader> AsignTicketHeaders { get; set; }
        public DbSet<TicketAssignRow> AsignTicketRows { get; set; }
        public DbSet<StaffListAssign> StaffListAssignResult { get; set; }

        public DbSet<TicketAssignHeaderV2> TicketAssignHeaderV2Result { get; set; }
        public DbSet<TicketAssignRowV2> TicketAssignRowV2Result { get; set; }
        public DbSet<TicketChatHistoryModerator> TicketChatHistoryModeratorResult { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<MyTicketDto> MyTicketDtoResult { get; set; }
        public DbSet<Ticket1> Ticket1s { get; set; }
        public DbSet<TicketCompleteFeedBack> TicketCompleteFeedBackResult { get; set; }
        // New: Ticket Client Models (for SP JSON)
        public DbSet<TicketClientHeader> TicketClientHeaders { get; set; }
        public DbSet<TicketClientRow> TicketClientRows { get; set; }
        public DbSet<AlertDto> AlertDtos { get; set; }
        public DbSet<ClientTicketSummaryDto> ClientTicketSummaryDtos { get; set; }
        public DbSet<ClientTicketSummaryDto> ClientTicketReportDto { get; set; }
        public DbSet<TaskTrackingByStaffSummaryDto> TaskTrackingByStaffSummaryDtos { get; set; }
        public DbSet<BranchMainDto> BranchMainDtos { get; set; }
        public DbSet<TaskTrackingByStaffDto> TaskTrackingByStaffDtos { get; set; }
        public DbSet<AssignTicketSummaryDto> AssignTicketSummaryDtos { get; set; }
        public DbSet<AssignTicketBySupporterSummaryDto> AssignTicketBySupporterSummaryDtos { get; set; }
        public DbSet<AssignTicketByCompanySummaryDto> AssignTicketByCompanySummaryDtos { get; set; }
        public DbSet<RecurringTask> RecurringTasks { get; set; }
        public DbSet<RecurringTaskResult> RecurringTaskResults { get; set; }
        public DbSet<user> Users { get; set; }
        public DbSet<user1> UserBranches { get; set; }
        public DbSet<userList> userListResults { get; set; }
        public DbSet<CompanyBranch> CompanyBranchResult { get; set; }
        public DbSet<UserBranch> UserBranchResult { get; set; }
        public DbSet<AdvertisementDto> AdvertisementDtos { get; set; }
        public DbSet<Advertisement> Advertisements { get; set; }
        public DbSet<AdvertisementListDto> AdvertisementListDtos { get; set; }
        public DbSet<ClientDto> ClientDtos { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<ProjectAssignResult> ProjectAssignResults { get; set; }
        public DbSet<ProjectAssignHeader> ProjectAssigns { get; set; }              // Real table: ProjectAssign
        public DbSet<ProjectAssignRow> ProjectAssignDetails { get; set; }
        public DbSet<ProjectAvailable> ProjectAvailables { get; set; }
        public DbSet<ProjectAssignHeaderByID> ProjectAssignHeaderByIDs { get; set; }
        public DbSet<ProjectAssignRowByID> ProjectAssignRowByIDs { get; set; }
        public DbSet<StaffResult> StaffResults { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<UserStaff> UserStaffs { get; set; }
        public DbSet<ApprovalModel> Approvallist { get; set; }
        public DbSet<ApprovalHeader> ApprovalHeaders { get; set; }              // Real table: ProjectAssign
        public DbSet<ApprovalDetail> approvalDetails { get; set; }
        public DbSet<UserforAssignApproval> UserforAssignApprovals { get; set; }
        public DbSet<ApprovalHeaderRaw> ApprovalHeaderRaws { get; set; }
        public DbSet<ApprovalDetailRaw> ApprovalDetailRaws { get; set; }
        public DbSet<ProjectTrackingList> ProjectTrackingLists { get; set; }
        public DbSet<ProjectTracking> ProjectTrackings { get; set; }
        public DbSet<ProjectTrackingDetail> ProjectTrackingDetails { get; set; }
        public DbSet<ProjectTracking1> ProjectTracking1s { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<ProjectTracking1>().HasNoKey();
            modelBuilder.Entity<ProjectTracking>(entity =>
            {
                entity.HasKey(p => p.ID);
                entity.ToTable("ProjectTracking");
            });

            modelBuilder.Entity<ProjectTrackingDetail>(entity =>
            {
                entity.HasKey(d => new { d.ID, d.RowNo });
                entity.ToTable("ProjectTrackingDetail");
            });

            modelBuilder.Entity<LoginResultDto>().HasNoKey();
            modelBuilder.Entity<AttributeRuleDto>().HasNoKey();
            modelBuilder.Entity<BranchDto>().HasNoKey();
            modelBuilder.Entity<TicketAttribute>().HasNoKey();
            modelBuilder.Entity<SpResult>().HasNoKey();

            // SP result (Attendance List)
            modelBuilder.Entity<AttendanceHeaderResult>().HasNoKey();


            modelBuilder.Entity<AttributeRule>(entity =>
            {
                entity.HasKey(a => a.AttributeId);
                entity.ToTable("AttributeRule");
            });

            // ===================== Attendance PKs & Table Mapping =====================
            modelBuilder.Entity<AttendanceHeader>(entity =>
            {
                entity.HasKey(a => a.AttendanceID);
                entity.ToTable("AttendanceHeader"); // map to singular table
            });
            modelBuilder.Entity<AttendanceDetail>(entity =>
            {
                entity.HasKey(d => d.DetailID);
                entity.ToTable("AttendanceDetail"); // map to singular table
            });

            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.HasKey(a => a.TicketId);
                entity.ToTable("Ticket");
            });


            modelBuilder.Entity<ReportTrackingParameterResult>().HasNoKey();

            modelBuilder.Entity<TicketTrackingDto>().HasNoKey();
            modelBuilder.Entity<TicketAssignListDto>().HasNoKey();

            modelBuilder.Entity<TicketAssignHeader>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.ToTable("Assignment"); // map to real table name
            });

            modelBuilder.Entity<TicketAssignRow>(entity =>
            {
                entity.HasKey(d => new { d.Id, d.LineNum }); // composite key
                entity.ToTable("Assignment1"); // map to real table name
            });

            modelBuilder.Entity<StaffListAssign>().HasNoKey();
            modelBuilder.Entity<TicketAssignHeaderV2>().HasNoKey();
            modelBuilder.Entity<TicketAssignRowV2>().HasNoKey();
            modelBuilder.Entity<TicketChatHistoryModerator>().HasNoKey();
            modelBuilder.Entity<Feedback>(entity =>
            {
                entity.HasKey(a => a.FeedbackId);
                entity.ToTable("Feedback"); // map to singular table
            });
            modelBuilder.Entity<MyTicketDto>().HasNoKey();
            modelBuilder.Entity<Ticket1>(entity =>
            {
                entity.HasKey(d => new { d.AssignID, d.AssignIDLine }); // composite key
                entity.ToTable("Ticket1"); // map to real table name
            });
            modelBuilder.Entity<TicketCompleteFeedBack>().HasNoKey();

            // New: Ticket Client Header/Row (no real table)
            modelBuilder.Entity<TicketClientHeader>().HasNoKey();
            modelBuilder.Entity<TicketClientRow>().HasNoKey();
            modelBuilder.Entity<AlertDto>().HasNoKey();
            modelBuilder.Entity<ClientTicketSummaryDto>().HasNoKey();
            modelBuilder.Entity<ClientTicketReportDto>().HasNoKey();
            modelBuilder.Entity<TaskTrackingByStaffSummaryDto>().HasNoKey();
            modelBuilder.Entity<BranchMainDto>().HasNoKey();
            modelBuilder.Entity<TaskTrackingByStaffDto>().HasNoKey();
            modelBuilder.Entity<AssignTicketSummaryDto>().HasNoKey();
            modelBuilder.Entity<AssignTicketBySupporterSummaryDto>().HasNoKey();
            modelBuilder.Entity<AssignTicketByCompanySummaryDto>().HasNoKey();

            modelBuilder.Entity<RecurringTask>(entity =>
            {
                entity.HasKey(a => a.TaskID);
                entity.ToTable("RecurringTask");
            });
            modelBuilder.Entity<RecurringTaskResult>().HasNoKey();
            modelBuilder.Entity<user>().HasNoKey();           // used for SP input/output
            modelBuilder.Entity<user1>().HasNoKey();
            modelBuilder.Entity<userList>().HasNoKey();
            modelBuilder.Entity<CompanyBranch>().HasNoKey();
            modelBuilder.Entity<UserBranch>().HasNoKey();
            modelBuilder.Entity<AdvertisementDto>().HasNoKey();
            modelBuilder.Entity<Advertisement>(entity =>
            {
                entity.HasKey(a => a.ID);
                entity.ToTable("Advertisement");
            });
            modelBuilder.Entity<AdvertisementListDto>().HasNoKey();
            modelBuilder.Entity<ClientDto>().HasNoKey();
            modelBuilder.Entity<Client>().HasNoKey();
            modelBuilder.Entity<ProjectAssignResult>().HasNoKey();
            modelBuilder.Entity<ProjectAssignHeader>(entity =>
            {
                entity.HasKey(p => p.ID);
                entity.ToTable("ProjectAssign");
            });

            modelBuilder.Entity<ProjectAssignRow>(entity =>
            {
                entity.HasKey(d => new { d.ID, d.RowNo });
                entity.ToTable("ProjectAssign1");
            });
            modelBuilder.Entity<ProjectAvailable>().HasNoKey();
            modelBuilder.Entity<ProjectAssignRowByID>().HasNoKey();
            modelBuilder.Entity<ProjectAssignHeaderByID>().HasNoKey();
            modelBuilder.Entity<StaffResult>().HasNoKey();
            modelBuilder.Entity<Staff>(entity =>
            {
                entity.HasKey(a => a.StaffId);
                entity.ToTable("Staff");
            });
            modelBuilder.Entity<UserStaff>().HasNoKey();
            modelBuilder.Entity<ApprovalModel>().HasNoKey();
            modelBuilder.Entity<ApprovalHeader>(entity =>
            {
                entity.HasKey(a => a.ID);
                entity.ToTable("Approval"); // map to real table name
            });

            modelBuilder.Entity<TicketAssignRow>(entity =>
            {
                entity.HasKey(d => new { d.Id, d.LineNum }); // composite key
                entity.ToTable("Approval1"); // map to real table name
            });
            modelBuilder.Entity<UserforAssignApproval>().HasNoKey();
            modelBuilder.Entity<ApprovalHeaderRaw>().HasNoKey();
            modelBuilder.Entity<ApprovalHeaderRaw>().HasNoKey();
            modelBuilder.Entity<ProjectTrackingList>().HasNoKey();

            base.OnModelCreating(modelBuilder);
        }
    }
}
