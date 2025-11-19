using ETicketNewUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;

namespace ModernUIEticket.Controllers
{
    public class RecurringController : Controller
    {
        private readonly TicketDbContext _context;

        public RecurringController(TicketDbContext context)
        {
            _context = context;
        }

        #region Index
        public async Task<IActionResult> Index(string frequency = "ALL", string status = "ALL", string primaryKey = "ALL")
        {
            var tasks = await _context.RecurringTaskResults
                .FromSqlRaw("EXEC ICC_GET_RecurringTask @Frequency, @Status, @PrimaryKey",
                    new SqlParameter("@Frequency", frequency ?? "ALL"),
                    new SqlParameter("@Status", status ?? "ALL"),
                    new SqlParameter("@PrimaryKey", primaryKey ?? "ALL"))
                .ToListAsync();

            return View(tasks);
        }
        #endregion

        #region AddRecurring
        public async Task<IActionResult> AddRecurring()
        {
            var tokenData = GetTokenData();
            if (tokenData == null)
            {
                TempData["ErrorMessage"] = "Session expired. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            // Dropdowns
            ViewBag.FrequencyList = new List<string> { "Daily", "Weekly", "Monthly" };
            ViewBag.StatusList = new List<string> { "Active", "Inactive", "Completed" };

            // Staff List
            var UserList = await _context.StaffListAssignResult
                .FromSqlInterpolated($"EXEC ICC_GET_StaffAllV2")
                .ToListAsync();
            ViewBag.StaffList = UserList;

            // Project/Branch List
            ViewBag.ProjectList = await GetBranchClientListAsync(tokenData);

            return View();
        }
        #endregion

        #region Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RecurringTask model)
        {
            var tokenData = GetTokenData();
            if (tokenData == null)
            {
                return Json(new { success = false, message = "Session expired. Please log in again." });
            }

            string entryPrimary = tokenData.UserId.ToString();
            model.CreatedBy = model.CreatedBy != 0 ? model.CreatedBy : tokenData.UserId;

            var jsonBody = new
            {
                Mode = "Add",
                TaskID = model.TaskID != 0 ? model.TaskID : -1,
                ProjectID = model.ProjectID,
                AssignToUserID = model.AssignToUserID,
                HandleByUserID = model.HandleByUserID,
                FromDate = model.FromDate,
                ToDate = model.ToDate,
                Frequency = model.Frequency,
                ExecutionTime = model.ExecutionTime,
                ExecutionDayForWeekly = model.ExecutionDayForWeekly,
                ExecutionDayForMonthly = model.ExecutionDayForMonthly,
                ExecutationMonthForYearly = model.ExecutationMonthForYearly,
                Deadline = model.Deadline,
                Status = "Active",
                Remark = model.Remark,
                CreatedBy = model.CreatedBy,
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedBy = model.UpdatedBy,
                UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_RecurringTask @MasterType, @TranType, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "RecurringTask"),
                        new SqlParameter("@TranType", "A"),
                        new SqlParameter("@EntryPrimary", entryPrimary),
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
                    return Json(new { success = false, message = result?.Message ?? "Error creating recurring task." });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while creating recurring task." });
            }
        }
        #endregion

        #region Edit
        public async Task<IActionResult> Edit(int id)
        {

            var tasks = await _context.RecurringTaskResults
            .FromSqlRaw("EXEC ICC_GET_RecurringTask @Frequency, @Status, @PrimaryKey",
                new SqlParameter("@Frequency", "ALL"),
                new SqlParameter("@Status", "ALL"),
                new SqlParameter("@PrimaryKey", id.ToString()))
            .ToListAsync();

            var task = tasks.FirstOrDefault();


            if (task == null)
            {
                return NotFound();
            }

            // Dropdowns
            ViewBag.FrequencyList = new List<string> { "Daily", "Weekly", "Monthly" };
            ViewBag.StatusList = new List<string> { "Active", "Inactive", "Completed" };

            // Staff List
            var UserList = await _context.StaffListAssignResult
                .FromSqlInterpolated($"EXEC ICC_GET_StaffAllV2")
                .ToListAsync();
            ViewBag.StaffList = UserList;

            // Project/Branch List
            var tokenData = GetTokenData();
            ViewBag.ProjectList = await GetBranchClientListAsync(tokenData);

            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RecurringTask model)
        {
            var tokenData = GetTokenData();
            if (tokenData == null)
            {
                return Json(new { success = false, message = "Session expired. Please log in again." });
            }

            string entryPrimary = tokenData.UserId.ToString();

            var jsonBody = new
            {
                Mode = "Edit",
                TaskID = model.TaskID,
                ProjectID = model.ProjectID,
                AssignToUserID = model.AssignToUserID,
                HandleByUserID = model.HandleByUserID,
                FromDate = model.FromDate,
                ToDate = model.ToDate,
                Frequency = model.Frequency,
                ExecutionTime = model.ExecutionTime,
                ExecutionDayForWeekly = model.ExecutionDayForWeekly,
                ExecutionDayForMonthly = model.ExecutionDayForMonthly,
                ExecutationMonthForYearly = model.ExecutationMonthForYearly,
                Deadline = model.Deadline,
                Status = model.Status,
                Remark = model.Remark,
                UpdatedBy = tokenData.UserId,
                UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_RecurringTask @MasterType, @TranType, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "RecurringTask"),
                        new SqlParameter("@TranType", "U"),
                        new SqlParameter("@EntryPrimary", entryPrimary),
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
                    return Json(new { success = false, message = result?.Message ?? "Error updating recurring task." });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while updating recurring task." });
            }
        }
        #endregion

        #region Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var tokenData = GetTokenData();
            if (tokenData == null)
            {
                return Json(new { success = false, message = "Session expired. Please log in again." });
            }

            string entryPrimary = tokenData.UserId.ToString();

            var jsonBody = new
            {
                Mode = "Delete",
                TaskID = id
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_RecurringTask @MasterType, @TranType, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "RecurringTask"),
                        new SqlParameter("@TranType", "D"),
                        new SqlParameter("@EntryPrimary", entryPrimary),
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
                    return Json(new { success = false, message = result?.Message ?? "Error deleting recurring task." });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while deleting recurring task." });
            }
        }
        #endregion

        #region Helper Methods
        private LoginResultDto? GetTokenData()
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
        #endregion
    }
}
