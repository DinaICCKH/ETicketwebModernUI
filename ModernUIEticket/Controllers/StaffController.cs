using ETicketNewUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;

namespace ModernUIEticket.Controllers
{
    public class StaffController : Controller
    {
        private readonly TicketDbContext _context;
        private readonly IWebHostEnvironment _env;

        public StaffController(TicketDbContext context, IWebHostEnvironment env)
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

            var List = await _context.StaffResults
                .FromSqlInterpolated($@"
                    EXEC ICC_GET_STAFF 
                    @EntryPrimary = {token.UserId},
                    @JsonBody = {"ALL"}")
                .ToListAsync();

            return View(List);
        }


        // GET: AddTicket
        public async Task<IActionResult> AddStaff()
        {
            var tokenData = GetTokenData();
            // Load main branches

            var userList = await _context.UserStaffs
               .FromSqlInterpolated($"EXEC ICC_GET_UserForStaff")
               .ToListAsync();
            ViewBag.UserList = userList;

            var attributeList = await GetAttributeRulesAsync(tokenData);
            ViewBag.Role = attributeList.Where(a => a.AType == "Position").ToList();


            return View();
        }

        #region Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Staff model)
        {
            var tokenData = GetTokenData();
            if (tokenData == null)
            {
                return Json(new { success = false, message = "Session expired. Please log in again." });
            }

            string entryPrimary = tokenData.UserId.ToString();

            // Default values
            model.CreateBy ??= tokenData.UserId.ToString();
            model.UpdateBy ??= tokenData.UserId.ToString();
            model.CreatedDate ??= DateTime.Now;
            model.UpdatedDate ??= DateTime.Now;
            model.Active ??= "A";

            // JSON body for SP
            var jsonBody = new
            {
                Mode = "Add",
                SecretCode = model.SecretCode ?? "",
                StaffId = model.StaffId != 0 ? model.StaffId : -1,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Gender = model.Gender,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                ProfilePicture = model.ProfilePicture,
                WorkExperience = model.WorkExperience,
                Position = model.Position,
                HireDate = model.HireDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                CreatedDate = model.CreatedDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedDate = model.UpdatedDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                CreateBy = model.CreateBy,
                UpdateBy = model.UpdateBy,
                Active = model.Active,
                UserID = model.UserID,
                Alert = model.Alert
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Staff @MasterType, @TranType, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "Staff"),
                        new SqlParameter("@TranType", "Add"),
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
                    return Json(new { success = false, message = result?.Message ?? "Error creating staff record." });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while creating staff record." });
            }
        }
        #endregion


        public async Task<IActionResult> EditStaff(int ID)
        {
            var tokenData = GetTokenData();
            // Load main branches

            var userList = await _context.UserStaffs
               .FromSqlInterpolated($"EXEC ICC_GET_UserForStaffUpdate")
               .ToListAsync();
            ViewBag.UserList = userList;

            var attributeList = await GetAttributeRulesAsync(tokenData);
            ViewBag.Role = attributeList.Where(a => a.AType == "Position").ToList();

            var existingData = await _context.Staffs
            .FromSqlInterpolated($"EXEC ICC_GET_StaffByID @UserID={tokenData.UserId}, @StaffID={ID}")
            .ToListAsync();


            // Pass only the first item to the view
            return View(existingData.FirstOrDefault());


           

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStaff(Staff model)
        {
            var tokenData = GetTokenData();
            if (tokenData == null)
                return Json(new { success = false, message = "Session expired. Please log in again." });

            string entryPrimary = tokenData.UserId.ToString();

            model.UpdateBy = tokenData.UserId.ToString();
            model.UpdatedDate = DateTime.Now;

            var jsonBody = new
            {
                Mode = "Update",
                SecretCode = model.SecretCode ?? "",
                StaffId = model.StaffId,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Gender = model.Gender,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                ProfilePicture = model.ProfilePicture,
                WorkExperience = model.WorkExperience,
                Position = model.Position,
                HireDate = model.HireDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                CreatedDate = model.CreatedDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedDate = model.UpdatedDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                CreateBy = model.CreateBy,
                UpdateBy = model.UpdateBy,
                Active = model.Active,
                UserID = model.UserID,
                Alert = model.Alert
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Staff @MasterType, @TranType, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "Staff"),
                        new SqlParameter("@TranType", "Update"),
                        new SqlParameter("@EntryPrimary", entryPrimary),
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();
                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });
                else
                    return Json(new { success = false, message = result?.Message ?? "Error updating staff." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Unexpected error occurred while updating staff." });
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStaff(Staff model)
        {
            var tokenData = GetTokenData();
            if (tokenData == null)
                return Json(new { success = false, message = "Session expired. Please log in again." });

            string entryPrimary = tokenData.UserId.ToString();

            model.UpdateBy = tokenData.UserId.ToString();
            model.UpdatedDate = DateTime.Now;

            var jsonBody = new
            {
                Mode = "Delete",
                SecretCode = model.SecretCode ?? "",
                StaffId = model.StaffId,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Gender = model.Gender,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                ProfilePicture = model.ProfilePicture,
                WorkExperience = model.WorkExperience,
                Position = model.Position,
                HireDate = model.HireDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                CreatedDate = model.CreatedDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedDate = model.UpdatedDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                CreateBy = model.CreateBy,
                UpdateBy = model.UpdateBy,
                Active = model.Active,
                UserID = model.UserID,
                Alert = model.Alert
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Staff @MasterType, @TranType, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "Staff"),
                        new SqlParameter("@TranType", "Update"),
                        new SqlParameter("@EntryPrimary", entryPrimary),
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();
                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });
                else
                    return Json(new { success = false, message = result?.Message ?? "Error deleting staff." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Unexpected error occurred while deleting staff." });
            }
        }


        private async Task<List<TicketAttribute>> GetAttributeRulesAsync(LoginResultDto tokenData)
        {
            if (tokenData == null) return new List<TicketAttribute>();

            return await _context.Attributes
                .FromSqlRaw("EXEC ICC_GET_AttributeRule @EntryPrimary, @JsonBody, @Type",
                    new SqlParameter("@EntryPrimary", "-1"),
                    new SqlParameter("@JsonBody", ""),
                    new SqlParameter("@Type", "Position"))
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
