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
    public class ProjectAssignController : Controller
    {
        private readonly TicketDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProjectAssignController(TicketDbContext context, IWebHostEnvironment env)
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

            var List = await _context.ProjectAssignResults
                .FromSqlInterpolated($@"
                    EXEC ICC_GET_ProjectAssign 
                    @User = {token.UserId},
                    @EntryPrimary = {"ALL"}, 
                    @JsonBody = {"ALL"}")
                .ToListAsync();

            return View(List);
        }

        // GET: AddTicket
        public async Task<IActionResult> AddProjectAssign()
        {
            var tokenData = GetTokenData();
            // Load main branches
            var branchList = await _context.ProjectAvailables
                .FromSqlInterpolated($"EXEC ICC_GET_AvailableProjectAssign @User = {tokenData.UserId}")
                .ToListAsync();

            ViewBag.MainCompany = branchList;

            var supporterList = await _context.StaffListAssignResult
               .FromSqlInterpolated($"EXEC ICC_GET_StaffAllV2")
               .ToListAsync();
            ViewBag.SupporterList = supporterList;

            var attributeList = await GetAttributeRulesAsync(tokenData);
            ViewBag.Role = attributeList.Where(a => a.AType == "Role").ToList();


            return View();
        }

        private async Task<List<TicketAttribute>> GetAttributeRulesAsync(LoginResultDto tokenData)
        {
            if (tokenData == null) return new List<TicketAttribute>();

            return await _context.Attributes
                .FromSqlRaw("EXEC ICC_GET_AttributeRule @EntryPrimary, @JsonBody, @Type",
                    new SqlParameter("@EntryPrimary", "-1"),
                    new SqlParameter("@JsonBody", ""),
                    new SqlParameter("@Type", "Role"))
                .AsNoTracking()
                .ToListAsync();
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

        #region Add User (POST)
        [HttpPost]
        public async Task<IActionResult> AddProjectAssign([FromBody] ProjectAssignHeader model)
        {
            var token = GetTokenData();
            if (token == null)
                return Json(new { success = false, message = "Unauthorized" });

            model.Mode ??= "Add";
            model.CreateBy = token.UserId.ToString();
            model.UpdateBy = token.UserId.ToString();
            // Truncate to seconds so SQL DATETIME accepts it
            model.CreatedDate = model.FromDate;
            model.UpdatedDate = model.FromDate;
            model.Status ??= "A";
            model.RowList?.ForEach(b => b.Status ??= "A");
            var jsonString = JsonConvert.SerializeObject(model);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_ProjectAssign @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "USR"),
                        new SqlParameter("@Trantype", "Add"),
                        new SqlParameter("@EntryPrimary", model.ClientID),
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();
                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });

                return Json(new { success = false, message = result?.Message ?? "Error saving Project Assign." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while saving user." });
            }
        }
        #endregion

        private DateTime? ToUtc(DateTime? date)
        {
            if (!date.HasValue) return null;
            return DateTime.SpecifyKind(date.Value, DateTimeKind.Utc);
        }

        // GET: Edit
        public async Task<IActionResult> EditProjectAssign(int id)
        {
            var tokenData = GetTokenData();
            if (tokenData == null) return RedirectToAction("Login", "Logout");

            // Get the main project
            var projectList = await _context.ProjectAssignHeaderByIDs
                .FromSqlInterpolated($"EXEC ICC_GET_ProjectAssignByID @ID={id}")
                .ToListAsync();
            var projectByID = projectList.FirstOrDefault();

            if (projectByID == null)
                return NotFound();

            // Map to ProjectAssignHeader
            var project = new ProjectAssignHeader
            {
                ID = Convert.ToInt16(projectByID.ID),
                ClientID = Convert.ToInt16( projectByID.ClientID),
                FromDate = projectByID.FromDate, // handle nullable
                ToDate = projectByID.ToDate,
                Remark = projectByID.Remark,
                Status = projectByID.Status,
                CreatedDate = ToUtc(projectByID.CreatedDate) ,
                UpdatedDate = ToUtc(projectByID.UpdatedDate),
                CreateBy = projectByID.CreateBy,
                UpdateBy = projectByID.UpdateBy,
                SecretCode = "projectByID.SecretCode",
                Mode="Update"
            };

            var existingRowList = await _context.ProjectAssignRowByIDs.FromSqlInterpolated($"EXEC ICC_GET_ProjectAssign1ByID @ID={id}").ToListAsync();
            ViewBag.RowLists = existingRowList;


            ViewBag.MainCompanyName = projectByID.CompanyName ?? "";

            var attributeList = await GetAttributeRulesAsync(tokenData);
            ViewBag.Role = attributeList.Where(a => a.AType == "Role").ToList();

            var subbranchList = await _context.ProjectAvailables
                .FromSqlInterpolated($"EXEC ICC_GET_AvailableProjectAssign1 @ID = {project.ClientID}")
                .ToListAsync();
            ViewBag.subbranchList = subbranchList;

            var supporterList = await _context.StaffListAssignResult
                .FromSqlInterpolated($"EXEC ICC_GET_StaffAllV2")
                .ToListAsync();
            ViewBag.SupporterList = supporterList;

            return View(project);
        }


        [HttpPost]
  
        public async Task<IActionResult> EditProjectAssign([FromBody] ProjectAssignHeader model)
        {
            var token = GetTokenData();
            if (token == null)
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                model.Mode = "Update";
                model.UpdateBy = token.UserId.ToString();
                model.UpdatedDate = model.FromDate;

                model.RowList?.ForEach(r => r.Status ??= "A");

                var jsonString = JsonConvert.SerializeObject(model);

                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_ProjectAssign @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "USR"),
                        new SqlParameter("@Trantype", "Edit"),
                        new SqlParameter("@EntryPrimary", model.ClientID),
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();
                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });

                return Json(new { success = false, message = result?.Message ?? "Error saving Project Assign." });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while saving." });
            }
        }

    }
}
