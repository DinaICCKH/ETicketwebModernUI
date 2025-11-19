using ETicketNewUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Reporting.NETCore;
using System.Globalization;
using System.Text.RegularExpressions;

public class ReportController : Controller
{
    private readonly string _connectionString;

    public ReportController(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("TicketDb");
    }

    #region Attendance Form Report

    public IActionResult Export(string reportName, int id, string format = "PDF", bool inline = true)
    {
        var data = GetAttendanceFormData(reportName, id);
        var reportBytes = GenerateAttendanceFormReport(reportName, data, format);

        string mimeType = format switch
        {
            "PDF" => "application/pdf",
            "WORDOPENXML" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "EXCELOPENXML" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/pdf"
        };

        string fileExt = format switch
        {
            "PDF" => "pdf",
            "WORDOPENXML" => "docx",
            "EXCELOPENXML" => "xlsx",
            _ => "pdf"
        };

        string fileName = $"{reportName}.{fileExt}";

        if (format == "PDF" && inline)
            Response.Headers["Content-Disposition"] = $"inline; filename={fileName}";
        else
            Response.Headers["Content-Disposition"] = $"attachment; filename={fileName}";

        return File(reportBytes, mimeType);
    }

    public IActionResult Print(string reportName, int id)
    {
        // Reuse Export with inline PDF
        return Export(reportName, id, "PDF", inline: true);
    }

