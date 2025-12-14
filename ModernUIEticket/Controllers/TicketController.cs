using ETicketNewUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.ReportingServices.ReportProcessing.ReportObjectModel;
using System.Text.Json.Nodes;

namespace ETicketNewUI.Controllers
{
    public class TicketController : Controller
    {
        private readonly TicketDbContext _context;

        public TicketController(TicketDbContext context)
        {
            _context = context;
        }

        // GET: AddTicket
        public async Task<IActionResult> AddTicket()
        {
            var tokenData = GetTokenData();
            bool isClientType = tokenData?.Type == "Client";
            ViewBag.IsClientType = isClientType;
            ViewBag.BranchList = await GetBranchListAsync(tokenData);
            ViewBag.BranchClientList = await GetBranchClientListAsync(tokenData);
            var attributeList = await GetAttributeRulesAsync(tokenData);
            ViewBag.Modules = attributeList.Where(a => a.AType == "Module").ToList();
            ViewBag.Priorities = attributeList.Where(a => a.AType == "Priority").ToList();
            ViewBag.ProblemTypes = attributeList.Where(a => a.AType == "ProblemType").ToList();
            ViewBag.SubProblemTypes = attributeList.Where(a => a.AType == "SubProblemType").ToList();
            return View();
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
                    new SqlParameter("@EntryPrimary", "-1"),
                    new SqlParameter("@JsonBody", ""),
                    new SqlParameter("@Type", "ALL"))
                .AsNoTracking()
                .ToListAsync();
        }
        #endregion

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ticket model, IFormFile Attachment)
        {
            // Decode token
            var tokenData = GetTokenData();
            string entryPrimary = tokenData?.UserId.ToString() ?? "";
            model.CreateBy ??= entryPrimary;

            // Handle file upload
            if (Attachment != null && Attachment.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".xlsx", ".xls", ".csv", ".pdf" };
                var fileExt = Path.GetExtension(Attachment.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExt))
                {
                    TempData["ErrorMessage"] = "Invalid file type. Allowed: images or Excel/PDF files.";
                    return View("AddTicket", model);
                }

                if (Attachment.Length > 5 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "File size exceeds 5MB limit.";
                    return View("AddTicket", model);
                }

                var sanitizedFileName = System.Text.RegularExpressions.Regex.Replace(
                    Path.GetFileNameWithoutExtension(Attachment.FileName),
                    @"[^a-zA-Z0-9_-]", "_");

                var newFileName = $"{model.Branch}_{DateTime.Now:dd_MM_yyyy_HH_mm_ss}_{sanitizedFileName}{fileExt}";

                // Set uploads folder under wwwroot
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "UploadedFiles");

                // Create folder if it doesn't exist
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, newFileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await Attachment.CopyToAsync(stream);

                // Save the relative path to the database
                model.Attachment = "/UploadedFiles/" + newFileName;
            }

            // Build JSON body for SP
            var jsonBody = new
            {
                SecretCode = "Tga5vz5oWiClgzayaLMZAx7KoTGLSJGrCQkY01gNb0c=",
                Mode = "Add",
                TicketId = -1,
                RefID = (string?)null,
                Title = model.Title,
                Description = model.Description,
                Status = "Request",
                Priority = model.Priority,
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                DeadlineDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                CloseDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                CreateBy = model.CreateBy ?? entryPrimary,
                UpdateBy = (string?)null,
                Attachment = model.Attachment,
                ProblemType = model.ProblemType,
                Module = model.Module,
                Branch = model.Branch,
                SubProblemType = model.SubProblemType,
                Project = model.Project
            };

            // **Use Newtonsoft.Json explicitly**
            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Ticket @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "Ticket"),
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
                    return Json(new { success = false, message = result?.Message ?? "Error creating ticket." });
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                TempData["ErrorMessage"] = "Unexpected error occurred while creating ticket.";
                return View("AddTicket", model);
            }
        }


        // GET: EditTicket/5
        public async Task<IActionResult> EditTicket(string id)
        {
            var tokenData = GetTokenData();
            bool isClientType = tokenData?.Type == "Client";
            ViewBag.IsClientType = isClientType;

            // Load dropdowns
            ViewBag.BranchList = await GetBranchListAsync(tokenData);
            ViewBag.BranchClientList = await GetBranchClientListAsync(tokenData);
            var attributeList = await GetAttributeRulesAsync(tokenData);
            ViewBag.Modules = attributeList.Where(a => a.AType == "Module").ToList();
            ViewBag.Priorities = attributeList.Where(a => a.AType == "Priority").ToList();
            ViewBag.ProblemTypes = attributeList.Where(a => a.AType == "ProblemType").ToList();
            ViewBag.SubProblemTypes = attributeList.Where(a => a.AType == "SubProblemType").ToList();
            // Load ticket info
            var ticketResult = await _context.Tickets
             .FromSqlInterpolated($"EXEC ICC_GET_TICKETV2 @ID = {id}")
             .ToListAsync();

            var ticket = ticketResult.FirstOrDefault();  // take the first item

            if (ticket == null) return NotFound();

            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTicket(Ticket model, IFormFile Attachment)
        {
            var tokenData = GetTokenData();
            string entryPrimary = tokenData?.UserId.ToString() ?? "";
            model.UpdateBy = entryPrimary;

            // Handle new file upload
            if (Attachment != null && Attachment.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".xlsx", ".xls", ".csv", ".pdf" };
                var fileExt = Path.GetExtension(Attachment.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExt))
                {
                    return Json(new { success = false, message = "Invalid file type." });
                }

                if (Attachment.Length > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "File size exceeds 5MB." });
                }

                var sanitizedFileName = System.Text.RegularExpressions.Regex.Replace(
                Path.GetFileNameWithoutExtension(Attachment.FileName),
                @"[^a-zA-Z0-9_-]", "_");

                var newFileName = $"{model.Branch}_{DateTime.Now:dd_MM_yyyy_HH_mm_ss}_{sanitizedFileName}{fileExt}";

                // Use one consistent folder
                var folderName = "Attachments";
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folderName);

                // Create if not exist
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Save file
                var filePath = Path.Combine(uploadsFolder, newFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Attachment.CopyToAsync(stream);
                }

                // Store relative path correctly
                model.Attachment = $"/{folderName}/{newFileName}";

            }

            // Build JSON body for SP
            var jsonBody = new
            {
                SecretCode = "Tga5vz5oWiClgzayaLMZAx7KoTGLSJGrCQkY01gNb0c=",
                Mode = "Update",
                TicketId = model.TicketId,
                RefID = model.RefID,
                Title = model.Title,
                Description = model.Description,
                Status = model.Status ?? "Request",
                Priority = model.Priority,
                UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdateBy = model.UpdateBy,
                Attachment = model.Attachment,
                ProblemType = model.ProblemType,
                Module = model.Module,
                Branch = model.Branch,
                SubProblemType = model.SubProblemType,
                Project = model.Project
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Ticket @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "Ticket"),
                        new SqlParameter("@Trantype", "E"),
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
                    return Json(new { success = false, message = result?.Message ?? "Error updating ticket." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Unexpected error: " + ex.Message });
            }
        }



        public async Task<IActionResult> TicketTracking(
            string status = "Request,Pending,Draf,Checking,Progress",
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string priority = "ALL",
            string branchCompany = "ALL",
            string createBy = "ALL")
        {
            var token = GetTokenData();
            if (token == null) return RedirectToAction("Login", "Logout");

            // ------------------- 1) Ticket Tracking List -------------------
            var tickets = await _context.ICC_GET_TICKET_TrackingListV2Results
                .FromSqlInterpolated($@"
            EXEC ICC_GET_TICKET_TrackingListV2
                @User = {token.UserId},
                @Status = {status},
                @DateFrom = {dateFrom},
                @DateTo = {dateTo},
                @Priority = {priority},
                @BranchCompany = {branchCompany},
                @CreateBy = {createBy}")
                .ToListAsync();

            // ------------------- 2) Prepare Branch List -------------------
            var branchList = tickets
                .Where(t => !string.IsNullOrEmpty(t.BranchID))
                .Select(t => new { ID = t.BranchID, Name = t.Branch })
                .Distinct()
                .ToList();

            // ------------------- 3) Prepare CreatedBy List -------------------
            var createByList = tickets
                .Where(t => !string.IsNullOrEmpty(t.CreateByID))
                .Select(t => new { ID = t.CreateByID, Name = t.CreateByName })
                .Distinct()
                .ToList();

            // ------------------- 4) Handle Selected Branches -------------------
            ViewBag.SelectedBranches = string.Equals(branchCompany, "ALL", StringComparison.OrdinalIgnoreCase)
                ? branchList
                : branchList.Where(b => branchCompany.Split(',').Contains(b.ID)).ToList();

            // ------------------- 5) Handle Selected CreatedBy -------------------
            ViewBag.SelectedCreateBy = string.Equals(createBy, "ALL", StringComparison.OrdinalIgnoreCase)
                ? createByList
                : createByList.Where(u => createBy.Split(',').Contains(u.ID)).ToList();

            // ------------------- 6) Pass Lists to View -------------------
            ViewBag.BranchList = branchList;
            ViewBag.CreateByList = createByList;

            // Keep filter values in ViewData for dateFrom/dateTo
            ViewData["dateFrom"] = dateFrom?.ToString("dd-MMM-yyyy") ?? "";
            ViewData["dateTo"] = dateTo?.ToString("dd-MMM-yyyy") ?? "";

            return View(tickets);
        }



        [HttpGet]
        public async Task<IActionResult> TicketHistory(string id)
        {
            var token = GetTokenData();
            if (token == null) return RedirectToAction("Login", "Logout");

            // ------------------- Load chat history -------------------
            var chatResult = await _context.TicketChatHistoryModeratorResult
                .FromSqlInterpolated($@" EXEC ICC_GET_TICKETChatHistoryForClient 
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
                Type = "TickCusComment",
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
        public async Task<IActionResult> UpdateTicketStatus(string id)
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
            var rows = await _context.TicketCompleteFeedBackResult
                .FromSqlRaw(
                 "EXEC ICC_GET_TICKETCompletFeedBack @User, @EntryPrimary, @JsonBody",
                 new SqlParameter("@User", token.UserId.ToString() ?? (object)DBNull.Value),
                 new SqlParameter("@EntryPrimary", id ?? (object)DBNull.Value),
                 new SqlParameter("@JsonBody", "" ?? (object)DBNull.Value)
             )
             .AsNoTracking()
             .ToListAsync();



            // Ensure we have an empty list if no rows
            rows ??= new List<TicketCompleteFeedBack>();
            ViewBag.Header = header;
            ViewBag.Rows = rows;

            return View(ticket);
        }


        [HttpPost]
 
        public async Task<IActionResult> SaveTicketClient([FromBody] TicketClientHeader model)

        {
            // Get user token
            var tokenData = GetTokenData();
            string entryPrimary = tokenData?.UserId.ToString() ?? "";

            // Ensure defaults for header
            model.Mode ??= "Add";
            model.SecretCode ??= "";
            model.RefID ??= "ALL";
            model.RowList ??= new List<TicketClientRow>();

            // Update RowList items
            foreach (var row in model.RowList)
            {
                // Keep existing IDs
                row.AssignIDLine = row.AssignIDLine;
                row.AssignID = row.AssignID;
                row.RefID = model.RefID;
                // Defaults
                row.Status ??= "Done";
                row.Remark ??= "";
                row.Hide ??= "N";
                row.CreateBy = entryPrimary;
                //row.CreatedDate = "20250101";
            }

            // Build JSON body for SP
            var jsonBody = new
            {
                model.SecretCode,
                model.Mode,
                model.RefID,
                RowList = model.RowList
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Ticket1Client @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "Ticket1Client"),
                        new SqlParameter("@Trantype", model.Mode == "Add" ? "A" : "U"),
                        new SqlParameter("@EntryPrimary", entryPrimary ?? (object)DBNull.Value),
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();
                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });

                return Json(new { success = false, message = result?.Message ?? "Error saving TicketClient." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while saving TicketClient." });
            }
        }


        public async Task<IActionResult> ApproveTicket(string status = "Draf", bool isCountOnly = false)
        {
            var token = GetTokenData();
            if (token == null)
                return RedirectToAction("Login", "Logout");

            // ------------------- Ticket Tracking List -------------------
            var tickets = await _context.ICC_GET_TICKET_AssignListV2Results
                .FromSqlInterpolated($@"
            EXEC ICC_GET_TICKET_PenddingApprove
                @User = {token.UserId},
                @Status = {status},
                @EntryPrimary = 'ALL',
                @JsonBody = 'ALL'")
                .ToListAsync();

            if (isCountOnly)
            {
                return Json(new { count = tickets.Count });
            }

            // ------------------- Report Tracking Parameters -------------------
            var branchList = await _context.ReportTrackingResults
                .FromSqlInterpolated($"EXEC ICC_ReportTracking_ParameterV2 @Type = {"Branch"}")
                .ToListAsync();

            var createByList = await _context.ReportTrackingResults
                .FromSqlInterpolated($"EXEC ICC_ReportTracking_ParameterV2 @Type = {"CreateBy"}")
                .ToListAsync();

            ViewBag.BranchList = branchList;
            ViewBag.CreateByList = createByList;

            return View(tickets);
        }


        [HttpGet]
        public async Task<IActionResult> SaveApproveTicket(string id)
        {
            var token = GetTokenData();
            if (token == null) return RedirectToAction("Login", "Logout");

            // Load ticket info
            var ticketResult = await _context.ICC_GET_TICKET_TrackingListV2Results
             .FromSqlInterpolated($"EXEC ICC_GET_TICKET_ByIDV2 @ID = {id}")
             .ToListAsync();

            var ticket = ticketResult.FirstOrDefault();  // take the first item

            if (ticket == null) return NotFound();
            // Pass ticket to the view
            return View(ticket);
        }


        [HttpPost]
        public async Task<IActionResult> ApproveTicketDeccession(string TicketId, string Reason,string Status)
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
                        new SqlParameter("@Status", Status),
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
        public async Task<IActionResult> GetAlerts(bool isCountOnly = false)
        {
            var token = GetTokenData(); // Get current user
            if (token == null)
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var alerts = await _context.Set<AlertDto>()
                    .FromSqlInterpolated($"EXEC ICC_GET_AlerByUser @UserID={token.UserId}")
                    .AsNoTracking()
                    .ToListAsync();

                if (isCountOnly)
                    return Json(new { success = true, count = alerts.Count });

                return Json(new { success = true, data = alerts });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAlertRead(string id)
        {
            var token = GetTokenData();
            if (token == null) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                // Single alert read
                var param = new SqlParameter("@ID", id);
                await _context.Database.ExecuteSqlRawAsync("EXEC ICC_SET_AlerByUser @ID", param);

                return Json(new { success = true, message = "Alert marked as read." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAlertsRead()
        {
            var token = GetTokenData();
            if (token == null) return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var param = new SqlParameter("@User", token.UserId);
                await _context.Database.ExecuteSqlRawAsync("EXEC ICC_SET_AlerAsReadAll @User", param);

                return Json(new { success = true, message = "All alerts marked as read." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ReportPreview()
        {
            var token = GetTokenData();
            if (token == null)
                return RedirectToAction("Login", "Logout");

            try
            {


                var summary = (await _context.Set<ClientTicketSummaryDto>()
                .FromSqlInterpolated($"EXEC ICC_RPT_ClientTicket_Summary @UserID={token.UserId}")
                .AsNoTracking()
                .ToListAsync())
                .FirstOrDefault(); // pick first row

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


        /// For Mobile App API 


        [HttpGet("api/GetAttributeforTicket")]
        public async Task<IActionResult> GetAttributeforTicket(
            [FromHeader] int UserId,
            [FromHeader] string CompanyId,
            [FromHeader] string Type)
        {
            if (UserId <= 0 || string.IsNullOrEmpty(CompanyId))
                return BadRequest(new { message = "Missing or invalid headers: UserId, CompanyId, or Type." });

            bool isClientType = Type?.ToLower() == "client";

            var branchList = await GetBranchListAsync(UserId, CompanyId);
            var branchClientList = await GetBranchClientListAsync(UserId, CompanyId);
            var attributeList = await GetAttributeRulesAsync();

            var result = new
            {
                IsClientType = isClientType,
                BranchList = branchList,
                BranchClientList = branchClientList,
                Modules = attributeList.Where(a => a.AType == "Module").ToList(),
                Priorities = attributeList.Where(a => a.AType == "Priority").ToList(),
                ProblemTypes = attributeList.Where(a => a.AType == "ProblemType").ToList(),
                SubProblemTypes = attributeList.Where(a => a.AType == "SubProblemType").ToList()
            };

            return Ok(result);
        }

        #region Helper Methods

        private async Task<List<BranchDto>> GetBranchListAsync(int userId, string companyId)
        {
            return await _context.Branches
                .FromSqlRaw("EXEC ICC_GET_Branch @UserID, @Company",
                    new SqlParameter("@UserID", userId),
                    new SqlParameter("@Company", companyId))
                .AsNoTracking()
                .ToListAsync();
        }

        private async Task<List<BranchDto>> GetBranchClientListAsync(int userId, string companyId)
        {
            return await _context.Branches
                .FromSqlRaw("EXEC ICC_GET_Branch_Client @UserID, @Company",
                    new SqlParameter("@UserID", userId),
                    new SqlParameter("@Company", companyId))
                .AsNoTracking()
                .ToListAsync();
        }

        private async Task<List<TicketAttribute>> GetAttributeRulesAsync()
        {
            return await _context.Attributes
                .FromSqlRaw("EXEC ICC_GET_AttributeRule @EntryPrimary, @JsonBody, @Type",
                    new SqlParameter("@EntryPrimary", "-1"),
                    new SqlParameter("@JsonBody", ""),
                    new SqlParameter("@Type", "ALL"))
                .AsNoTracking()
                .ToListAsync();
        }

        #endregion

        [HttpPost("api/CreateTicket")]
        public async Task<IActionResult> CreateTicketAPI(
         [FromBody] Ticket model,
         [FromHeader] int UserId,
         [FromHeader] string CompanyId,
         [FromHeader] string Type)
        {
            if (UserId <= 0 || string.IsNullOrEmpty(CompanyId))
                return BadRequest(new { message = "Missing or invalid headers: UserId, CompanyId, or Type." });

            model.CreateBy ??= UserId.ToString();
            model.CreatedDate = DateTime.Now;
            model.Status ??= "Request";

            // Prepare JSON body for SP
            var jsonBody = new
            {
                SecretCode = "Tga5vz5oWiClgzayaLMZAx7KoTGLSJGrCQkY01gNb0c=",
                Mode = "Add",
                TicketId = -1,
                RefID = model.RefID,
                Title = model.Title,
                Description = model.Description,
                Status = model.Status,
                Priority = model.Priority,
                CreatedDate = model.CreatedDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                DeadlineDate = model.DeadlineDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                CloseDate = model.CloseDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                CreateBy = model.CreateBy,
                UpdateBy = model.UpdateBy,
                Attachment = model.Attachment,
                ProblemType = model.ProblemType,
                Module = model.Module,
                Branch = model.Branch,
                SubProblemType = model.SubProblemType,
                Project = model.Project
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Ticket @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "Ticket"),
                        new SqlParameter("@Trantype", "A"),
                        new SqlParameter("@EntryPrimary", UserId),
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();
                if (result != null && result.Code == 200)
                    return Ok(new { success = true, message = result.Message });

                return BadRequest(new { success = false, message = result?.Message ?? "Error creating ticket." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Unexpected error: {ex.Message}" });
            }
        }


        [HttpPost("api/UploadAttachment")]
        [RequestSizeLimit(10_000_000)] // 10 MB
        public async Task<IActionResult> UploadAttachment(
            [FromForm] int TicketId,
            IFormFile Attachment)
        {
            if (Attachment == null || Attachment.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".xlsx", ".xls", ".csv", ".pdf" };
            var fileExt = Path.GetExtension(Attachment.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExt))
                return BadRequest(new { message = "Invalid file type. Allowed: images or Excel/PDF files." });

            if (Attachment.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "File size exceeds 5MB limit." });

            var sanitizedFileName = System.Text.RegularExpressions.Regex.Replace(
                Path.GetFileNameWithoutExtension(Attachment.FileName),
                @"[^a-zA-Z0-9_-]", "_");

            var newFileName = $"{TicketId}_{DateTime.Now:dd_MM_yyyy_HH_mm_ss}_{sanitizedFileName}{fileExt}";
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "UploadedFiles");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, newFileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await Attachment.CopyToAsync(stream);

            var relativePath = "/UploadedFiles/" + newFileName;

            // TODO: Save this attachment path to Ticket record in DB if needed
            // e.g., update Ticket set Attachment = @relativePath where TicketId = @TicketId

            return Ok(new { success = true, path = relativePath });
        }


        [HttpPost("api/EditTicket")]
        public async Task<IActionResult> EditTicketAPI(
            [FromBody] Ticket model,
            [FromHeader] int UserId,
            [FromHeader] string CompanyId,
            [FromHeader] string Type)
        {
            if (UserId <= 0 || string.IsNullOrEmpty(CompanyId))
                return BadRequest(new { message = "Missing or invalid headers: UserId, CompanyId, or Type." });

            model.UpdateBy = UserId.ToString();
            model.UpdatedDate = DateTime.Now;

            // Build JSON body for stored procedure
            var jsonBody = new
            {
                SecretCode = "Tga5vz5oWiClgzayaLMZAx7KoTGLSJGrCQkY01gNb0c=",
                Mode = "Update",
                TicketId = model.TicketId,
                RefID = model.RefID,
                Title = model.Title,
                Description = model.Description,
                Status = model.Status ?? "Request",
                Priority = model.Priority,
                UpdatedDate = model.UpdatedDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdateBy = model.UpdateBy,
                Attachment = model.Attachment,
                ProblemType = model.ProblemType,
                Module = model.Module,
                Branch = model.Branch,
                SubProblemType = model.SubProblemType,
                Project = model.Project
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Ticket @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "Ticket"),
                        new SqlParameter("@Trantype", "E"),
                        new SqlParameter("@EntryPrimary", UserId),
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();
                if (result != null && result.Code == 200)
                    return Ok(new { success = true, message = result.Message });

                return BadRequest(new { success = false, message = result?.Message ?? "Error updating ticket." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Unexpected error: {ex.Message}" });
            }
        }


        [HttpGet("api/TicketTracking")]
        public async Task<IActionResult> TicketTrackingAPI(
           [FromHeader] int UserId,
           [FromHeader] string Type,
           [FromHeader] string status,
           [FromHeader] DateTime? dateFrom = null,
           [FromHeader] DateTime? dateTo = null,
           [FromHeader] string priority = null,
           [FromHeader] string branchCompany = null,
           [FromHeader] string createBy = null)
        {
            if (UserId <= 0)
                return BadRequest(new { message = "Missing or invalid UserId header." });

            // ✅ Apply default values only when null or empty
            status = string.IsNullOrWhiteSpace(status) ? "Request,Pending,Draf,Checking,Progress" : status;
            priority = string.IsNullOrWhiteSpace(priority) ? "ALL" : priority;
            branchCompany = string.IsNullOrWhiteSpace(branchCompany) ? "ALL" : branchCompany;
            createBy = string.IsNullOrWhiteSpace(createBy) ? "ALL" : createBy;

            // ------------------- 1) Ticket Tracking List -------------------
            var tickets = await _context.ICC_GET_TICKET_TrackingListV2Results
                .FromSqlInterpolated($@"
                    EXEC ICC_GET_TICKET_TrackingListV2
                        @User = {UserId},
                        @Status = {status},
                        @DateFrom = {dateFrom},
                        @DateTo = {dateTo},
                        @Priority = {priority},
                        @BranchCompany = {branchCompany},
                        @CreateBy = {createBy}")
                .ToListAsync();

            // ------------------- 2) Prepare Branch List -------------------
            var branchList = tickets
                .Where(t => !string.IsNullOrEmpty(t.BranchID))
                .Select(t => new { ID = t.BranchID, Name = t.Branch })
                .Distinct()
                .ToList();

            // ------------------- 3) Prepare CreatedBy List -------------------
            var createByList = tickets
                .Where(t => !string.IsNullOrEmpty(t.CreateByID))
                .Select(t => new { ID = t.CreateByID, Name = t.CreateByName })
                .Distinct()
                .ToList();

            // ------------------- 4) Handle Selected Branches -------------------
            var selectedBranches = string.Equals(branchCompany, "ALL", StringComparison.OrdinalIgnoreCase)
                ? branchList
                : branchList.Where(b => branchCompany.Split(',').Contains(b.ID)).ToList();

            // ------------------- 5) Handle Selected CreatedBy -------------------
            var selectedCreateBy = string.Equals(createBy, "ALL", StringComparison.OrdinalIgnoreCase)
                ? createByList
                : createByList.Where(u => createBy.Split(',').Contains(u.ID)).ToList();

            // ------------------- 6) Return JSON -------------------
            return Ok(new
            {
                tickets,
                branchList,
                createByList,
                selectedBranches,
                selectedCreateBy,
                filter = new
                {
                    status,
                    dateFrom = dateFrom?.ToString("dd-MMM-yyyy") ?? "",
                    dateTo = dateTo?.ToString("dd-MMM-yyyy") ?? "",
                    priority,
                    branchCompany,
                    createBy
                }
            });
        }


        [HttpGet("api/TicketHistory")]
        public async Task<IActionResult> TicketHistoryAPI(
            [FromHeader] int UserId,
            [FromHeader] string Type,
            [FromQuery] string TicketId)
        {
            if (UserId <= 0 || string.IsNullOrEmpty(TicketId))
                return BadRequest(new { message = "Missing UserId header or TicketId query parameter." });

            // ------------------- 1) Load chat history -------------------
            var chatResult = await _context.TicketChatHistoryModeratorResult
                .FromSqlInterpolated($@"
            EXEC ICC_GET_TICKETChatHistoryForClient 
                @User = {UserId}, 
                @EntryPrimary = {TicketId}, 
                @JsonBody = ''")
                .ToListAsync();

            if (chatResult == null || !chatResult.Any())
                return NotFound(new { message = "No chat history found." });

            // ------------------- 2) Load ticket info -------------------
            var ticketResult = await _context.ICC_GET_TICKET_TrackingListV2Results
                .FromSqlInterpolated($@"EXEC ICC_GET_TICKET_ByIDV2 @ID = {TicketId}")
                .ToListAsync();

            var ticket = ticketResult.FirstOrDefault();
            if (ticket == null)
                return NotFound(new { message = "Ticket not found." });

            // ------------------- 3) Return JSON -------------------
            return Ok(new
            {
                ticket,
                chatHistory = chatResult,
                currentUserId = UserId
            });
        }


        [HttpPost("api/Comment")]
        public async Task<IActionResult> CommentAPI(
            [FromHeader] int UserId,
            [FromHeader] string Type,
            [FromHeader] string TicketRef,
            [FromHeader] string FeedbackText)
        {
            if (UserId <= 0 || string.IsNullOrEmpty(TicketRef) || string.IsNullOrEmpty(FeedbackText))
                return BadRequest(new { success = false, message = "Missing headers: UserId, TicketRef, or FeedbackText." });

            // --- Build JSON body for stored procedure ---
            var jsonBody = new
            {
                SecretCode = "Tga5vz5oWiClgzayaLMZAx7KoTGLSJGrCQkY01gNb0c=",
                FeedbackId = -1,
                TicketRef = TicketRef,
                Rating = 0,
                FeedbackText = FeedbackText,
                Type = "TickCusComment",
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                CreateBy = UserId.ToString(),
                UpdateBy = UserId.ToString()
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Feedback @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@Trantype", "A"),
                        new SqlParameter("@EntryPrimary", TicketRef ?? (object)DBNull.Value),
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
                return StatusCode(500, new { success = false, message = $"Unexpected error: {ex.Message}" });
            }
        }


        [HttpPost("api/RejectTicket")]
        public async Task<IActionResult> RejectTicketAPI(
            [FromHeader] int UserId,
            [FromHeader] string TicketId,
            [FromHeader] string Reason)
        {
            if (UserId <= 0)
                return BadRequest(new { success = false, message = "Missing or invalid header: UserId." });

            if (string.IsNullOrWhiteSpace(TicketId))
                return BadRequest(new { success = false, message = "Missing header: TicketId." });

            if (string.IsNullOrWhiteSpace(Reason))
                return BadRequest(new { success = false, message = "Missing header: Reason." });

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_UPDATE_TICKET_STATUS @User, @EntryPrimary, @Status, @Reason",
                        new SqlParameter("@User", UserId),
                        new SqlParameter("@EntryPrimary", TicketId),
                        new SqlParameter("@Status", "Reject"),
                        new SqlParameter("@Reason", Reason))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();
                if (result != null && result.Code == 200)
                    return Ok(new { success = true, message = result.Message });

                return BadRequest(new { success = false, message = result?.Message ?? "Error rejecting ticket." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Unexpected error: {ex.Message}" });
            }
        }
        public static DateTime TrimMilliseconds(DateTime dt)
        {
            return new DateTime(dt.Ticks - (dt.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
        }


        [HttpGet("api/GetUpdateTicketStatusbyID")]
        public async Task<IActionResult> GetUpdateTicketStatusbyIDAPI(
            [FromHeader] int UserId,
            [FromHeader] string TicketId)
        {
            if (UserId <= 0)
                return BadRequest(new { success = false, message = "Missing or invalid header: UserId." });

            if (string.IsNullOrWhiteSpace(TicketId))
                return BadRequest(new { success = false, message = "Missing header: TicketId." });

            try
            {
                // ------------------- 1) Load ticket info -------------------
                var ticketResult = await _context.ICC_GET_TICKET_TrackingListV2Results
                    .FromSqlInterpolated($"EXEC ICC_GET_TICKET_ByIDV2 @ID = {TicketId}")
                    .ToListAsync();

                var ticket = ticketResult.FirstOrDefault();
                if (ticket == null)
                    return NotFound(new { success = false, message = "Ticket not found." });

                // ------------------- 2) Load assignment header -------------------
                var headerResult = await _context.TicketAssignHeaderV2Result
                    .FromSqlInterpolated($"EXEC ICC_GET_TICKET_AssignHeaderV2 @ID = {TicketId}")
                    .ToListAsync();

                var header = headerResult.FirstOrDefault();

                // ------------------- 3) Load assignment rows -------------------
                var rows = await _context.TicketCompleteFeedBackResult
                    .FromSqlRaw(
                        "EXEC ICC_GET_TICKETCompletFeedBack @User, @EntryPrimary, @JsonBody",
                        new SqlParameter("@User", UserId),
                        new SqlParameter("@EntryPrimary", TicketId),
                        new SqlParameter("@JsonBody", ""))
                    .AsNoTracking()
                    .ToListAsync();

                // Ensure we always return a list even if empty
                rows ??= new List<TicketCompleteFeedBack>();

                return Ok(new
                {
                    success = true,
                    message = "Ticket status details loaded successfully.",
                    ticket,
                    header,
                    rows
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Unexpected error: {ex.Message}" });
            }
        }

        [HttpPost("api/UserUpdateTicketStatus")]
        public async Task<IActionResult> UserUpdateTicketStatusAPI([FromBody] TicketClientHeader model)
        {
            try
            {
                // 1️⃣ Read required headers
                var secretCode = Request.Headers["SecretCode"].FirstOrDefault() ?? "";
                var mode = Request.Headers["Mode"].FirstOrDefault() ?? "Add";
                var refID = Request.Headers["RefID"].FirstOrDefault() ?? "ALL";
                var entryPrimary = Request.Headers["UserId"].FirstOrDefault() ?? "SYSTEM";

                // 2️⃣ Ensure defaults for header
                model.SecretCode = secretCode;
                model.Mode = mode;
                model.RefID = refID;
                model.RowList ??= new List<TicketClientRow>();

                // 3️⃣ Update RowList items
                foreach (var row in model.RowList)
                {
                    row.RefID = model.RefID;
                    row.Status ??= "Done";
                    row.Remark ??= "";
                    row.Hide ??= "N";
                    row.CreateBy ??= entryPrimary;
                    row.CreatedDate = TrimMilliseconds(DateTime.UtcNow);

                }

                // 4️⃣ Build JSON body for SP
                var jsonBody = new
                {
                    model.SecretCode,
                    model.Mode,
                    model.RefID,
                    RowList = model.RowList
                };

                var jsonString = JsonConvert.SerializeObject(jsonBody);

                // 5️⃣ Execute stored procedure
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Ticket1Client @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "Ticket1Client"),
                        new SqlParameter("@Trantype", model.Mode == "Add" ? "A" : "U"),
                        new SqlParameter("@EntryPrimary", entryPrimary ?? (object)DBNull.Value),
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                // 6️⃣ Handle response
                var result = spResults.FirstOrDefault();
                if (result != null && result.Code == 200)
                {
                    return Ok(new
                    {
                        success = true,
                        message = result.Message
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = result?.Message ?? "Error saving TicketClient."
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unexpected error occurred while saving TicketClient.",
                    detail = ex.Message
                });
            }
        }

        [HttpGet("api/GetPendingApproveList")]
        public async Task<IActionResult> GetPendingApproveListAPI(
            [FromHeader] string UserId,
            [FromHeader] string Status = "Draft",
            [FromHeader] string EntryPrimary = "ALL",
            [FromHeader] string JsonBody = "ALL",
            [FromHeader] bool IsCountOnly = false)
        {
            if (string.IsNullOrEmpty(UserId))
                return BadRequest(new { message = "UserId header is required." });

            try
            {
                // ------------------- Ticket Tracking List -------------------
                var tickets = await _context.ICC_GET_TICKET_AssignListV2Results
                    .FromSqlInterpolated($@"
                EXEC ICC_GET_TICKET_PenddingApprove
                    @User = {UserId},
                    @Status = {Status},
                    @EntryPrimary = {EntryPrimary},
                    @JsonBody = {JsonBody}")
                    .ToListAsync();

                if (IsCountOnly)
                {
                    return Ok(new { count = tickets.Count });
                }

                return Ok(new
                {
                    tickets
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("api/GetApproveTicketByID")]
        public async Task<IActionResult> GetApproveTicketByIDAPI([FromHeader] string UserId, [FromHeader] string TicketId)
        {
            if (string.IsNullOrEmpty(UserId))
                return BadRequest(new { success = false, message = "UserId header is required." });

            if (string.IsNullOrEmpty(TicketId))
                return BadRequest(new { success = false, message = "TicketId header is required." });

            try
            {
                // ------------------- Load ticket info -------------------
                var ticketResult = await _context.ICC_GET_TICKET_TrackingListV2Results
                    .FromSqlInterpolated($"EXEC ICC_GET_TICKET_ByIDV2 @ID = {TicketId}")
                    .ToListAsync();

                var ticket = ticketResult.FirstOrDefault();
                if (ticket == null)
                    return NotFound(new { success = false, message = "Ticket not found." });

                // Return ticket data as JSON
                return Ok(new { success = true, ticket });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Unexpected error: " + ex.Message });
            }
        }

        [HttpPost("api/ApproveTicketDecision")]
        public async Task<IActionResult> ApproveTicketDecisionAPI(
            [FromHeader] string UserId,
            [FromHeader] string TicketId,
            [FromHeader] string Reason,
            [FromHeader] string Status)
        {
            if (string.IsNullOrEmpty(UserId))
                return BadRequest(new { success = false, message = "UserId header is required." });

            if (string.IsNullOrEmpty(TicketId))
                return BadRequest(new { success = false, message = "TicketId header is required." });

            if (string.IsNullOrWhiteSpace(Reason))
                return BadRequest(new { success = false, message = "Reason is required." });

            if (string.IsNullOrEmpty(Status))
                return BadRequest(new { success = false, message = "Status header is required." });

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_UPDATE_TICKET_STATUS @User, @EntryPrimary, @Status, @Reason",
                        new SqlParameter("@User", UserId),
                        new SqlParameter("@EntryPrimary", TicketId),
                        new SqlParameter("@Status", Status),
                        new SqlParameter("@Reason", Reason))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();
                if (result != null && result.Code == 200)
                    return Ok(new { success = true, message = result.Message });

                return BadRequest(new { success = false, message = result?.Message ?? "Error updating ticket status." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Unexpected error: " + ex.Message });
            }
        }

    }
}
