using Microsoft.AspNetCore.Mvc;
using ETicketNewUI.Models;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ModernUIEticket.Controllers
{
    public class UserController : Controller
    {
        private readonly TicketDbContext _context;
        private readonly IWebHostEnvironment _env;

        public UserController(TicketDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        #region Helper: Get Token
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

        #region Index
        public async Task<IActionResult> Index()
        {
            var token = GetTokenData();
            if (token == null) return RedirectToAction("Login", "Logout");

            var userList = await _context.userListResults
                .FromSqlInterpolated($@"
                    EXEC ICC_GET_USER 
                    @EntryPrimary = {token.UserId + token.UserName + token.CompanyId + token.UserName + token.UserId}, 
                    @JsonBody = {"ALL"}")
                .ToListAsync();

            return View(userList);
        }
        #endregion

        #region Get User List (API)
        [HttpGet("api/getuserlist")]
        public async Task<IActionResult> GetUserListApi(
            [FromHeader(Name = "UserId")] string userId,
            [FromHeader(Name = "UserName")] string userName,
            [FromHeader(Name = "CompanyId")] string companyId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(companyId))
                return BadRequest(new { Message = "Missing one or more headers: UserId, UserName, CompanyId." });

            try
            {
                // Build the same EntryPrimary as before
                string entryPrimary = $"{userId}{userName}{companyId}{userName}{userId}";

                var userList = await _context.userListResults
                            .FromSqlInterpolated($@"
                  EXEC ICC_GET_USER 
                  @EntryPrimary = {entryPrimary}, 
                  @JsonBody = {"ALL"}")
                            .AsNoTracking()
                            .ToListAsync();

                return Ok(userList);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in GetUserListApi: " + ex.Message);
                return StatusCode(500, new { Message = "Unexpected error occurred while fetching user list." });
            }
        }
        #endregion

        #region Get Branches (API) via Header
        [HttpGet("api/getbranchesforusermodule")]
        public async Task<IActionResult> GetBranchesForUserModule([FromHeader(Name = "CompanyId")] int companyId)
        {
            try
            {
                var subbranchList = await _context.CompanyBranchResult
                    .FromSqlInterpolated($"EXEC ICC_GET_Branch_SubV2 @CompanyID = {companyId}")
                    .AsNoTracking()
                    .ToListAsync();

                if (subbranchList == null || subbranchList.Count == 0)
                    return NotFound(new { Message = "No branches found for this company." });

                return Ok(subbranchList);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in GetBranchesForUserModule: " + ex.Message);
                return StatusCode(500, new { Message = "Unexpected error occurred while fetching branches." });
            }
        }
        #endregion



        #region Add User (API)
        [HttpGet("api/supportingFiedladduser")]
        public async Task<IActionResult> AddUserApi([FromHeader(Name = "UserId")] int userId)
        {
            try
            {
                // Prepare user model
                var userModel = new user(); // empty model for "Add" operation

                // Get branches for the user
                var branchList = await _context.CompanyBranchResult
                    .FromSqlInterpolated($"EXEC ICC_GET_Branch_MainV2 @UserSign = {userId}")
                    .AsNoTracking()
                    .ToListAsync();

                // Prepare type list
                var typeList = new List<object>
                 {
                     new { Value = "Admin", Text = "Admin" },
                     new { Value = "Manager", Text = "Manager" },
                     new { Value = "Moderator", Text = "Moderator" },
                     new { Value = "Supporter", Text = "Supporter" },
                     new { Value = "Client", Text = "Client" }
                 };

                // Prepare super user list
                var superUserList = new List<object>
             {
                 new { Value = "Y", Text = "Yes" },
                 new { Value = "N", Text = "No" }
             };

                // Return everything as JSON
                return Ok(new
                {
                    BranchList = branchList,
                    TypeList = typeList,
                    SuperUserList = superUserList
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in AddUserApi: " + ex.Message);
                return StatusCode(500, new { Message = "Unexpected error occurred while preparing Add User data." });
            }
        }
        #endregion

        #region Add User (GET)
        [HttpGet]
        public async Task<IActionResult> AddUser(int? id)
        {
            var token = GetTokenData();
            if (token == null) return RedirectToAction("Login", "Logout");

            var userModel = new user();

            var branchList = await _context.CompanyBranchResult
                .FromSqlInterpolated($"EXEC ICC_GET_Branch_MainV2 @UserSign = {token.UserId}")
                .ToListAsync();

            ViewBag.Branch = branchList;


            if (token.Type == "Client")
            {
                ViewBag.TypeList = new List<SelectListItem>
                {
                    new("Client", "Client")
                };

                ViewBag.SuperUserList = new List<SelectListItem>
                {
                    new("N", "No")
                };

            }
            else
            {
                ViewBag.TypeList = new List<SelectListItem>
                {
                    new("Admin", "Admin"),
                    new("Manager", "Manager"),
                    new("Moderator", "Moderator"),
                    new("Supporter", "Supporter"),
                    new("Client", "Client")
                };

                ViewBag.SuperUserList = new List<SelectListItem>
                {
                    new("Y", "Yes"),
                    new("N", "No")
                };
            }
            return View(userModel);
        }
        #endregion

        #region Get SubBranches
        [HttpPost]
        public async Task<JsonResult> GetBranches(int companyId)
        {
            var subbranchList = await _context.CompanyBranchResult
                .FromSqlInterpolated($"EXEC ICC_GET_Branch_SubV2 @CompanyID = {companyId}")
                .ToListAsync();

            return Json(subbranchList);
        }
        #endregion

        #region Add User (POST)
        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] user model)
        {
            var token = GetTokenData();
            if (token == null) return Json(new { success = false, message = "Unauthorized" });

            if (model.PasswordHash != model.ConfirmPassword)
                return Json(new { success = false, message = "Password and Confirm Password do not match." });

            if (!string.IsNullOrEmpty(model.PasswordHash))
                model.PasswordHash = HashPassword(model.PasswordHash);

            model.Mode ??= "Add";
            model.CreateBy ??= token.UserId.ToString();
            model.UpdateBy ??= token.UserId.ToString();
            model.CreateDate ??= DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            model.UpdateDate ??= DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            model.UStatus ??= "A";
            model.ULock = "N";

            model.SelectedBranches?.ForEach(b => b.Assign ??= "N");

            var jsonString = JsonConvert.SerializeObject(model);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw(
                        "EXEC dbo.ICC_User @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "USR"),
                        new SqlParameter("@Trantype", "Add"),
                        new SqlParameter("@EntryPrimary", model.UserName ?? "0"),
                        new SqlParameter("@JsonBody", jsonString)
                    )
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();

                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });

                return Json(new { success = false, message = result?.Message ?? "Error saving user." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while saving user." });
            }
        }
        #endregion


        #region Add User (API)
        [HttpPost("api/adduser")]
        public async Task<IActionResult> AddUserApi(
            [FromHeader(Name = "UserId")] string userId,
            [FromHeader(Name = "UserName")] string userName,
            [FromHeader(Name = "CompanyId")] string companyId,
            [FromBody] user model)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(companyId))
                return Unauthorized(new { success = false, message = "Missing header parameters." });

            try
            {
                // Password validation
                if (!string.IsNullOrEmpty(model.PasswordHash) && model.PasswordHash != model.ConfirmPassword)
                    return BadRequest(new { success = false, message = "Password and Confirm Password do not match." });

                // Hash password if provided
                if (!string.IsNullOrEmpty(model.PasswordHash))
                    model.PasswordHash = HashPassword(model.PasswordHash);

                // Set defaults
                model.Mode ??= "Add";
                model.CreateBy ??= userId;
                model.UpdateBy ??= userId;
                model.CreateDate ??= DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                model.UpdateDate ??= DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                model.UStatus ??= "A";
                model.ULock = "N";

                // Ensure branches have default Assign
                model.SelectedBranches?.ForEach(b => b.Assign ??= "N");

                var jsonString = JsonConvert.SerializeObject(model);

                // Call stored procedure
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw(
                        "EXEC dbo.ICC_User @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "USR"),
                        new SqlParameter("@Trantype", "Add"),
                        new SqlParameter("@EntryPrimary", model.UserName ?? "0"),
                        new SqlParameter("@JsonBody", jsonString)
                    )
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();

                if (result != null && result.Code == 200)
                    return Ok(new { success = true, message = result.Message });

                return BadRequest(new { success = false, message = result?.Message ?? "Error saving user." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in AddUserApi: " + ex.Message);
                return StatusCode(500, new { success = false, message = "Unexpected error occurred while saving user." });
            }
        }
        #endregion




        #region Edit User (GET)
        [HttpGet]
        public async Task<IActionResult> EditUser(string userid)
        {
            var token = GetTokenData();
            if (token == null) return RedirectToAction("Login", "Logout");

            var existingUser = (await _context.userListResults
                .FromSqlInterpolated($"EXEC ICC_GET_USER @EntryPrimary = {userid}, @JsonBody = {"ByUser"}")
                .AsNoTracking()
                .ToListAsync())
                .FirstOrDefault();

            if (existingUser == null) return RedirectToAction("Index");

            var userModel = new user
            {
                UserName = existingUser.UserName,
                UserFullName = existingUser.UserFullName,
                Telephone = existingUser.Telephone,
                Email = existingUser.Email,
                Type = existingUser.Type,
                FullAuthorization = existingUser.FullAuthorization,
                CompanyId = existingUser.CompanyId.ToString(),
                UStatus = existingUser.UStatus,
                ULock = existingUser.ULock,
                Mode = "Update"
            };

            var userBranches = await _context.UserBranchResult
                .FromSqlInterpolated($"EXEC ICC_GET_USER1 {existingUser.UserName}")
                .AsNoTracking()
                .ToListAsync();

            userModel.SelectedBranches = userBranches
                .Select(b => new user1
                {
                    CompanyId = b.CompanyId.ToString(),
                    Name = b.Name,
                    Assign = b.Assign
                })
                .ToList();

            ViewBag.CompanyName = existingUser.CompanyName;
            ViewBag.UserType = token.Type;
            ViewBag.FullAuthorize = token.FullAuthorization;
            ViewBag.User = token.UserId;

            ViewBag.TypeList = new List<SelectListItem>
            {
                new("Admin", "Admin"),
                new("Manager", "Manager"),
                new("Moderator", "Moderator"),
                new("Supporter", "Supporter"),
                new("Client", "Client")
            };

            ViewBag.SuperUserList = new SelectList(new[]
  {
    new { Value = "Y", Text = "Yes" },
    new { Value = "N", Text = "No" }
}, "Value", "Text", existingUser.FullAuthorization ?? "N");

            ViewBag.LastLogin = existingUser.LastLoginDate;

            return View("EditUser", userModel);
        }
        #endregion

        #region Edit User (POST)
        [HttpPost]
        public async Task<IActionResult> EditUser([FromBody] user model)
        {
            var token = GetTokenData();
            if (token == null) return Json(new { success = false, message = "Unauthorized" });

            if (!string.IsNullOrEmpty(model.PasswordHash) && model.PasswordHash != model.ConfirmPassword)
                return Json(new { success = false, message = "Password and Confirm Password do not match." });

            if (!string.IsNullOrEmpty(model.PasswordHash))
                model.PasswordHash = HashPassword(model.PasswordHash);
            else
                model.PasswordHash = null;

            model.Mode = "Update";
            model.UpdateBy = token.UserId.ToString();
            model.UpdateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            model.SelectedBranches?.ForEach(b => b.Assign ??= "N");

            if (model.FullAuthorization == "Yes")
            {
                model.FullAuthorization = "Y";
            }
            else {
                model.FullAuthorization = "N";
            }

            if (model.ULock == "Yes" || model.ULock=="Y")
            {
                model.ULock = "Y";
            }
            else
            {
                model.ULock = "N";
            }

            var jsonString = JsonConvert.SerializeObject(model);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw(
                        "EXEC dbo.ICC_User @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "USR"),
                        new SqlParameter("@Trantype", "Update"),
                        new SqlParameter("@EntryPrimary", model.UserName ?? "0"),
                        new SqlParameter("@JsonBody", jsonString)
                    )
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();

                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });

                return Json(new { success = false, message = result?.Message ?? "Error updating user." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while updating user." });
            }
        }
        #endregion


        #region Get User Info (API) - Header Only
        [HttpGet("api/getedituserbyid")]
        public async Task<IActionResult> GetUserInfoApi(
            [FromHeader(Name = "UserId")] string userIdHeader,
            [FromHeader(Name = "UserName")] string userNameHeader,
            [FromHeader(Name = "CompanyId")] string companyIdHeader)
        {
            if (string.IsNullOrEmpty(userIdHeader) || string.IsNullOrEmpty(userNameHeader) || string.IsNullOrEmpty(companyIdHeader))
                return Unauthorized(new { success = false, message = "Missing header parameters." });

            try
            {
                // Use the UserName header as the parameter for the stored procedure
                // Build the entry string just like you did with the token
                string entryPrimary = $"{userIdHeader}{userNameHeader}{companyIdHeader}{userNameHeader}{userIdHeader}";

                var existingUser = (await _context.userListResults
                    .FromSqlInterpolated($@"EXEC ICC_GET_USER 
                @EntryPrimary = {entryPrimary}, 
                @JsonBody = {"ByUser"}")
                    .AsNoTracking()
                    .ToListAsync())
                    .FirstOrDefault();

                if (existingUser == null)
                    return NotFound(new { success = false, message = "User not found." });

                var userBranches = await _context.UserBranchResult
                    .FromSqlInterpolated($"EXEC ICC_GET_USER1 {existingUser.UserName}")
                    .AsNoTracking()
                    .ToListAsync();

                var userModel = new user
                {
                    UserName = existingUser.UserName,
                    UserFullName = existingUser.UserFullName,
                    Telephone = existingUser.Telephone,
                    Email = existingUser.Email,
                    Type = existingUser.Type,
                    FullAuthorization = existingUser.FullAuthorization,
                    CompanyId = existingUser.CompanyId.ToString(),
                    UStatus = existingUser.UStatus,
                    ULock = existingUser.ULock,
                    Mode = "Update",
                    SelectedBranches = userBranches.Select(b => new user1
                    {
                        UserId=b.UserId.ToString(),
                        CompanyId = b.CompanyId.ToString(),
                        Name = b.Name,
                        Assign = b.Assign,
                        LineNum=b.LineNum.ToString()
                    }).ToList()
                };

                return Ok(new { success = true, user = userModel });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in GetUserInfoApi: " + ex.Message);
                return StatusCode(500, new { success = false, message = "Unexpected error occurred while fetching user info." });
            }
        }
        #endregion


        #region Add User (API)
        [HttpPost("api/UpdateUserApi")]
        public async Task<IActionResult> UpdateUserApi(
            [FromHeader(Name = "UserId")] string userIdHeader,
            [FromHeader(Name = "UserName")] string userNameHeader,
            [FromHeader(Name = "CompanyId")] string companyIdHeader,
            [FromBody] user model)
        {
            // Validate headers
            if (string.IsNullOrEmpty(userIdHeader) || string.IsNullOrEmpty(userNameHeader) || string.IsNullOrEmpty(companyIdHeader))
                return Unauthorized(new { success = false, message = "Missing header parameters." });

            try
            {
                // Validate password
                if (model.PasswordHash != model.ConfirmPassword)
                    return BadRequest(new { success = false, message = "Password and Confirm Password do not match." });

                if (!string.IsNullOrEmpty(model.PasswordHash))
                    model.PasswordHash = HashPassword(model.PasswordHash);

                // Set default values for Add
                model.Mode = "Update";
                model.UpdateBy = userIdHeader;
                model.CreateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                model.UpdateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                model.UStatus ??= "A";
                model.ULock = "N";
                model.SelectedBranches?.ForEach(b => b.Assign ??= "N");

                // Serialize to JSON for SP
                var jsonString = JsonConvert.SerializeObject(model);

                // Call stored procedure
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw(
                        "EXEC dbo.ICC_User @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "USR"),
                        new SqlParameter("@Trantype", "Add"),
                        new SqlParameter("@EntryPrimary", model.UserName ?? "0"),
                        new SqlParameter("@JsonBody", jsonString)
                    )
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();

                if (result != null && result.Code == 200)
                    return Ok(new { success = true, message = result.Message });

                return BadRequest(new { success = false, message = result?.Message ?? "Error saving user." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in AddUserApi: " + ex.Message);
                return StatusCode(500, new { success = false, message = "Unexpected error occurred while saving user." });
            }
        }
        #endregion



        #region Delete User
        [HttpPost]
        public async Task<IActionResult> DeleteUser([FromBody] user model)
        {
            var token = GetTokenData();
            if (token == null) return Json(new { success = false, message = "Unauthorized" });

            model.Mode = "Delete";
            model.UpdateBy = token.UserId.ToString();
            model.UpdateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var jsonString = JsonConvert.SerializeObject(model);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw(
                        "EXEC dbo.ICC_User @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "USR"),
                        new SqlParameter("@Trantype", "Delete"),
                        new SqlParameter("@EntryPrimary", token.UserId),
                        new SqlParameter("@JsonBody", jsonString)
                    )
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();

                if (result != null && result.Code == 200)
                    return Json(new { success = true, message = result.Message });

                return Json(new { success = false, message = result?.Message ?? "Error deleting user." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred while deleting user." });
            }
        }
        #endregion

        #region Delete User (API)
        [HttpPost("api/deleteuser")]
        public async Task<IActionResult> DeleteUserApi(
            [FromHeader(Name = "UserId")] string userIdHeader,
            [FromHeader(Name = "UserName")] string userNameHeader,
            [FromHeader(Name = "CompanyId")] string companyIdHeader,
            [FromBody] user model)
        {
            // Validate headers
            if (string.IsNullOrEmpty(userIdHeader) || string.IsNullOrEmpty(userNameHeader) || string.IsNullOrEmpty(companyIdHeader))
                return Unauthorized(new { success = false, message = "Missing header parameters." });

            try
            {
                // Prepare model for deletion
                model.Mode = "Delete";
                model.UpdateBy = userIdHeader;
                model.UpdateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                model.SelectedBranches?.ForEach(b => b.Assign ??= "N");

                var jsonString = JsonConvert.SerializeObject(model);

                // Call stored procedure
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw(
                        "EXEC dbo.ICC_User @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "USR"),
                        new SqlParameter("@Trantype", "Delete"),
                        new SqlParameter("@EntryPrimary", model.UserName ?? "0"),
                        new SqlParameter("@JsonBody", jsonString)
                    )
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();

                if (result != null && result.Code == 200)
                    return Ok(new { success = true, message = result.Message });

                return BadRequest(new { success = false, message = result?.Message ?? "Error deleting user." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in DeleteUserApi: " + ex.Message);
                return StatusCode(500, new { success = false, message = "Unexpected error occurred while deleting user." });
            }
        }
        #endregion


        #region Password Hash
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new();
                foreach (var b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        public bool VerifyPassword(string inputPassword, string storedHash)
        {
            string hashedInput = HashPassword(inputPassword);
            return hashedInput == storedHash;
        }
        #endregion
    }
}