    private List<AttendanceFormData> GetAttendanceFormData(string reportName, int id)
    {
        if (reportName != "AttendanceForm")
            throw new Exception("Report not implemented");

        var list = new List<AttendanceFormData>();
        using var conn = new SqlConnection(_connectionString);
        conn.Open();
        using var cmd = new SqlCommand("ICC_FORM_Attendance", conn)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@ID", id);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new AttendanceFormData
            {
                AttendanceID = Convert.ToInt32(reader["AttendanceID"]),
                ProjectID = Convert.ToInt32(reader["ProjectID"]),
                CompanyName = reader["CompanyName"].ToString(),
                TrainingDate = Convert.ToDateTime(reader["TrainingDate"]),
                StartDateTime = Convert.ToDateTime(reader["StartDateTime"]),
                EndDateTime = Convert.ToDateTime(reader["EndDateTime"]),
                Status = reader["Status"].ToString(),
                QrCodeLink = reader["QrCodeLink"].ToString(),
                QrCodeImagePath = reader["QrCodeImagePath"].ToString(),
                CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                Remark = reader["Remark"].ToString(),
                Latitude = Convert.ToDecimal(reader["Latitude"]),
                Longitude = Convert.ToDecimal(reader["Longitude"]),
                CreatedBy = reader["CreatedBy"].ToString(),
                UserFullName = reader["UserFullName"].ToString(),
                UserName = reader["UserName"].ToString(),
                Visorder = Convert.ToInt32(reader["Visorder"]),
                ParticipantName = reader["ParticipantName"].ToString(),
                PicturePath = reader["PicturePath"].ToString(),
                PictureTakenDate = Convert.ToDateTime(reader["PictureTakenDate"]),
                DetailLatitude = Convert.ToDecimal(reader["Latitude"]),
                DetailLongitude = Convert.ToDecimal(reader["Longitude"]),
                DeviceName = reader["DeviceName"].ToString(),
                Position = reader["Position"].ToString(),
                DetailRemark = reader["Remark"].ToString()
            });
        }

        return list;
    }

    private byte[] GenerateAttendanceFormReport(string reportName, List<AttendanceFormData> data, string format)
    {
        using var report = new LocalReport();
        report.ReportPath = Path.Combine(Directory.GetCurrentDirectory(), "Printing", $"{reportName}.rdlc");
        report.DataSources.Add(new ReportDataSource("DataSet1", data));
        return report.Render(format);
    }

    #endregion

    #region Client Ticket Report

    public IActionResult ExportClientTicket(
        string reportName,
        string fromDate,
        string toDate,
        string user,
        string createBy,
        string problemType,
        string module,
        string status,
        string entryPrimary,
        string format = "PDF",
        bool inline = true)
    {
        var data = GetClientTicketReportData(fromDate, toDate, user, createBy, problemType, module, status, entryPrimary);
        var reportBytes = GenerateClientTicketReport(reportName, data, format);

        string mimeType = format switch
        {
            "PDF" => "application/pdf",
            "WORDOPENXML" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "EXCELOPENXML" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/pdf"
        };

        string fileExt = format switch
        {
            "PDF" => "pdf",
            "WORDOPENXML" => "docx",
            "EXCELOPENXML" => "xlsx",
            _ => "pdf"
        };

        string fileName = $"{reportName}.{fileExt}";

        if (format == "PDF" && inline)
            Response.Headers["Content-Disposition"] = $"inline; filename={fileName}";
        else
            Response.Headers["Content-Disposition"] = $"attachment; filename={fileName}";

        return File(reportBytes, mimeType);
    }

    private List<ClientTicketReportDto> GetClientTicketReportData(
        string fromDate,
        string toDate,
        string user,
        string createBy,
        string problemType,
        string module,
        string status,
        string entryPrimary)
    {
        var list = new List<ClientTicketReportDto>();

        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        using var cmd = new SqlCommand("ICC_REPORT_ClientTICKET", conn)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@FromDate", fromDate);
        cmd.Parameters.AddWithValue("@ToDate", toDate);
        cmd.Parameters.AddWithValue("@User", user ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@CreateBy", createBy ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ProblemType", problemType ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Module", module ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", status ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@EntryPrimary", entryPrimary ?? (object)DBNull.Value);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ClientTicketReportDto
            {
                TicketId = reader["TicketId"] as int?,
                RefID = reader["RefID"] as string,
                Title = reader["Title"] as string,
                Description = reader["Description"] as string,
                Status = reader["Status"] as string,
                Priority = reader["Priority"] as string,
                CreatedDate = reader["CreatedDate"] as string,
                UpdatedDate = reader["UpdatedDate"] as string,
                DeadlineDate = reader["DeadlineDate"] as string,
                CloseDate = reader["CloseDate"] as string,
                OverDate = reader["OverDate"] as int?,
                CreateBy = reader["CreateBy"] as string,
                UpdateBy = reader["UpdateBy"] as string,
                Attachment = reader["Attachment"] as string,
                ProblemType = reader["ProblemType"] as string,
                Module = reader["Module"] as string,
                Branch = reader["Branch"] as string,
                SubProblemType = reader["SubProblemType"] as string,
                Followup = reader["Followup"] as string,
                CancelStatus = reader["CancelStatus"] as string,
                CreateByID = reader["CreateByID"] as string,
                CreateByName = reader["CreateByName"] as string,
                PFromDate = reader["PFromDate"] as string,
                PToDate = reader["PToDate"] as string,
                PUser = reader["PUser"] as string,
                PCreateBy = reader["PCreateBy"] as string,
                PProblemType = reader["PProblemType"] as string,
                PModule = reader["PModule"] as string,
                PStatus = reader["PStatus"] as string,
                PEntryPrimary = reader["PEntryPrimary"] as string,
                PUserLoginName = reader["PUserLoginName"] as string,
                AssignBy = reader["AssignBy"] as string,
                AssignTo = reader["AssignTo"] as string
            });
        }

        return list;
    }

    private byte[] GenerateClientTicketReport(string reportName, List<ClientTicketReportDto> data, string format)
    {
        using var report = new LocalReport();
        report.ReportPath = Path.Combine(Directory.GetCurrentDirectory(), "Printing", $"{reportName}.rdlc");
        report.DataSources.Add(new ReportDataSource("DataSet1", data));
        string deviceInfo = $@"
        <DeviceInfo>
            <OutputFormat>{format}</OutputFormat>
            <PageWidth>11.69in</PageWidth>   <!-- A4 Landscape width -->
            <PageHeight>8.27in</PageHeight>  <!-- A4 Landscape height -->
            <MarginTop>0.5in</MarginTop>
            <MarginLeft>0.5in</MarginLeft>
            <MarginRight>0.5in</MarginRight>
            <MarginBottom>0.5in</MarginBottom>
        </DeviceInfo>";
        return report.Render(format);
    }

    #endregion

    #region Working Status Report By Staff

    public IActionResult ExportWorkingStatusReportByStaff(
        string fromDate,
        string toDate,
        string staffId,
        string company,
        string assignBy,
        string status,
        string format = "PDF",
        bool inline = true)
    {
        var data = GetWorkingStatusReportByStaffData(fromDate, toDate, staffId, company, assignBy, status);
        var reportBytes = GenerateWorkingStatusReportByStaffReport("WorkingStatusReportByStaff", data, format);

        string mimeType = format switch
        {
            "PDF" => "application/pdf",
            "WORDOPENXML" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "EXCELOPENXML" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/pdf"
        };

        string fileExt = format switch
        {
            "PDF" => "pdf",
            "WORDOPENXML" => "docx",
            "EXCELOPENXML" => "xlsx",
            _ => "pdf"
        };

        string fileName = $"WorkingStatusReportByStaff.{fileExt}";

        if (format == "PDF" && inline)
            Response.Headers["Content-Disposition"] = $"inline; filename={fileName}";
        else
            Response.Headers["Content-Disposition"] = $"attachment; filename={fileName}";

        return File(reportBytes, mimeType);
    }


    private static string NormalizeDate(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        var monthMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "January", "Jan" }, { "Jan", "Jan" },
                    { "February", "Feb" }, { "Feb", "Feb" },
                    { "March", "Mar" }, { "Mar", "Mar" },
                    { "April", "Apr" }, { "Apr", "Apr" },
                    { "May", "May" },
                    { "June", "Jun" }, { "Jun", "Jun" },
                    { "July", "Jul" }, { "Jul", "Jul" },
                    { "August", "Aug" }, { "Aug", "Aug" },
                    { "September", "Sep" }, { "Sept", "Sep" }, { "Sep", "Sep" },
                    { "October", "Oct" }, { "Oct", "Oct" },
                    { "November", "Nov" }, { "Nov", "Nov" },
                    { "December", "Dec" }, { "Dec", "Dec" }
                };

        foreach (var kvp in monthMap)
        {
            if (input.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                input = Regex.Replace(input, kvp.Key, kvp.Value, RegexOptions.IgnoreCase);
                break;
            }
        }

        return input;
    }



    private List<TaskTrackingByStaffDto> GetWorkingStatusReportByStaffData(
        string fromDate,
        string toDate,
        string staffId,
        string company,
        string assignBy,
        string status)
    {
        var list = new List<TaskTrackingByStaffDto>();

        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        using var cmd = new SqlCommand("ICC_RPT_TaskTracking_ByStaff", conn)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };

        var startDate = !string.IsNullOrEmpty(fromDate)
                   ? DateTime.ParseExact(NormalizeDate(fromDate), "dd-MMM-yyyy", CultureInfo.InvariantCulture)
                   : new DateTime(DateTime.Now.Year, 1, 1);

        var endDate = !string.IsNullOrEmpty(toDate)
            ? DateTime.ParseExact(NormalizeDate(toDate), "dd-MMM-yyyy", CultureInfo.InvariantCulture)
            : DateTime.Today;

        cmd.Parameters.AddWithValue("@StaffID", staffId ?? "ALL");
        cmd.Parameters.AddWithValue("@FromDate", startDate);
        cmd.Parameters.AddWithValue("@ToDate", endDate);
        cmd.Parameters.AddWithValue("@Company", company ?? "ALL");
        cmd.Parameters.AddWithValue("@AssignBy", assignBy ?? "ALL");
        cmd.Parameters.AddWithValue("@Status", status ?? "ALL"); // supports comma-separated values

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new TaskTrackingByStaffDto
            {
                Type = reader["Type"].ToString(),
                AssigBy = reader["AssigBy"].ToString(),
                AssigByName = reader["AssigByName"].ToString(),
                AssignTo = reader["AssignTo"].ToString(),
                AssignToName = reader["AssignToName"].ToString(),
                RequestBy = reader["RequestBy"].ToString(),
                RequestByName = reader["RequestByName"].ToString(),
                RequestDate = reader["RequestDate"] as DateTime?,
                AssignDate = reader["AssignDate"] as DateTime?,
                DeadlineDate = reader["DeadlineDate"] as DateTime?,
                Remark = reader["Remark"].ToString(),
                ProgessStatus = reader["ProgessStatus"].ToString(),
                Status = reader["Status"].ToString(),
                IssueLevel = reader["IssueLevel"].ToString(),
                BranchID = reader["BranchID"].ToString(),
                BranchName = reader["BranchName"].ToString(),
                MainCompany = reader["MainCompany"].ToString(),
                MainCompanyName = reader["MainCompanyName"].ToString(),
                Title = reader["Title"].ToString(),
                Module = reader["Module"]?.ToString() ?? reader["ModuleCode"]?.ToString(),
                Problem = reader["Problem"]?.ToString() ?? reader["ProblemName"]?.ToString(),
                SubProblem = reader["SubProblem"].ToString(),
                Priority = reader["Priority"].ToString(),
                FromDate = reader["FromDate"] as DateTime?,
                ToDate = reader["ToDate"] as DateTime?,
                Over = reader["Over"] as int?,
                UpdateDate = reader["UpdateDate"] as DateTime?,
                OverOfDone = reader["OverOfDone"] as int?,
                Id = reader["Id"] as int?,
                LineNum = reader["LineNum"] as int?,
                TicketRef = reader["TicketRef"].ToString(),
                FinalUpdateDoneDate = reader["FinalUpdateDoneDate"] as DateTime?
            });
        }

        return list;
    }

    private byte[] GenerateWorkingStatusReportByStaffReport(string reportName, List<TaskTrackingByStaffDto> data, string format)
    {
        using var report = new LocalReport();
        report.ReportPath = Path.Combine(Directory.GetCurrentDirectory(), "Printing", $"{reportName}.rdlc");
        report.DataSources.Add(new ReportDataSource("DataSet1", data));

        string deviceInfo = $@"
    <DeviceInfo>
        <OutputFormat>{format}</OutputFormat>
        <PageWidth>11.69in</PageWidth>
        <PageHeight>8.27in</PageHeight>
        <MarginTop>0.5in</MarginTop>
        <MarginLeft>0.5in</MarginLeft>
        <MarginRight>0.5in</MarginRight>
        <MarginBottom>0.5in</MarginBottom>
    </DeviceInfo>";

        return report.Render(format);
    }

    #endregion


}
