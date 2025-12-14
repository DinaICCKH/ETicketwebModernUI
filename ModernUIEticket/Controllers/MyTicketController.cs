using ETicketNewUI.Models;
using Microsoft.AspNetCore.Http.HttpResults;
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
    public class MyTicketController : Controller
    {
        private readonly TicketDbContext _context;

        public MyTicketController(TicketDbContext context)
        {
            _context = context;
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
        #endregion

        public async Task<IActionResult> Index(
          string ProgressStatus = "Start",
          string? EntryPrimary = "ALL",
          string priority = "ALL",
          string branchCompany = "ALL",
          string createBy = "ALL",
          string Status = "ALL")
        {
            var token = GetTokenData();
            if (token == null) return RedirectToAction("Login", "Logout");

            // ------------------- 1) Ticket Tracking List -------------------
            var tickets = await _context.MyTicketDtoResult
                .FromSqlInterpolated($@" EXEC ICC_GET_TICKET_MYTicketV2
                    @User = {token.UserId},
                    @ProgressStatus = {ProgressStatus},
                    @Priority = {priority},
                    @BranchCompany = {branchCompany},
                    @CreateBy = {createBy},
                    @EntryPrimary = {EntryPrimary},
                    @Status = {Status}"
                )
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

        [HttpGet]
        public async Task<IActionResult> SaveAcceptTicket(string id)
        {
            var token = GetTokenData();
            if (token == null) return RedirectToAction("Login", "Logout");

            // Load ticket info
            var ticketResult = await _context.ICC_GET_TICKET_TrackingListV2Results
             .FromSqlInterpolated($"EXEC ICC_GET_TICKET_ByIDV2 @ID = {id}")
             .ToListAsync();
            var ticket = ticketResult.FirstOrDefault();  // take the first item

            if (ticket == null) return NotFound();

            var Assign = await _context.MyTicketDtoResult
               .FromSqlInterpolated($@" EXEC ICC_GET_TICKET_MYTicketV2
                    @User = {token.UserId},
                    @ProgressStatus = 'ALL',
                    @Priority = 'ALL',
                    @BranchCompany = 'ALL',
                    @CreateBy = 'ALL',
                    @EntryPrimary = {id},
                    @Status = 'ALL'"
               )
               .ToListAsync();
            var assign = Assign.FirstOrDefault();  // take the first item

            ViewBag.TicketAssign = assign;
            return View(ticket);
        }

        [HttpPost]

        public async Task<IActionResult> UpdateTicketStatus(string TicketId, string Comment)
        {
            var token = GetTokenData();
            if (token == null)
                return Json(new { success = false, message = "Unauthorized" });

            if (string.IsNullOrWhiteSpace(Comment))
                return Json(new { success = false, message = "Comment is required" });

            try
            {
                var spResults = await _context.Set<SpResult>()
                .FromSqlInterpolated($@"EXEC dbo.ICC_UPDATE_MY_TICKET_STATUS 
                    @User={token.UserId}, 
                    @EntryPrimary={TicketId}, 
                    @Status={"P"}, 
                    @Reason={Comment}")
                .AsNoTracking()
                .ToListAsync();


                var result = spResults.FirstOrDefault();

                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });

                return Json(new { success = false, message = result?.Message ?? "Error updating ticket." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex);
                return Json(new { success = false, message = "Unexpected error occurred while updating ticket." });
            }
        }


        public async Task<IActionResult> MyTicketTracking(
            string ProgressStatus = "Progress",
            string? EntryPrimary = "ALL",
            string priority = "ALL",
            string branchCompany = "ALL",
            string createBy = "ALL",
            string? status = "Progress"
            )
        {
            var token = GetTokenData();
            if (token == null) return RedirectToAction("Login", "Logout");

            // ------------------- 1) Ticket Tracking List -------------------
            var tickets = await _context.MyTicketDtoResult
                .FromSqlInterpolated($@" EXEC ICC_GET_TICKET_MYTicketV2
                    @User = {token.UserId},
                    @ProgressStatus = {ProgressStatus},
                    @Priority = {priority},
                    @BranchCompany = {branchCompany},
                    @CreateBy = {createBy},
                    @EntryPrimary = {EntryPrimary},
                    @Status = {status}"
                )
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

        [HttpGet]
        public async Task<IActionResult> SaveCompleteTicket(string id)
        {
            var token = GetTokenData();
            if (token == null) return RedirectToAction("Login", "Logout");

            // Load ticket info
            var ticketResult = await _context.ICC_GET_TICKET_TrackingListV2Results
             .FromSqlInterpolated($"EXEC ICC_GET_TICKET_ByIDV2 @ID = {id}")
             .ToListAsync();
            var ticket = ticketResult.FirstOrDefault();  // take the first item

            if (ticket == null) return NotFound();

            var Assign = await _context.MyTicketDtoResult
               .FromSqlInterpolated($@" EXEC ICC_GET_TICKET_MYTicketV2
                    @User = {token.UserId},
                    @ProgressStatus = 'ALL',
                    @Priority = 'ALL',
                    @BranchCompany = 'ALL',
                    @CreateBy = 'ALL',
                    @EntryPrimary = {id},
                    @Status = 'ALL'"
               )
               .ToListAsync();
            var assign = Assign.FirstOrDefault();  // take the first item


            ViewBag.TicketAssign = assign;
            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCheckingTicket(Ticket1 model, IFormFile Attachment)
        {
            // Decode token
            var tokenData = GetTokenData();
            string entryPrimary = tokenData?.UserId.ToString() ?? "";
            model.CreateBy ??= entryPrimary;
            model.Mode = "Add";
            model.Status = "Checking";
            model.Remark = model.Comment;

            // Handle file upload
            if (Attachment != null && Attachment.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".xlsx", ".xls", ".csv", ".pdf" };
                var fileExt = Path.GetExtension(Attachment.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExt))
                {
                    return Json(new { success = false, message = "Invalid file type. Allowed: images or Excel/PDF files." });
                }

                if (Attachment.Length > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "File size exceeds 5MB limit." });
                }

                var sanitizedFileName = System.Text.RegularExpressions.Regex.Replace(
                    Path.GetFileNameWithoutExtension(Attachment.FileName),
                    @"[^a-zA-Z0-9_-]", "_");

                var newFileName = $"{DateTime.Now:dd_MM_yyyy_HH_mm_ss}_{sanitizedFileName}{fileExt}";

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "UploadedFiles");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, newFileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await Attachment.CopyToAsync(stream);

                model.Attachment = "/UploadedFiles/" + newFileName;
              
            }

            // Build JSON body for ICC_Ticket1
            var jsonBody = new
            {
                model.SecretCode,
                model.Mode,
                model.AssignID,
                model.AssignIDLine,
                model.RefID,
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                model.CreateBy,
                model.Status,
                model.Remark,
                model.Attachment,
                model.Hide
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Ticket1 @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "Ticket1"),
                        new SqlParameter("@Trantype", "A"),
                        new SqlParameter("@EntryPrimary", entryPrimary ?? (object)DBNull.Value),
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
                    return Json(new { success = false, message = result?.Message ?? "Error creating Ticket1." });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while creating Ticket1." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> TicketHistory(string id)
        {
            var token = GetTokenData();
            if (token == null) return RedirectToAction("Login", "Logout");

            // ------------------- Load chat history -------------------
            var chatResult = await _context.TicketChatHistoryModeratorResult
                .FromSqlInterpolated($@" EXEC ICC_GET_TICKETChatHistory 
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
                Type = "TickAssignDevComment",
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




        [HttpGet]
        public async Task<IActionResult> RPTMyTicket(string? fromDatesummary, string? toDatesummary)
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
                     @User={token.Email}, 
                     @FromDate={startDate}, 
                     @ToDate={endDate}")
                    .AsNoTracking()
                    .ToListAsync();

                if (summaryList == null || !summaryList.Any())
                    summaryList = new List<TaskTrackingByStaffSummaryDto>();

                var supporterList = await _context.StaffListAssignResult
                    .FromSqlInterpolated($"EXEC ICC_GET_StaffAllV2")
                    .ToListAsync();

                var branchList = await _context.BranchMainDtos
                    .FromSqlInterpolated($"EXEC ICC_GET_Branch_Main")
                    .ToListAsync();

                ViewBag.CurrentUser = token.Email;
                ViewBag.Branch = branchList;
                ViewBag.Staff = supporterList;
                ViewBag.FromDate = startDate.ToString("dd-MMM-yyyy");
                ViewBag.ToDate = endDate.ToString("dd-MMM-yyyy");

                return View("RPTMyTicket", summaryList);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("Error");
            }
        }



        public async Task<IActionResult> AcceptTaskSupporter(
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


        [HttpGet]
        public async Task<IActionResult> SaveTicketAssignBySupporter(int id)
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
              .FromSqlInterpolated($"EXEC ICC_GET_StaffByAssignCompanyV2Supporter @Branch = {ticket.BranchID},@ID={id},@UserID={token.UserId}")
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
        public async Task<IActionResult> SaveTicketAssignBySupporter([FromBody] TicketAssignHeader model)
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

        //API 

        [HttpGet("api/MyTicket")]
        public async Task<IActionResult> MyTicketAPI()
        {
            // ----- Read parameters from request header -----
            string ProgressStatus = Request.Headers["ProgressStatus"].ToString() ?? "Start";
            string EntryPrimary = Request.Headers["EntryPrimary"].ToString() ?? "ALL";
            string priority = Request.Headers["priority"].ToString() ?? "ALL";
            string branchCompany = Request.Headers["branchCompany"].ToString() ?? "ALL";
            string createBy = Request.Headers["createBy"].ToString() ?? "ALL";
            string Status = Request.Headers["Status"].ToString() ?? "ALL";
            string UserId = Request.Headers["UserId"].ToString() ?? "-1";

            // ------------------- 1) Ticket Tracking List -------------------
            var tickets = await _context.MyTicketDtoResult
                .FromSqlInterpolated($@" EXEC ICC_GET_TICKET_MYTicketV2
            @User = {UserId},
            @ProgressStatus = {ProgressStatus},
            @Priority = {priority},
            @BranchCompany = {branchCompany},
            @CreateBy = {createBy},
            @EntryPrimary = {EntryPrimary},
            @Status = {Status}"
                )
                .ToListAsync();

            return Ok(tickets);   // ✔ return JSON
        }

        [HttpGet("api/GetAcceptTicketByID")]
        public async Task<IActionResult> AcceptTicketByID(string id)
        {
            // ----- Read UserId from header -----
            string userId = Request.Headers["UserId"].ToString();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Missing UserId header" });

            // ---- Load ticket info ----
            var ticketResult = await _context.ICC_GET_TICKET_TrackingListV2Results
                .FromSqlInterpolated($"EXEC ICC_GET_TICKET_ByIDV2 @ID = {id}")
                .ToListAsync();

            var ticket = ticketResult.FirstOrDefault();
            if (ticket == null)
                return NotFound(new { message = "Ticket not found" });

            // ---- Load assign info ----
            var assignList = await _context.MyTicketDtoResult
                .FromSqlInterpolated($@" EXEC ICC_GET_TICKET_MYTicketV2
                @User = {userId},
                @ProgressStatus = 'ALL',
                @Priority = 'ALL',
                @BranchCompany = 'ALL',
                @CreateBy = 'ALL',
                @EntryPrimary = {id},
                @Status = 'ALL'"
                )
                .ToListAsync();

            var assign = assignList.FirstOrDefault();

            // ---- Return JSON ----
            return Ok(new
            {
                success = true,
                message = "Success",
                Ticket = ticket,
                Assign = assign
            });
        }


        [HttpPost("api/SaveAcceptTicket")]
        public async Task<IActionResult> SaveAcceptTicketAPI()
        {
            // ----- Read values from headers -----
            string userId = Request.Headers["UserId"].ToString();
            string ticketId = Request.Headers["TicketId"].ToString();
            string comment = Request.Headers["Comment"].ToString();

            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Missing UserId header" });

            if (string.IsNullOrEmpty(ticketId))
                return Json(new { success = false, message = "Missing TicketId header" });

            if (string.IsNullOrWhiteSpace(comment))
                return Json(new { success = false, message = "Comment header is required" });

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlInterpolated($@"EXEC dbo.ICC_UPDATE_MY_TICKET_STATUS 
                @User = {userId}, 
                @EntryPrimary = {ticketId}, 
                @Status = {"P"}, 
                @Reason = {comment}")
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();

                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });

                return Json(new { success = false, message = result?.Message ?? "Error updating ticket." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex);
                return Json(new { success = false, message = "Unexpected error occurred while updating ticket." });
            }
        }


        [HttpGet("api/MyTicketTracking")]
        public async Task<IActionResult> MyTicketTrackingAPI()
        {
            // ----- Read parameters from request headers -----
            string userId = Request.Headers["UserId"].ToString() ?? "-1";
            string progressStatus = Request.Headers["ProgressStatus"].ToString() ?? "Progress";
            string entryPrimary = Request.Headers["EntryPrimary"].ToString() ?? "ALL";
            string priority = Request.Headers["priority"].ToString() ?? "ALL";
            string branchCompany = Request.Headers["branchCompany"].ToString() ?? "ALL";
            string createBy = Request.Headers["createBy"].ToString() ?? "ALL";
            string status = Request.Headers["status"].ToString() ?? "Progress";

            // --- Validate ---
            if (userId == "-1")
                return Json(new { success = false, message = "Missing UserId header" });

            // ------------------- 1) Ticket Tracking List -------------------
            var tickets = await _context.MyTicketDtoResult
                .FromSqlInterpolated($@" EXEC ICC_GET_TICKET_MYTicketV2
            @User = {userId},
            @ProgressStatus = {progressStatus},
            @Priority = {priority},
            @BranchCompany = {branchCompany},
            @CreateBy = {createBy},
            @EntryPrimary = {entryPrimary},
            @Status = {status}"
                )
                .ToListAsync();

            // ------------------- 2) Report Tracking Parameters -------------------
            var branchList = await _context.ReportTrackingResults
                .FromSqlInterpolated($"EXEC ICC_ReportTracking_ParameterV2 @Type = {"Branch"}")
                .ToListAsync();

            var createByList = await _context.ReportTrackingResults
                .FromSqlInterpolated($"EXEC ICC_ReportTracking_ParameterV2 @Type = {"CreateBy"}")
                .ToListAsync();

            // ------------------- Return JSON (API Response) -------------------
            return Json(new
            {
                success = true,
                tickets,
                branchList,
                createByList
            });
        }

        [HttpGet("api/GetCompleteTicketByID")]
        public async Task<IActionResult> GetCompleteTicketByID()
        {
            // ============================
            // Read from Request Headers
            // ============================
            string userId = Request.Headers["UserId"].ToString();
            string id = Request.Headers["ID"].ToString();

            if (string.IsNullOrWhiteSpace(userId))
                return Json(new { success = false, message = "UserId header is required." });

            if (string.IsNullOrWhiteSpace(id))
                return Json(new { success = false, message = "ID header is required." });

            // ============================
            // 1) Load ticket info
            // ============================
            var ticketResult = await _context.ICC_GET_TICKET_TrackingListV2Results
                .FromSqlInterpolated($"EXEC ICC_GET_TICKET_ByIDV2 @ID = {id}")
                .ToListAsync();

            var ticket = ticketResult.FirstOrDefault();
            if (ticket == null)
                return Json(new { success = false, message = "Ticket not found." });

            // ============================
            // 2) Load assign info
            // ============================
            var Assign = await _context.MyTicketDtoResult
                .FromSqlInterpolated($@" EXEC ICC_GET_TICKET_MYTicketV2
             @User = {userId},
             @ProgressStatus = 'ALL',
             @Priority = 'ALL',
             @BranchCompany = 'ALL',
             @CreateBy = 'ALL',
             @EntryPrimary = {id},
             @Status = 'ALL' "
                )
                .ToListAsync();

            var assign = Assign.FirstOrDefault();

            // ============================
            // 3) Return JSON result
            // ============================
            return Json(new
            {
                success = true,
                message = "Success",
                ticket = ticket,
                assign = assign
            });
        }


        [HttpPost("api/SaveCompleteTicket")]
        public async Task<IActionResult> SaveCompleteTicketAPI([FromForm] Ticket1 model, [FromForm] IFormFile Attachment)
        {
            // ------------------- 1) Prepare model -------------------
            model.Mode = "Add";
            model.Status = "Complete";
            model.Remark = model.Comment; // Copy Comment to Remark

            // ------------------- 2) Handle file upload -------------------
            if (Attachment != null && Attachment.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".xlsx", ".xls", ".csv", ".pdf" };
                var fileExt = Path.GetExtension(Attachment.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExt))
                    return BadRequest(new { success = false, message = "Invalid file type. Allowed: images or Excel/PDF files." });

                if (Attachment.Length > 5 * 1024 * 1024)
                    return BadRequest(new { success = false, message = "File size exceeds 5MB limit." });

                var sanitizedFileName = System.Text.RegularExpressions.Regex.Replace(
                    Path.GetFileNameWithoutExtension(Attachment.FileName),
                    @"[^a-zA-Z0-9_-]", "_");

                var newFileName = $"{DateTime.Now:dd_MM_yyyy_HH_mm_ss}_{sanitizedFileName}{fileExt}";

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "UploadedFiles");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, newFileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await Attachment.CopyToAsync(stream);

                model.Attachment = "/UploadedFiles/" + newFileName;
            }

            // ------------------- 3) Build JSON body for SP -------------------
            var jsonBody = new
            {
                model.SecretCode,
                model.Mode,
                model.AssignID,
                model.AssignIDLine,
                model.RefID,
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                model.CreateBy,
                model.Status,
                model.Remark,
                model.Attachment,
                model.Hide
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            // ------------------- 4) Call stored procedure -------------------
            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Ticket1 @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "Ticket1"),
                        new SqlParameter("@Trantype", "A"),
                        new SqlParameter("@EntryPrimary", model.CreateBy ?? (object)DBNull.Value),
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();
                if (result != null && result.Code == 200)
                    return Ok(new { success = true, message = result.Message });

                return BadRequest(new { success = false, message = result?.Message ?? "Error creating Ticket1." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return StatusCode(500, new { success = false, message = "Unexpected error occurred while creating Ticket1." });
            }
        }


        [HttpGet("api/MyTicketHistory")]
        public async Task<IActionResult> MyTicketHistoryAPI()
        {
            // ------------------- Read parameters from request headers -------------------
            string userId = Request.Headers["UserId"].ToString();
            string ticketId = Request.Headers["TicketId"].ToString();

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(ticketId))
                return BadRequest(new { success = false, message = "UserId and TicketId headers are required." });

            // ------------------- Load chat history -------------------
            var chatResult = await _context.TicketChatHistoryModeratorResult
                .FromSqlInterpolated($@"
            EXEC ICC_GET_TICKETChatHistory 
                @User = {userId}, 
                @EntryPrimary = {ticketId}, 
                @JsonBody = ''")
                .ToListAsync();

            if (chatResult == null || !chatResult.Any())
                return NotFound(new { success = false, message = "No chat history found." });

            // ------------------- Load ticket info -------------------
            var ticketResult = await _context.ICC_GET_TICKET_TrackingListV2Results
                .FromSqlInterpolated($@"EXEC ICC_GET_TICKET_ByIDV2 @ID = {ticketId}")
                .ToListAsync();

            var ticket = ticketResult.FirstOrDefault();
            if (ticket == null)
                return NotFound(new { success = false, message = "Ticket not found." });

            // ------------------- Return combined result -------------------
            return Ok(new
            {
                success = true,
                Ticket = ticket,
                ChatHistory = chatResult
            });
        }


        [HttpPost("api/CommentMyTicket")]
        public async Task<IActionResult> CommentTicketAssignAPI()
        {
            // ------------------- Read parameters from request headers -------------------
            string userId = Request.Headers["UserId"].ToString();
            string ticketRef = Request.Headers["TicketRef"].ToString();
            string feedbackText = Request.Headers["FeedbackText"].ToString();

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(ticketRef) || string.IsNullOrWhiteSpace(feedbackText))
                return BadRequest(new { success = false, message = "UserId, TicketRef and FeedbackText headers are required." });

            // ------------------- Build JSON body for SP -------------------
            var jsonBody = new
            {
                SecretCode = "Tga5vz5oWiClgzayaLMZAx7KoTGLSJGrCQkY01gNb0c=",
                FeedbackId = -1,
                TicketRef = ticketRef,
                Rating = 0,
                FeedbackText = feedbackText,
                Type = "TickAssignDevComment",
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                CreateBy = userId,
                UpdateBy = userId
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                // ------------------- Call stored procedure -------------------
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Feedback @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@Trantype", "A"),
                        new SqlParameter("@EntryPrimary", ticketRef ?? (object)DBNull.Value),
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();

                if (result != null && result.Code == 200)
                    return Ok(new { success = true, message = result.Message });

                return BadRequest(new { success = false, message = result?.Message ?? "Error adding comment." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return StatusCode(500, new { success = false, message = "Unexpected error occurred while adding comment." });
            }
        }


        [HttpGet("api/RPTMyTicket")]
        public async Task<IActionResult> RPTMyTicketAPI()
        {
            try
            {
                // ------------------- 1) Read headers -------------------
                string user = Request.Headers["UserId"].ToString() ?? "ALL";
                string fromDatesummary = Request.Headers["FromDate"].ToString();
                string toDatesummary = Request.Headers["ToDate"].ToString();

                var startDate = !string.IsNullOrEmpty(fromDatesummary)
                    ? DateTime.ParseExact(NormalizeDate(fromDatesummary), "dd-MMM-yyyy", CultureInfo.InvariantCulture)
                    : new DateTime(DateTime.Now.Year, 1, 1);

                var endDate = !string.IsNullOrEmpty(toDatesummary)
                    ? DateTime.ParseExact(NormalizeDate(toDatesummary), "dd-MMM-yyyy", CultureInfo.InvariantCulture)
                    : DateTime.Today;

                // ------------------- 2) Fetch summary -------------------
                var summaryList = await _context.Set<TaskTrackingByStaffSummaryDto>()
                    .FromSqlInterpolated($@"
                EXEC ICC_RPT_TaskTracking_ByStaff_Summary 
                     @User={user}, 
                     @FromDate={startDate}, 
                     @ToDate={endDate}")
                    .AsNoTracking()
                    .ToListAsync();

                if (summaryList == null || !summaryList.Any())
                    summaryList = new List<TaskTrackingByStaffSummaryDto>();

                // ------------------- 3) Fetch supporter & branch list -------------------
                var supporterList = await _context.StaffListAssignResult
                    .FromSqlInterpolated($"EXEC ICC_GET_StaffAllV2")
                    .ToListAsync();

                var branchList = await _context.BranchMainDtos
                    .FromSqlInterpolated($"EXEC ICC_GET_Branch_Main")
                    .ToListAsync();

                // ------------------- 4) Return as JSON -------------------
                return Ok(new
                {
                    CurrentUser = user,
                    Branch = branchList,
                    Staff = supporterList,
                    FromDate = startDate.ToString("dd-MMM-yyyy"),
                    ToDate = endDate.ToString("dd-MMM-yyyy"),
                    SummaryList = summaryList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

    }
}
