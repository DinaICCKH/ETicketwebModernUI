using ETicketNewUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ModernUIEticket.Controllers
{
    public class AsignTicketController : Controller
    {
        private readonly TicketDbContext _context;

        public AsignTicketController(TicketDbContext context)
        {
            _context = context;
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

        #region Helper Methods
        private LoginResultDto GetTokenData()
        {
            var tokenBase64 = HttpContext.Session.GetString("UserToken");
            if (string.IsNullOrEmpty(tokenBase64)) return null;

            try
            {
                var tokenJson = Encoding.UTF8.GetString(Convert.FromBase64String(tokenBase64));
                return JsonConvert.DeserializeObject<LoginResultDto>(tokenJson);
            }
            catch
            {
                return null;
            }
        }

        private async Task<List<BranchDto>> GetBranchListAsync(LoginResultDto tokenData)
        {
            if (tokenData == null) return new List<BranchDto>();

            return await _context.Branches
                .FromSqlRaw("EXEC ICC_GET_Branch @UserID, @Company",
                    new SqlParameter("@UserID", tokenData.UserId),
                    new SqlParameter("@Company", tokenData.CompanyId))
                .AsNoTracking()
                .ToListAsync();
        }

        private async Task<List<BranchDto>> GetBranchClientListAsync(LoginResultDto tokenData)
        {
            if (tokenData == null) return new List<BranchDto>();

            return await _context.Branches
                .FromSqlRaw("EXEC ICC_GET_Branch_Client @UserID, @Company",
                    new SqlParameter("@UserID", tokenData.UserId),
                    new SqlParameter("@Company", tokenData.CompanyId))
                .AsNoTracking()
                .ToListAsync();
        }

        private async Task<List<TicketAttribute>> GetAttributeRulesAsync(LoginResultDto tokenData)
        {
            if (tokenData == null) return new List<TicketAttribute>();

            return await _context.Attributes
                .FromSqlRaw("EXEC ICC_GET_AttributeRule @EntryPrimary, @JsonBody, @Type",
                    new SqlParameter("@EntryPrimary", tokenData.UserId),
                    new SqlParameter("@JsonBody", ""),
                    new SqlParameter("@Type", "ALL"))
                .AsNoTracking()
                .ToListAsync();
        }
        #endregion




        public async Task<IActionResult> Index(
           string status = "Request",
           DateTime? dateFrom = null,
           DateTime? dateTo = null,
           string priority = "ALL",
           string branchCompany = "ALL",
           string createBy = "ALL")
        {
            var token = GetTokenData();
            if (token == null) return RedirectToAction("Login", "Logout");

            // ------------------- 1) Ticket Tracking List -------------------
            var tickets = await _context.ICC_GET_TICKET_AssignListV2Results
                .FromSqlInterpolated($@"
            EXEC ICC_GET_TICKET_AssignListV2
                @User = {token.UserId},
                @Status = {status},
                @DateFrom = {dateFrom},
                @DateTo = {dateTo},
                @Priority = {priority},
                @BranchCompany = {branchCompany},
                @CreateBy = {createBy}")
                .ToListAsync();

            // ------------------- 2) Report Tracking Parameters -------------------
            // Branch list
            var branchList = await _context.ReportTrackingResults
                .FromSqlInterpolated($"EXEC ICC_ReportTracking_ParameterV2 @Type = {"Branch"}")
                .ToListAsync();

            // CreateBy list
            var createByList = await _context.ReportTrackingResults
                .FromSqlInterpolated($"EXEC ICC_ReportTracking_ParameterV2 @Type = {"CreateBy"}")
                .ToListAsync();

            // ------------------- 3) Pass to View -------------------
            ViewBag.BranchList = branchList;
            ViewBag.CreateByList = createByList;

            return View(tickets);
        }


        public async Task<IActionResult> TrackingTikcetAssing(
           string status = "Pending,Progress",
           DateTime? dateFrom = null,
           DateTime? dateTo = null,
           string priority = "ALL",
           string branchCompany = "ALL",
           string createBy = "ALL")
        {
            var token = GetTokenData();
            if (token == null) return RedirectToAction("Login", "Logout");

            // ------------------- 1) Ticket Tracking List -------------------
            var tickets = await _context.ICC_GET_TICKET_AssignListV2Results
                .FromSqlInterpolated($@"
            EXEC ICC_GET_TICKET_AssignListV2
                @User = {token.UserId},
                @Status = {status},
                @DateFrom = {dateFrom},
                @DateTo = {dateTo},
                @Priority = {priority},
                @BranchCompany = {branchCompany},
                @CreateBy = {createBy}")
                .ToListAsync();

            // ------------------- 2) Report Tracking Parameters -------------------
            // Branch list
            var branchList = await _context.ReportTrackingResults
                .FromSqlInterpolated($"EXEC ICC_ReportTracking_ParameterV2 @Type = {"Branch"}")
                .ToListAsync();

            // CreateBy list
            var createByList = await _context.ReportTrackingResults
                .FromSqlInterpolated($"EXEC ICC_ReportTracking_ParameterV2 @Type = {"CreateBy"}")
                .ToListAsync();

            // ------------------- 3) Pass to View -------------------
            ViewBag.BranchList = branchList;
            ViewBag.CreateByList = createByList;
            ViewBag.CurrentUser = token.UserId;

            return View(tickets);
        }


        [HttpGet]
        public async Task<IActionResult> SaveTicketAssign(int id)
        {
            var token = GetTokenData();
            if (token == null) return RedirectToAction("Login", "Logout");

            // Load ticket info
            var ticketResult = await _context.ICC_GET_TICKET_TrackingListV2Results
             .FromSqlInterpolated($"EXEC ICC_GET_TICKET_ByIDV2 @ID = {id}")
             .ToListAsync();

            var ticket = ticketResult.FirstOrDefault();  // take the first item

            if (ticket == null) return NotFound();

            var staffList = await _context.StaffListAssignResult
              .FromSqlInterpolated($"EXEC ICC_GET_StaffByAssignCompanyV2 @Branch = {ticket.BranchID},@ID={id}")
              .ToListAsync();

            var supporterList = await _context.StaffListAssignResult
              .FromSqlInterpolated($"EXEC ICC_GET_StaffAllV2")
              .ToListAsync();

            ViewBag.StaffList = staffList;
            ViewBag.SupporterList = supporterList;
            // Pass ticket to the view
            return View(ticket);
        }

        [HttpPost]
        public async Task<IActionResult> SaveTicketAssign([FromBody] TicketAssignHeader model)
        {
            var token = GetTokenData();
            if (token == null)
                return Json(new { success = false, message = "Unauthorized" });

            model.Mode ??= "Add";
            model.CreateBy = token.UserId.ToString();
            model.UpdateBy = token.UserId.ToString();
            model.CreatedDate ??= DateTime.Now;
            model.UpdatedDate ??= DateTime.Now;
            model.Status ??= "A";
            model.Progress ??= "Assign to Suppporter";

            // Ensure RowList dates
            if (model.RowList != null)
            {
                foreach (var row in model.RowList)
                {
                    row.AssignDate ??= DateTime.Now;
                    row.ProgessStatus = "Start";
                    row.Status = "A";
                    // DeadlineDate is nullable; JSON.NET will parse ISO strings automatically
                }
            }

            var jsonString = JsonConvert.SerializeObject(model);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_TaskAssigment @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "Task"),
                        new SqlParameter("@Trantype", model.Mode == "Add" ? "A" : "U"),
                        new SqlParameter("@EntryPrimary", model.Id.HasValue ? model.Id.Value.ToString() : "0"),
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();
                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });

                return Json(new { success = false, message = result?.Message ?? "Error saving assignment." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while saving assignment." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RejectTicket(string TicketId, string Reason)
        {
            var token = GetTokenData();
            if (token == null)
                return Json(new { success = false, message = "Unauthorized" });

            if (string.IsNullOrWhiteSpace(Reason))
                return Json(new { success = false, message = "Reason is required" });

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_UPDATE_TICKET_STATUS @User, @EntryPrimary, @Status, @Reason",
                        new SqlParameter("@User", token.UserId),
                        new SqlParameter("@EntryPrimary", TicketId),
                        new SqlParameter("@Status", "Reject"),
                        new SqlParameter("@Reason", Reason))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();
                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });

                return Json(new { success = false, message = result?.Message ?? "Error rejecting ticket." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while rejecting ticket." });
            }
        }


        [HttpGet]
        public async Task<IActionResult> EditTicketAssign(string id)
        {
            var token = GetTokenData();
            if (token == null) return RedirectToAction("Login", "Logout");

            // ------------------- 1) Load ticket info -------------------
            var ticketResult = await _context.ICC_GET_TICKET_TrackingListV2Results
                .FromSqlInterpolated($"EXEC ICC_GET_TICKET_ByIDV2 @ID = {id}")
                .ToListAsync();

            var ticket = ticketResult.FirstOrDefault();
            if (ticket == null) return NotFound();

            // ------------------- 2) Load assignment header -------------------
            var headerResult = await _context.TicketAssignHeaderV2Result
                .FromSqlInterpolated($"EXEC ICC_GET_TICKET_AssignHeaderV2 @ID = {id.ToString()}")
                .ToListAsync();

            var header = headerResult.FirstOrDefault();
            if (ticket == null) return NotFound();

            // ------------------- 3) Load assignment rows -------------------
            var rows = await _context.TicketAssignRowV2Result
                .FromSqlInterpolated($"EXEC ICC_GET_TICKET_AssignRowV2 @ID = {header.Id.ToString()}")
                .ToListAsync();

            // Ensure we have an empty list if no rows
            rows ??= new List<TicketAssignRowV2>();

            // ------------------- 4) Load staff lists -------------------
            var staffList = await _context.StaffListAssignResult
                .FromSqlInterpolated($"EXEC ICC_GET_StaffByAssignCompanyV2 @Branch = {ticket.BranchID}, @ID = {id}")
                .ToListAsync();

            var supporterList = await _context.StaffListAssignResult
                .FromSqlInterpolated($"EXEC ICC_GET_StaffAllV2")
                .ToListAsync();

            // ------------------- 5) Pass data to View -------------------

            ViewBag.Header = header;
            ViewBag.Rows = rows;
            ViewBag.StaffList = staffList;
            ViewBag.SupporterList = supporterList;

            return View(ticket);
        }


        [HttpPost]
        public async Task<IActionResult> EditTicketAssign([FromBody] TicketAssignHeader model)
        {
            var token = GetTokenData();
            if (token == null)
                return Json(new { success = false, message = "Unauthorized" });

            model.Mode ??= "Update";
            model.UpdateBy = token.UserId.ToString();
            model.CreatedDate = DateTime.Parse(model.CreatedDate.Value
                .ToUniversalTime()
                .ToString("yyyy-MM-ddTHH:mm:ss"));

            model.UpdatedDate = DateTime.Parse(DateTime.Now
                .ToUniversalTime()
                .ToString("yyyy-MM-ddTHH:mm:ss"));
            model.Status ??= "Request";
            model.Progress ??= "Pending";

            // Ensure RowList dates
            if (model.RowList != null)
            {
                foreach (var row in model.RowList)
                {
                  
                    row.ProgessStatus ??= "Start";
                    row.AssignDate ??= DateTime.Now;
                    row.AssignDate = DateTime.Parse(row.AssignDate.Value
                        .ToUniversalTime()
                        .ToString("yyyy-MM-ddTHH:mm:ss"));

                    row.DeadlineDate ??= DateTime.Now;
                    row.DeadlineDate = DateTime.Parse(row.DeadlineDate.Value
                        .ToUniversalTime()
                        .ToString("yyyy-MM-ddTHH:mm:ss"));

                    row.TransferDate = DateTime.Parse(DateTime.Now
                        .ToUniversalTime()
                        .ToString("yyyy-MM-ddTHH:mm:ss"));

                    // DeadlineDate is nullable; JSON.NET will parse ISO strings automatically
                }
            }

            var jsonString = JsonConvert.SerializeObject(model);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_TaskAssigment @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "Task"),
                        new SqlParameter("@Trantype", model.Mode == "Add" ? "A" : "U"),
                        new SqlParameter("@EntryPrimary", model.Id.HasValue ? model.Id.Value.ToString() : "0"),
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();
                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });

                return Json(new { success = false, message = result?.Message ?? "Error saving assignment." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while saving assignment." });
            }
        }


        [HttpGet]
        public async Task<IActionResult> TicketHistory(string id)
        {
            var token = GetTokenData();
            if (token == null) return RedirectToAction("Login", "Logout");

            // ------------------- Load chat history -------------------
            var chatResult = await _context.TicketChatHistoryModeratorResult
                .FromSqlInterpolated($@" EXEC ICC_GET_TICKETChatHistory_Moderator 
                    @User = {token.UserId}, 
                    @EntryPrimary = {id}, 
                    @JsonBody = ''")
                .ToListAsync();

            if (chatResult == null || !chatResult.Any()) return NotFound();

            // Pass current user ID to View via ViewBag
            ViewBag.CurrentUserId = token.UserId.ToString();
            // Load ticket info
            var ticketResult = await _context.ICC_GET_TICKET_TrackingListV2Results
             .FromSqlInterpolated($"EXEC ICC_GET_TICKET_ByIDV2 @ID = {id}")
             .ToListAsync();
            var ticket = ticketResult.FirstOrDefault();  // take the first item

            if (ticket == null) return NotFound();

            ViewBag.Ticket = ticket;

            return View(chatResult);
        }


        [HttpPost]
        public async Task<IActionResult> Comment(string TicketRef, string FeedbackText)
        {
            var token = GetTokenData();
            if (token == null)
                return Json(new { success = false, message = "Unauthorized" });

            // Build JSON body for SP
            var jsonBody = new
            {
                SecretCode = "Tga5vz5oWiClgzayaLMZAx7KoTGLSJGrCQkY01gNb0c=",
                FeedbackId = -1,
                TicketRef = TicketRef,
                Rating = 0,
                FeedbackText = FeedbackText,
                Type = "TickModeratorComment",
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                CreateBy = token.UserId.ToString(),
                UpdateBy = token.UserId.ToString()
            };

            // Serialize JSON
            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                // Call stored procedure
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Feedback @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@Trantype", "A"),
                        new SqlParameter("@EntryPrimary", TicketRef ?? (object)DBNull.Value),
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();
                if (result != null && result.Code == 200)
                {
                    return Json(new { success = true, message = result.Message });
                }
                else
                {
                    return Json(new { success = false, message = result?.Message ?? "Error adding comment." });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while adding comment." });
            }
        }


        [HttpGet]
        public async Task<IActionResult> RPTTicketTrackingbystaff(string? fromDatesummary, string? toDatesummary)
        {
            var token = GetTokenData();
            if (token == null)
                return RedirectToAction("Login", "Logout");

            try
            {
                var startDate = !string.IsNullOrEmpty(fromDatesummary)
                   ? DateTime.ParseExact(NormalizeDate(fromDatesummary), "dd-MMM-yyyy", CultureInfo.InvariantCulture)
                   : new DateTime(DateTime.Now.Year, 1, 1);

                var endDate = !string.IsNullOrEmpty(toDatesummary)
                    ? DateTime.ParseExact(NormalizeDate(toDatesummary), "dd-MMM-yyyy", CultureInfo.InvariantCulture)
                    : DateTime.Today;

                var summaryList = await _context.Set<TaskTrackingByStaffSummaryDto>()
                    .FromSqlInterpolated($@"
                EXEC ICC_RPT_TaskTracking_ByStaff_Summary 
                     @User=-1, 
                     @FromDate={startDate}, 
                     @ToDate={endDate}")
                    .AsNoTracking()
                    .ToListAsync();

                // If nothing found, pass an empty list instead of null
                if (summaryList == null || !summaryList.Any())
                    summaryList = new List<TaskTrackingByStaffSummaryDto>();

                var supporterList = await _context.StaffListAssignResult
                     .FromSqlInterpolated($"EXEC ICC_GET_StaffAllV2")
                     .ToListAsync();

                var branchList = await _context.BranchMainDtos
                    .FromSqlInterpolated($"EXEC ICC_GET_Branch_Main")
                    .ToListAsync();

                ViewBag.CurrentUser = token.UserId;
                ViewBag.Branch = branchList;
                ViewBag.Staff = supporterList;
                ViewBag.FromDate = startDate.ToString("dd-MMM-yyyy");
                ViewBag.ToDate = endDate.ToString("dd-MMM-yyyy");

                return View("RPTTicketTrackingbystaff", summaryList);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("Error");
            }
        }


        [HttpGet]
        public async Task<IActionResult> ReportPreview(string? fromDate, string? toDate)
        {
           

            var startDate = !string.IsNullOrEmpty(fromDate)
                   ? DateTime.ParseExact(NormalizeDate(fromDate), "dd-MMM-yyyy", CultureInfo.InvariantCulture)
                   : new DateTime(DateTime.Now.Year, 1, 1);

            var endDate = !string.IsNullOrEmpty(toDate)
                ? DateTime.ParseExact(NormalizeDate(toDate), "dd-MMM-yyyy", CultureInfo.InvariantCulture)
                : DateTime.Today;

            var token = GetTokenData();
            if (token == null)
                return RedirectToAction("Login", "Logout");

            try
            {
                var summary = await _context.Set<TaskTrackingByStaffSummaryDto>()
                   .FromSqlInterpolated($@"
                EXEC ICC_RPT_TaskTracking_ByStaff_Summary 
                    @User=-1, 
                    @FromDate={startDate}, 
                    @ToDate={endDate}")
                   .AsNoTracking()
                   .ToListAsync();

                if (summary == null)
                    return View("ReportPreview", new ClientTicketSummaryDto());
                var attributeList = await GetAttributeRulesAsync(token);
                ViewBag.Modules = attributeList.Where(a => a.AType == "Module").ToList();
                ViewBag.ProblemTypes = attributeList.Where(a => a.AType == "ProblemType").ToList();
                ViewBag.CurrentUser = token.UserId;
                return View("ReportPreview", summary);
            }
            catch (Exception ex)
            {

                ViewBag.Error = ex.Message;
                return View("Error");
            }
        }




        [HttpGet]
        public async Task<IActionResult> RPTTicketAssign()
        {
            var token = GetTokenData();
            if (token == null)
                return RedirectToAction("Login", "Logout");

            try
            {
                // ------------------- 1) Ticket Assignment Summary -------------------
                var summaryResult = await _context.Set<AssignTicketSummaryDto>()
                    .FromSqlInterpolated($@"EXEC ICC_RPT_AssignTicket_Summary {token.UserId}")
                    .AsNoTracking()
                    .ToListAsync(); // Only one row is returned

                if (summaryResult == null)
                    summaryResult = new List<AssignTicketSummaryDto> { new AssignTicketSummaryDto() };

                ViewBag.CurrentUser = token.UserId;

                // ------------------- 2) Ticket Assignment By Supporter Summary -------------------
                var supporterSummaryResult = await _context.Set<AssignTicketBySupporterSummaryDto>()
                    .FromSqlInterpolated($@"EXEC ICC_RPT_AssignTicket_BySupporter_Summary {token.UserId}")
                    .AsNoTracking()
                    .ToListAsync();

                if (supporterSummaryResult == null)
                    supporterSummaryResult = new List<AssignTicketBySupporterSummaryDto>();

                // Pass supporter summary to ViewBag
                ViewBag.SupporterSummary = supporterSummaryResult;

                // ------------------- 3) Ticket Assignment By Company Summary -------------------
                var companySummaryResult = await _context.Set<AssignTicketByCompanySummaryDto>()
                    .FromSqlInterpolated($@"EXEC ICC_RPT_AssignTicket_ByCompany_Summary {token.UserId}")
                    .AsNoTracking()
                    .ToListAsync();

                if (companySummaryResult == null)
                    companySummaryResult = new List<AssignTicketByCompanySummaryDto>();

                // Pass company summary to ViewBag
                ViewBag.CompanySummary = companySummaryResult;

                return View("RPTTicketAssign", summaryResult);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("Error");
            }
        }




        /// <summary> API Part

        [HttpGet("api/GetPendingAssign")]
        public async Task<IActionResult> PendingAssignAPI(
           [FromQuery] string status = "Request",
           [FromQuery] DateTime? dateFrom = null,
           [FromQuery] DateTime? dateTo = null,
           [FromQuery] string priority = "ALL",
           [FromQuery] string branchCompany = "ALL",
           [FromQuery] string createBy = "ALL")
        {
            // ----- 1) Get UserId from Request Header -----
            if (!Request.Headers.TryGetValue("UserId", out var userIdHeader))
                return Unauthorized(new { message = "Missing UserId header" });

            int userId = int.Parse(userIdHeader);

            // ----- 2) Ticket Tracking -----
            var tickets = await _context.ICC_GET_TICKET_AssignListV2Results
                .FromSqlInterpolated($@"
            EXEC ICC_GET_TICKET_AssignListV2
                @User = {userId},
                @Status = {status},
                @DateFrom = {dateFrom},
                @DateTo = {dateTo},
                @Priority = {priority},
                @BranchCompany = {branchCompany},
                @CreateBy = {createBy}")
                .ToListAsync();

            // ----- 3) Report Tracking Parameters -----
            var branchList = await _context.ReportTrackingResults
                .FromSqlInterpolated($"EXEC ICC_ReportTracking_ParameterV2 @Type = {"Branch"}")
                .ToListAsync();

            var createByList = await _context.ReportTrackingResults
                .FromSqlInterpolated($"EXEC ICC_ReportTracking_ParameterV2 @Type = {"CreateBy"}")
                .ToListAsync();

            // ----- 4) Return JSON -----
            return Ok(new
            {
                tickets,
                branchList,
                createByList
            });
        }


        [HttpGet("api/GetAssignTicketByID")]
        public async Task<IActionResult> GetAssignTicketByIDAPI([FromQuery] int id)
        {

            // ---- 2) Load Ticket Info ----
            var ticketResult = await _context.ICC_GET_TICKET_TrackingListV2Results
                .FromSqlInterpolated($"EXEC ICC_GET_TICKET_ByIDV2 @ID = {id}")
                .ToListAsync();

            var ticket = ticketResult.FirstOrDefault();

            if (ticket == null)
                return NotFound(new { message = "Ticket not found" });

            // ---- 3) Staff List for Assign ----
            var staffList = await _context.StaffListAssignResult
                .FromSqlInterpolated($"EXEC ICC_GET_StaffByAssignCompanyV2 @Branch = {ticket.BranchID}, @ID = {id}")
                .ToListAsync();

            // ---- 4) Supporter List ----
            var supporterList = await _context.StaffListAssignResult
                .FromSqlInterpolated($"EXEC ICC_GET_StaffAllV2")
                .ToListAsync();

            // ---- 5) Return JSON ----
            return Ok(new
            {
                ticket,
                staffList,
                supporterList
            });
        }


        [HttpPost("api/SaveAssignTicket")]
        public async Task<IActionResult> SaveAssignTicketAPI([FromBody] TicketAssignHeader model)
        {
            // ---- 1) Get UserId from Header ----
            if (!Request.Headers.TryGetValue("UserId", out var userIdHeader))
                return Json(new { success = false, message = "Missing UserId header" });

            int userId = int.Parse(userIdHeader);

            // ---- 2) Set Defaults ----
            model.Mode ??= "Add";
            model.CreateBy = userId.ToString();
            model.UpdateBy = userId.ToString();
            model.CreatedDate ??= DateTime.Now;
            model.UpdatedDate ??= DateTime.Now;
            model.Status ??= "A";
            model.Progress ??= "Assign to Suppporter";

            if (model.RowList != null)
            {
                foreach (var row in model.RowList)
                {
                    row.AssignDate ??= DateTime.Now;
                    row.ProgessStatus = "Start";
                    row.Status = "A";
                }
            }

            var jsonString = JsonConvert.SerializeObject(model);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw(
                        "EXEC dbo.ICC_TaskAssigment @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "Task"),
                        new SqlParameter("@Trantype", model.Mode == "Add" ? "A" : "U"),
                        new SqlParameter("@EntryPrimary", model.Id.HasValue ? model.Id.Value.ToString() : "0"),
                        new SqlParameter("@JsonBody", jsonString)
                    )
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();

                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });

                return Json(new { success = false, message = result?.Message ?? "Error saving assignment." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Unexpected error occurred while saving assignment." });
            }
        }



        [HttpPost("api/RejectAssignTicket")]
        public async Task<IActionResult> RejectTicketAPI()
        {
            // ---- 1) Get UserId from Header ----
            if (!Request.Headers.TryGetValue("UserId", out var userIdHeader))
                return Json(new { success = false, message = "Missing UserId header" });

            int userId = int.Parse(userIdHeader);

            // ---- 2) Get TicketId and Reason from headers ----
            if (!Request.Headers.TryGetValue("TicketId", out var ticketIdHeader))
                return Json(new { success = false, message = "Missing TicketId header" });

            if (!Request.Headers.TryGetValue("Reason", out var reasonHeader))
                return Json(new { success = false, message = "Missing Reason header" });

            string TicketId = ticketIdHeader.ToString();
            string Reason = reasonHeader.ToString();

            if (string.IsNullOrWhiteSpace(TicketId))
                return Json(new { success = false, message = "TicketId is required" });

            if (string.IsNullOrWhiteSpace(Reason))
                return Json(new { success = false, message = "Reason is required" });

            try
            {
                // ---- 3) Execute Stored Procedure ----
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw(
                        "EXEC dbo.ICC_UPDATE_TICKET_STATUS @User, @EntryPrimary, @Status, @Reason",
                        new SqlParameter("@User", userId),
                        new SqlParameter("@EntryPrimary", TicketId),
                        new SqlParameter("@Status", "Reject"),
                        new SqlParameter("@Reason", Reason)
                    )
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();

                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });

                return Json(new { success = false, message = result?.Message ?? "Error rejecting ticket." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Unexpected error occurred while rejecting ticket." });
            }
        }


        [HttpGet("api/TrackingTicketAssign")]
        public async Task<IActionResult> TrackingTicketAssignAPI(
            [FromHeader] string? status = "Pending,Progress",
            [FromHeader] DateTime? dateFrom = null,
            [FromHeader] DateTime? dateTo = null,
            [FromHeader] string? priority = "ALL",
            [FromHeader] string? branchCompany = "ALL",
            [FromHeader] string? createBy = "ALL")
        {
            // ---- 1) Get UserId from headers ----
            if (!Request.Headers.TryGetValue("UserId", out var userIdHeader))
                return Json(new { success = false, message = "Missing UserId header" });

            int userId = int.Parse(userIdHeader);

            // ---- 2) Ticket Tracking List ----
            var tickets = await _context.ICC_GET_TICKET_AssignListV2Results
                .FromSqlInterpolated($@"
            EXEC ICC_GET_TICKET_AssignListV2
                @User = {userId},
                @Status = {status},
                @DateFrom = {dateFrom},
                @DateTo = {dateTo},
                @Priority = {priority},
                @BranchCompany = {branchCompany},
                @CreateBy = {createBy}")
                .ToListAsync();

            // ---- 3) Report Tracking Parameters ----
            var branchList = await _context.ReportTrackingResults
                .FromSqlInterpolated($"EXEC ICC_ReportTracking_ParameterV2 @Type = {"Branch"}")
                .ToListAsync();

            var createByList = await _context.ReportTrackingResults
                .FromSqlInterpolated($"EXEC ICC_ReportTracking_ParameterV2 @Type = {"CreateBy"}")
                .ToListAsync();

            // ---- 4) Return JSON ----
            return Ok(new
            {
                success = true,
                tickets,
                branchList,
                createByList,
                currentUser = userId
            });
        }


        [HttpGet("api/GetEditTicketAssignByID")]
        public async Task<IActionResult> GetEditTicketAssignByIDAPI([FromHeader] string id)
        {
            // ---- 1) Get UserId from header ----
            if (!Request.Headers.TryGetValue("UserId", out var userIdHeader))
                return Json(new { success = false, message = "Missing UserId header" });

            int userId = int.Parse(userIdHeader);

            if (string.IsNullOrWhiteSpace(id))
                return Json(new { success = false, message = "Ticket ID is required" });

            // ---- 2) Load ticket info ----
            var ticketResult = await _context.ICC_GET_TICKET_TrackingListV2Results
                .FromSqlInterpolated($"EXEC ICC_GET_TICKET_ByIDV2 @ID = {id}")
                .ToListAsync();

            var ticket = ticketResult.FirstOrDefault();
            if (ticket == null) return Json(new { success = false, message = "Ticket not found" });

            // ---- 3) Load assignment header ----
            var headerResult = await _context.TicketAssignHeaderV2Result
                .FromSqlInterpolated($"EXEC ICC_GET_TICKET_AssignHeaderV2 @ID = {id}")
                .ToListAsync();

            var header = headerResult.FirstOrDefault();
            if (header == null) return Json(new { success = false, message = "Assignment header not found" });

            // ---- 4) Load assignment rows ----
            var rows = await _context.TicketAssignRowV2Result
                .FromSqlInterpolated($"EXEC ICC_GET_TICKET_AssignRowV2 @ID = {header.Id}")
                .ToListAsync();

            rows ??= new List<TicketAssignRowV2>();

            // ---- 5) Load staff lists ----
            var staffList = await _context.StaffListAssignResult
                .FromSqlInterpolated($"EXEC ICC_GET_StaffByAssignCompanyV2 @Branch = {ticket.BranchID}, @ID = {id}")
                .ToListAsync();

            var supporterList = await _context.StaffListAssignResult
                .FromSqlInterpolated($"EXEC ICC_GET_StaffAllV2")
                .ToListAsync();

            // ---- 6) Return JSON ----
            return Ok(new
            {
                success = true,
                ticket,
                header,
                rows,
                staffList,
                supporterList,
                currentUser = userId
            });
        }


        [HttpPost("api/SaveEditTicketAssign")]
        public async Task<IActionResult> SaveEditTicketAssignAPI([FromBody] TicketAssignHeader model)
        {
            // ---- 1) Get UserId from header ----
            if (!Request.Headers.TryGetValue("UserId", out var userIdHeader))
                return Json(new { success = false, message = "Missing UserId header" });

            int userId = int.Parse(userIdHeader);

            // ---- 2) Prepare model ----
            model.Mode ??= "Update";
            model.UpdateBy = userId.ToString();

            if (model.CreatedDate.HasValue)
            {
                model.CreatedDate = DateTime.Parse(model.CreatedDate.Value
                    .ToUniversalTime()
                    .ToString("yyyy-MM-ddTHH:mm:ss"));
            }
            else
            {
                model.CreatedDate = DateTime.Now.ToUniversalTime();
            }

            model.UpdatedDate = DateTime.Parse(DateTime.Now
                .ToUniversalTime()
                .ToString("yyyy-MM-ddTHH:mm:ss"));

            model.Status ??= "Request";
            model.Progress ??= "Pending";

            // ---- 3) Ensure RowList dates ----
            if (model.RowList != null)
            {
                foreach (var row in model.RowList)
                {
                    row.ProgessStatus ??= "Start";

                    row.AssignDate ??= DateTime.Now;
                    row.AssignDate = DateTime.Parse(row.AssignDate.Value
                        .ToUniversalTime()
                        .ToString("yyyy-MM-ddTHH:mm:ss"));

                    row.DeadlineDate ??= DateTime.Now;
                    row.DeadlineDate = DateTime.Parse(row.DeadlineDate.Value
                        .ToUniversalTime()
                        .ToString("yyyy-MM-ddTHH:mm:ss"));

                    row.TransferDate = DateTime.Parse(DateTime.Now
                        .ToUniversalTime()
                        .ToString("yyyy-MM-ddTHH:mm:ss"));
                }
            }

            var jsonString = JsonConvert.SerializeObject(model);

            try
            {
                // ---- 4) Execute Stored Procedure ----
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw(
                        "EXEC dbo.ICC_TaskAssigment @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "Task"),
                        new SqlParameter("@Trantype", model.Mode == "Add" ? "A" : "U"),
                        new SqlParameter("@EntryPrimary", model.Id.HasValue ? model.Id.Value.ToString() : "0"),
                        new SqlParameter("@JsonBody", jsonString)
                    )
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();

                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });

                return Json(new { success = false, message = result?.Message ?? "Error saving assignment." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while saving assignment." });
            }
        }


        [HttpGet("api/TicketAssignHistoryAPI")]
        public async Task<IActionResult> TicketAssignHistoryAPI([FromHeader] string TicketId)
        {
            // ---- 1) Get UserId from header ----
            if (!Request.Headers.TryGetValue("UserId", out var userIdHeader))
                return Json(new { success = false, message = "Missing UserId header" });

            int userId = int.Parse(userIdHeader);

            if (string.IsNullOrWhiteSpace(TicketId))
                return Json(new { success = false, message = "Ticket ID is required" });

            // ---- 2) Load chat history ----
            var chatResult = await _context.TicketChatHistoryModeratorResult
                .FromSqlInterpolated($@"
            EXEC ICC_GET_TICKETChatHistory_Moderator 
                @User = {userId}, 
                @EntryPrimary = {TicketId}, 
                @JsonBody = ''")
                .ToListAsync();

            if (chatResult == null) chatResult = new List<TicketChatHistoryModerator>();

            // ---- 3) Load ticket info ----
            var ticketResult = await _context.ICC_GET_TICKET_TrackingListV2Results
                .FromSqlInterpolated($"EXEC ICC_GET_TICKET_ByIDV2 @ID = {TicketId}")
                .ToListAsync();

            var ticket = ticketResult.FirstOrDefault();
            if (ticket == null) return Json(new { success = false, message = "Ticket not found" });

            // ---- 4) Return JSON ----
            return Ok(new
            {
                success = true,
                ticket,
                chatHistory = chatResult,
                currentUserId = userId
            });
        }


        [HttpPost("api/CommentTicketAssign")]
        public async Task<IActionResult> CommentTicketAssignAPI(
            [FromHeader] string TicketRef,
            [FromHeader] string FeedbackText)
        {
            // ---- 1) Get UserId from header ----
            if (!Request.Headers.TryGetValue("UserId", out var userIdHeader))
                return Json(new { success = false, message = "Missing UserId header" });

            int userId = int.Parse(userIdHeader);

            if (string.IsNullOrWhiteSpace(TicketRef))
                return Json(new { success = false, message = "TicketRef is required" });

            if (string.IsNullOrWhiteSpace(FeedbackText))
                return Json(new { success = false, message = "FeedbackText is required" });

            // ---- 2) Build JSON body for SP ----
            var jsonBody = new
            {
                SecretCode = "Tga5vz5oWiClgzayaLMZAx7KoTGLSJGrCQkY01gNb0c=",
                FeedbackId = -1,
                TicketRef = TicketRef,
                Rating = 0,
                FeedbackText = FeedbackText,
                Type = "TickModeratorComment",
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                CreateBy = userId.ToString(),
                UpdateBy = userId.ToString()
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                // ---- 3) Call stored procedure ----
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Feedback @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@Trantype", "A"),
                        new SqlParameter("@EntryPrimary", TicketRef ?? (object)DBNull.Value),
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();

                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });

                return Json(new { success = false, message = result?.Message ?? "Error adding comment." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while adding comment." });
            }
        }



        [HttpGet("api/GetRPTTicketTrackingByStaff")]
        public async Task<IActionResult> GetRPTTicketTrackingByStaffAPI(
            [FromHeader] string? FromDate,
            [FromHeader] string? ToDate)
        {
            try
            {
                // -------------------- 1) Parse Dates --------------------
                var startDate = !string.IsNullOrEmpty(FromDate)
                    ? DateTime.ParseExact(FromDate, "dd-MMM-yyyy", CultureInfo.InvariantCulture)
                    : new DateTime(DateTime.Now.Year, 1, 1);

                var endDate = !string.IsNullOrEmpty(ToDate)
                    ? DateTime.ParseExact(ToDate, "dd-MMM-yyyy", CultureInfo.InvariantCulture)
                    : DateTime.Today;

                // -------------------- 2) Get Summary Data --------------------
                var summaryList = await _context.Set<TaskTrackingByStaffSummaryDto>()
                    .FromSqlInterpolated($@"
                EXEC ICC_RPT_TaskTracking_ByStaff_Summary 
                    @User = -1,
                    @FromDate = {startDate},
                    @ToDate = {endDate}")
                    .AsNoTracking()
                    .ToListAsync();

                if (summaryList == null || !summaryList.Any())
                    summaryList = new List<TaskTrackingByStaffSummaryDto>();

                // -------------------- 3) Get Staff & Branch Lists --------------------
                var supporterList = await _context.StaffListAssignResult
                    .FromSqlInterpolated($"EXEC ICC_GET_StaffAllV2")
                    .ToListAsync();

                var branchList = await _context.BranchMainDtos
                    .FromSqlInterpolated($"EXEC ICC_GET_Branch_Main")
                    .ToListAsync();

                // -------------------- 4) Return JSON --------------------
                return Json(new
                {
                    success = true,
                    message = "Report loaded successfully",
                    fromDate = startDate.ToString("dd-MMM-yyyy"),
                    toDate = endDate.ToString("dd-MMM-yyyy"),
                    staff = supporterList,
                    branch = branchList,
                    data = summaryList
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Unexpected error occurred while loading the report.",
                    error = ex.Message
                });
            }
        }




        // Model class for JSON input
        public class RejectTicketModel
        {
            public string TicketId { get; set; }
            public string Reason { get; set; }
        }


    }
}
