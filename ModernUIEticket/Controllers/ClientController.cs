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
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;

namespace ModernUIEticket.Controllers
{
    public class ClientController : Controller
    {
        private readonly TicketDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ClientController(TicketDbContext context, IWebHostEnvironment env)
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

            var clientList = await _context.ClientDtos
                .FromSqlInterpolated($@"
                    EXEC ICC_GET_CLIENT 
                    @EntryPrimary = {token.UserId}, 
                    @JsonBody = {"ALL"}")
                .ToListAsync();

            return View(clientList);
        }

        // GET: AddTicket
        public async Task<IActionResult> AddClient()
        {
            var tokenData = GetTokenData();
            // Load main branches
            var branchList = await _context.CompanyBranchResult
                .FromSqlInterpolated($"EXEC ICC_GET_Branch_MainV2 @UserSign = {tokenData.UserId}")
                .ToListAsync();

            ViewBag.MainCompany = branchList;

            var clientType = await _context.AttributeList
             .FromSqlInterpolated($"EXEC ICC_GET_AttributeRule @EntryPrimary = {tokenData.UserId},@JsonBody={""},@Type={"ClientGroup"}")
             .ToListAsync();

            ViewBag.ClientType = clientType;

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Client model)
        {
            // Decode token
            var tokenData = GetTokenData();
            string entryPrimary = tokenData?.UserId.ToString() ?? "";
            model.CreateBy ??= entryPrimary;
            // Build JSON body for SP
            var jsonBody = new
            {
                Mode = "Add", // Add / Update / Delete
                SecretCode = model.SecretCode, // -1 or null if new
                CompanyId = model.CompanyId,
                ContractNo = model.ContractNo,
                CompanyName = model.CompanyName,
                Address1 = model.Address1,
                ContactNumber = model.ContactNumber,
                Email = model.Email,
                JoinDate = model.JoinDate,
                Website = model.Website,
                KeyCode = model.KeyCode,
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                CreateBy = model.CreateBy ?? entryPrimary,
                UpdateBy = model.UpdateBy,
                Active = "A",
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                TerminationDate = model.TerminationDate,
                CompanyType = model.CompanyType,
                MainCompany = model.MainCompany,
                ClientGroup = model.ClientGroup,
                ShortName = model.ShortName
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Client @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "Client"), // ✅ fixed
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
                    return Json(new { success = false, message = result?.Message ?? "Error creating advertisement." });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                TempData["ErrorMessage"] = "Unexpected error occurred while creating advertisement.";
                return View("AddClient", model);
            }
        }


        // GET: EditClient
        public async Task<IActionResult> EditClient(int companyId)
        {
            var tokenData = GetTokenData();

            // Get client (single record)
            var client = (await _context.Clients
                .FromSqlInterpolated($@"EXEC ICC_GET_CLIENTRaw @ID = {companyId}")
                .ToListAsync())
                .FirstOrDefault();

            if (client == null)
            {
                return NotFound();
            }

            // Load main branches
            var branchList = await _context.CompanyBranchResult
                .FromSqlInterpolated($"EXEC ICC_GET_Branch_MainV2 @UserSign = {tokenData.UserId}")
                .ToListAsync();

            ViewBag.MainCompanyList = new SelectList(
                branchList.Select(b => new {
                    CompanyId = b.CompanyId,
                    DisplayName = $"{b.CompanyName} ({b.ShortName})"
                }),
                "CompanyId",
                "DisplayName",
                client.MainCompany // pre-select value
            );

            // Load client types
            var clientType = await _context.AttributeList
                .FromSqlInterpolated($"EXEC ICC_GET_AttributeRule @EntryPrimary = {tokenData.UserId},@JsonBody={""},@Type={"ClientGroup"}")
                .ToListAsync();

            ViewBag.ClientType = new SelectList(
                clientType,
                "AttributeId",
                "Description",
                client.ClientGroup // pre-select value
            );

            // Company type options (Main/Sub)
            ViewBag.CompanyTypeList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Main", Text = "Main" },
                new SelectListItem { Value = "Sub", Text = "Sub" }
            };

            return View("EditClient", client);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClient(Client model)
        {
            var tokenData = GetTokenData();
            string entryPrimary = tokenData?.UserId.ToString() ?? "";
            model.UpdateBy = entryPrimary;

            string mode = model.SecretCode == null || model.SecretCode == "-1" ? "Add" : "Update";

            var jsonBody = new
            {
                Mode = mode,
                SecretCode = model.SecretCode, // primary key for update
                CompanyId = model.CompanyId,
                ContractNo = model.ContractNo,
                CompanyName = model.CompanyName,
                Address1 = model.Address1,
                ContactNumber = model.ContactNumber,
                Email = model.Email,
                JoinDate = model.JoinDate,
                Website = model.Website,
                KeyCode = model.KeyCode,
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                CreateBy = model.CreateBy ?? entryPrimary,
                UpdateBy = model.UpdateBy,
                Active = "A",
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                TerminationDate = model.TerminationDate,
                CompanyType = model.CompanyType,
                MainCompany = model.MainCompany,
                ClientGroup = model.ClientGroup,
                ShortName = model.ShortName
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Client @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "Client"),
                        new SqlParameter("@Trantype", mode == "Add" ? "A" : "U"),
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
                    return Json(new { success = false, message = result?.Message ?? "Error saving client." });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClient(Client model)
        {
            var tokenData = GetTokenData();
            string entryPrimary = tokenData?.UserId.ToString() ?? "";
            model.UpdateBy = entryPrimary;

            string mode = model.SecretCode == null || model.SecretCode == "-1" ? "Delete" : "Delete";

            var jsonBody = new
            {
                Mode = mode,
                SecretCode = model.SecretCode, // primary key for update
                CompanyId = model.CompanyId,
                ContractNo = model.ContractNo,
                CompanyName = model.CompanyName,
                Address1 = model.Address1,
                ContactNumber = model.ContactNumber,
                Email = model.Email,
                JoinDate = model.JoinDate,
                Website = model.Website,
                KeyCode = model.KeyCode,
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                CreateBy = model.CreateBy ?? entryPrimary,
                UpdateBy = model.UpdateBy,
                Active = "A",
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                TerminationDate = model.TerminationDate,
                CompanyType = model.CompanyType,
                MainCompany = model.MainCompany,
                ClientGroup = model.ClientGroup,
                ShortName = model.ShortName
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Client @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "Client"),
                        new SqlParameter("@Trantype", mode == "Add" ? "A" : "U"),
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
                    return Json(new { success = false, message = result?.Message ?? "Error saving client." });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return Json(new { success = false, message = "Unexpected error occurred." });
            }
        }

    }
}
