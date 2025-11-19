using ETicketNewUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;

namespace ModernUIEticket.Controllers
{
    public class ApprovalController : Controller
    {
        private readonly TicketDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ApprovalController(TicketDbContext context, IWebHostEnvironment env)
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
            if (token == null) return RedirectToAction("Login", "Logout");

            var List = await _context.Approvallist
                .FromSqlInterpolated($@"
                    EXEC ICC_GET_APPROVAL 
                    @EntryPrimary = {token.UserId},
                    @JsonBody = {"ALL"}")
                .ToListAsync();

            return View(List);
        }

        // GET: AddTicket
        public async Task<IActionResult> AddApproval()
        {
            var tokenData = GetTokenData();
            // Load main branches
            var branchList = await _context.ProjectAvailables
                .FromSqlInterpolated($"EXEC ICC_GET_AvailableProjectAssignApproval @User = {tokenData.UserId}")
                .ToListAsync();

            ViewBag.MainCompany = branchList;

            return View();
        }

        #region Get SubBranches
        [HttpPost]
        public async Task<JsonResult> GetBranches(int companyId)
        {
            var subbranchList = await _context.ProjectAvailables
                .FromSqlInterpolated($"EXEC ICC_GET_AvailableProjectAssign1 @ID = {companyId}")
                .ToListAsync();

            return Json(subbranchList);
        }
        #endregion

        #region GetUserAssign
        [HttpPost]
        public async Task<JsonResult> GetUserAssign(int companyId)
        {
            var userAssignList = await _context.UserforAssignApprovals
                .FromSqlInterpolated($"EXEC ICC_GET_UserforAssignApproval @ID = {companyId}")
                .ToListAsync();

            return Json(userAssignList);
        }
        #endregion


        #region Add Approval (POST)
        [HttpPost]
        public async Task<IActionResult> AddApproval([FromBody] ApprovalHeader model)
        {
            var token = GetTokenData();
            if (token == null)
                return Json(new { success = false, message = "Unauthorized" });

            model.Mode ??= "Add";
            model.ID = 0;
            model.CreateBy = token.UserId.ToString();
            model.UpdateBy = token.UserId.ToString();
            model.Status ??= "A";

            // Ensure all child rows are initialized
            model.RowList?.ForEach(d =>
            {
                d.ID = 1;
                d.Status ??= "A";
            });

            var jsonString = JsonConvert.SerializeObject(model);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Approval @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@Trantype", "Add"),
                        new SqlParameter("@EntryPrimary", token.UserId.ToString()), // use current user as EntryPrimary
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();
                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });

                return Json(new { success = false, message = result?.Message ?? "Error saving approval." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while saving approval." });
            }
        }
        #endregion


        // GET: EditApproval
        public async Task<IActionResult> EditApproval(int id)
        {
            var tokenData = GetTokenData();
            if (tokenData == null)
                return RedirectToAction("Login", "Logout");

            // Load main branches
            var branchList = await _context.ProjectAvailables
                .FromSqlInterpolated($"EXEC ICC_GET_ProjectAssignApproval @User = {tokenData.UserId}")
                .ToListAsync();

            ViewBag.MainCompany = branchList;

            // Get the main approval header
            var header = await _context.ApprovalHeaderRaws
                .FromSqlInterpolated($"EXEC ICC_GET_ApprovalHeaderRawByID @ID={id}")
                .ToListAsync();
            var headerByID = header.FirstOrDefault();

            if (headerByID == null)
                return NotFound();

            // Map to ApprovalHeader model
            var model = new ApprovalHeader
            {
                ID = headerByID.ID,
                CompanyId = headerByID.CompanyId,
                Status = headerByID.Status,
                Remark = headerByID.Remark,
                CreateBy = headerByID.CreateBy,
                UpdateBy = headerByID.UpdateBy
            };

            // Load existing approval rows
            var existingRows = await _context.ApprovalDetailRaws
            .FromSqlInterpolated($"EXEC ICC_GET_ApprovalRowRawByID @ID={id}")
            .AsNoTracking()
            .ToListAsync();


            ViewBag.RowLists = existingRows;

            return View("EditApproval", model); // Use the same view
        }

        // POST: EditApproval
        [HttpPost]
        public async Task<IActionResult> EditApproval([FromBody] ApprovalHeader model)
        {
            var token = GetTokenData();
            if (token == null)
                return Json(new { success = false, message = "Unauthorized" });

            model.Mode ??= "Update"; // <-- important
            model.UpdateBy = token.UserId.ToString();
            model.Status ??= "A";

            // Ensure all child rows have proper initialization
            model.RowList?.ForEach(d =>
            {
                d.ID ??= 0;
                d.Status ??= "A";
            });

            var jsonString = JsonConvert.SerializeObject(model);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Approval @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@Trantype", "Update"),
                        new SqlParameter("@EntryPrimary", token.UserId.ToString()),
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();
                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });

                return Json(new { success = false, message = result?.Message ?? "Error updating approval." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while updating approval." });
            }
        }

        // POST: EditApproval
        [HttpPost]
        public async Task<IActionResult> DeleteApproval([FromBody] ApprovalHeader model)
        {
            var token = GetTokenData();
            if (token == null)
                return Json(new { success = false, message = "Unauthorized" });

            model.Mode ??= "Delete"; // <-- important
            model.UpdateBy = token.UserId.ToString();
            model.Status ??= "A";

            // Ensure all child rows have proper initialization
            model.RowList?.ForEach(d =>
            {
                d.ID ??= 0;
                d.Status ??= "A";
            });

            var jsonString = JsonConvert.SerializeObject(model);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Approval @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@Trantype", "Update"),
                        new SqlParameter("@EntryPrimary", token.UserId.ToString()),
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();
                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });

                return Json(new { success = false, message = result?.Message ?? "Error updating approval." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while updating approval." });
            }
        }
    }
}
