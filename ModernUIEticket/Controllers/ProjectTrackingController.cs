using ETicketNewUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel.Design;
using System.Text;
namespace ModernUIEticket.Controllers
{
    public class ProjectTrackingController : Controller
    {
        private readonly TicketDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProjectTrackingController(TicketDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

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

        public async Task<IActionResult> Index()
        {
            var token = GetTokenData();
            if (token == null)
                return RedirectToAction("Login", "Logout");

            var list = await _context.ProjectTrackingLists
                .FromSqlInterpolated($@"
            EXEC ICC_GET_ProjectTracking 
            @User = {token.UserId},
            @EntryPrimary = {"ALL"}, 
            @JsonBody = {"ALL"}")
                .ToListAsync();

            return View(list);
        }

        public async Task<IActionResult> AddProjectTracking()
        {
            var tokenData = GetTokenData();
            // Load main branches
            var branchList = await _context.ProjectAvailables
                .FromSqlInterpolated($"EXEC ICC_GET_AvailableProjectTracking @User = {tokenData.UserId}")
                .ToListAsync();

            ViewBag.MainCompany = branchList;

            var supporterList = await _context.StaffListAssignResult
               .FromSqlInterpolated($"EXEC ICC_GET_StaffAllV2")
               .ToListAsync();
            ViewBag.SupporterList = supporterList;


            return View();
        }

        #region Add Project Tracking (POST)
        [HttpPost]
        public async Task<IActionResult> AddProjectTracking([FromBody] ProjectTracking model)
        {
            var token = GetTokenData();
            if (token == null)
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                // Set defaults
                model.Mode ??= "Add";
                model.CreatedBy = token.UserId.ToString();
                model.UpdatedBy ??= token.UserId.ToString();
                model.CreateDate = model.StartDate; // truncate if needed
                model.UpdateDate = model.StartDate;
                model.Status ??= "A";
                model.RowList?.ForEach(r => r.RowStatus ??= "Pending");

                // Serialize to JSON
                var jsonString = JsonConvert.SerializeObject(model);

                var spResults = await _context.Set<SpResult>()
                 .FromSqlRaw("EXEC dbo.ICC_ProjectTracking @MasterType, @TranType, @EntryPrimary, @JsonBody",
                     new SqlParameter("@MasterType", "PRJ"),
                     new SqlParameter("@TranType", "Add"),
                     new SqlParameter("@EntryPrimary", model.ProjectCode ?? (object)DBNull.Value),
                     new SqlParameter("@JsonBody", jsonString))
                 .AsNoTracking()
                 .ToListAsync();


                var result = spResults.FirstOrDefault();
                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });

                return Json(new { success = false, message = result?.Message ?? "Error saving Project Tracking." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while saving Project Tracking." });
            }
        }
        #endregion




        public async Task<IActionResult> EditProjectTracking(int ID)
        {
            var tokenData = GetTokenData();
            if (tokenData == null)
                return RedirectToAction("Login", "Account");

            // Load available projects (for dropdown)
            var branchList = await _context.ProjectAvailables
                .FromSqlInterpolated($"EXEC ICC_GET_AvailableProjectTracking @User = {tokenData.UserId}")
                .ToListAsync();
            ViewBag.MainCompany = branchList;

            // Load supporter list
            var supporterList = await _context.StaffListAssignResult
                .FromSqlInterpolated($"EXEC ICC_GET_StaffAllV2")
                .ToListAsync();
            ViewBag.SupporterList = supporterList;

            // Load existing project tracking data
            var projectHeader = await _context.ProjectTrackings
                .FromSqlInterpolated($"EXEC ICC_GET_ProjectTrackingHeader @ID = {ID}")
                .ToListAsync();



            if (projectHeader == null || !projectHeader.Any())
                return NotFound();

            // Use the first item
            var header = projectHeader.First();

            var projectRows = await _context.ProjectTracking1s
                .FromSqlInterpolated($"EXEC ICC_GET_ProjectTrackingRow @ID = {ID}")
                .ToListAsync();

            // Assign to ViewBag for edit page
            ViewBag.Header = header;       // single header object
            ViewBag.RowList = projectRows; // list of rows

            return View("EditProjectTracking", header);
        }

        private DateTime? ToUtc(DateTime? date)
        {
            if (!date.HasValue) return null;
            return DateTime.SpecifyKind(date.Value, DateTimeKind.Utc);
        }


        #region Edit Project Tracking (POST)
        [HttpPost]
        public async Task<IActionResult> EditProjectTracking([FromBody] ProjectTracking model)
        {
            var token = GetTokenData();
            if (token == null)
                return Json(new { success = false, message = "Unauthorized" });

            model.Mode ??= "Update";
            model.UpdatedBy = token.UserId.ToString();
            model.UpdateDate = ToUtc(model.StartDate);
            model.CreateDate = ToUtc(model.StartDate);
            model.Status ??= "A";
            model.ProjectCreateDate = model.StartDate;

            // Ensure child rows have default values
            model.RowList?.ForEach(row =>
            {
                row.ID = model.ID;
                row.RowStatus ??= "A";
            });

            var jsonString = JsonConvert.SerializeObject(model);

            try
            {
                // Execute stored procedure for Edit
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_ProjectTracking @MasterType, @TranType, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "USR"),
                        new SqlParameter("@TranType", "Update"),
                        new SqlParameter("@EntryPrimary", model.ID),
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();
                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });

                return Json(new { success = false, message = result?.Message ?? "Error updating Project Tracking." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while updating Project Tracking." });
            }
        }
        #endregion


        #region Edit Project Tracking (POST)
        [HttpPost]
        public async Task<IActionResult> DeleteProjectTracking([FromBody] ProjectTracking model)
        {
            var token = GetTokenData();
            if (token == null)
                return Json(new { success = false, message = "Unauthorized" });

            model.Mode ??= "Delete";
            model.UpdatedBy = token.UserId.ToString();
            model.UpdateDate = model.StartDate;
            model.CreateDate = model.StartDate;
            model.Status ??= "A";
            model.ProjectCreateDate = model.StartDate;

            // Ensure child rows have default values
            model.RowList?.ForEach(row =>
            {
                row.ID = model.ID;
                row.RowStatus ??= "A";
            });

            var jsonString = JsonConvert.SerializeObject(model);

            try
            {
                // Execute stored procedure for Edit
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_ProjectTracking @MasterType, @TranType, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "USR"),
                        new SqlParameter("@TranType", "Delete"),
                        new SqlParameter("@EntryPrimary", model.ID),
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();
                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });

                return Json(new { success = false, message = result?.Message ?? "Error updating Project Tracking." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while updating Project Tracking." });
            }
        }
        #endregion
    }
}
