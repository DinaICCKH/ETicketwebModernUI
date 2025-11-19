using ETicketNewUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;

namespace ModernUIEticket.Controllers
{
    public class AdvertiseController : Controller
    {

        private readonly TicketDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdvertiseController(TicketDbContext context, IWebHostEnvironment env)
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

            var adsList = await _context.AdvertisementDtos
                .FromSqlInterpolated($@"
            EXEC ICC_GET_ADVERTISEMENT 
            @EntryPrimary = {"ALL"}, 
            @JsonBody = {"ALL"}")
                .ToListAsync();

            return View(adsList);
        }


        // GET: AddAdvertise
        public async Task<IActionResult>AddAdvertise()
        {
            var tokenData = GetTokenData();
            bool isClientType = tokenData?.Type == "Client";

            ViewBag.IsClientType = isClientType;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Advertisement model, IFormFile Attachment)
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
                    return View("AddAdvertise", model);
                }

                if (Attachment.Length > 50 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "File size exceeds 50MB limit.";
                    return View("AddAdvertise", model);
                }

                var sanitizedFileName = System.Text.RegularExpressions.Regex.Replace(
                    Path.GetFileNameWithoutExtension(Attachment.FileName),
                    @"[^a-zA-Z0-9_-]", "_");

                var newFileName = $"{model.CreateBy}_{DateTime.Now:dd_MM_yyyy_HH_mm_ss}_{sanitizedFileName}{fileExt}";

                // Set uploads folder under wwwroot
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "UploadedFiles");

                // Create folder if it doesn't exist
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, newFileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await Attachment.CopyToAsync(stream);

                // Save the relative path to the database
                model.Picture = "/UploadedFiles/" + newFileName;
            }

            // Build JSON body for SP
            var jsonBody = new
            {
                Mode = "Add", // Add / Update / Delete
                ID = model.ID, // -1 or null if new
                Picture = model.Picture,
                ShowPictureStatus = model.ShowPictureStatus,
                TextDes = model.TextDes,
                ShowTextDesStatus = model.ShowTextDesStatus,
                FromDate = model.FromDate.ToString("yyyy-MM-dd HH:mm:ss"),
                ToDate = model.ToDate.ToString("yyyy-MM-dd HH:mm:ss"),
                Status = "A",
                CreateBy = model.CreateBy ?? entryPrimary,
                UpdateBy = model.UpdateBy,
                CreateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Advertisement @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "Advertisement"), // ✅ fixed
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
                return View("AddAdvertise", model);
            }
        }


        // GET: Edit Advertisement
        public async Task<IActionResult> EditAdvertise(int id)
        {
            var tokenData = GetTokenData();
            if (tokenData == null) return RedirectToAction("Login", "Account");

            // Load Advertisement by ID (you may use EF or SP)

            // Get main user info
            var advert = (await _context.Advertisements
                .FromSqlInterpolated($"EXEC ICC_GET_ADVERTISEMENTByID @EntryPrimary = {id}")
                .AsNoTracking()
                .ToListAsync())
                .FirstOrDefault();

            if (advert == null) return NotFound();

            return View("EditAdvertise", advert); // now passes single Advertisement

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Advertisement model, IFormFile Attachment)
        {
            var tokenData = GetTokenData();
            string entryPrimary = tokenData?.UserId.ToString() ?? "";
            model.CreateBy ??= entryPrimary;

            // Handle file upload
            if (Attachment != null && Attachment.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExt = Path.GetExtension(Attachment.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExt))
                {
                    TempData["ErrorMessage"] = "Invalid file type. Allowed: images only.";
                    return View("AddAdvertise", model);
                }

                if (Attachment.Length > 50 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "File size exceeds 50MB limit.";
                    return View("AddAdvertise", model);
                }

                var sanitizedFileName = System.Text.RegularExpressions.Regex.Replace(
                    Path.GetFileNameWithoutExtension(Attachment.FileName),
                    @"[^a-zA-Z0-9_-]", "_");

                var newFileName = $"{model.CreateBy}_{DateTime.Now:dd_MM_yyyy_HH_mm_ss}_{sanitizedFileName}{fileExt}";
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "UploadedFiles");

                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, newFileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await Attachment.CopyToAsync(stream);

                model.Picture = "/UploadedFiles/" + newFileName;
            }

            // Determine mode
            string mode = model.ID > 0 ? "Update" : "Add";

            var jsonBody = new
            {
                Mode = mode,
                ID = model.ID,
                Picture = model.Picture,
                ShowPictureStatus = model.ShowPictureStatus,
                TextDes = model.TextDes,
                ShowTextDesStatus = model.ShowTextDesStatus,
                FromDate = model.FromDate.ToString("yyyy-MM-dd HH:mm:ss"),
                ToDate = model.ToDate.ToString("yyyy-MM-dd HH:mm:ss"),
                Status = model.Status,
                CreateBy = model.CreateBy ?? entryPrimary,
                UpdateBy = entryPrimary,
                CreateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Advertisement @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "Advertisement"),
                        new SqlParameter("@Trantype", "A"),
                        new SqlParameter("@EntryPrimary", entryPrimary ?? (object)DBNull.Value),
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();
                return Json(new
                {
                    success = result?.Code == 200,
                    message = result?.Message ?? "Error processing advertisement."
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                TempData["ErrorMessage"] = "Unexpected error occurred.";
                return View("AddAdvertise", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Advertisement model)
        {
            var tokenData = GetTokenData();
            string entryPrimary = tokenData?.UserId.ToString() ?? "";
            model.CreateBy ??= entryPrimary;

            // Determine mode
            string mode = model.ID > 0 ? "Delete" : "Add";

            var jsonBody = new
            {
                Mode = mode,
                ID = model.ID,
                Picture = model.Picture,
                ShowPictureStatus = model.ShowPictureStatus,
                TextDes = model.TextDes,
                ShowTextDesStatus = model.ShowTextDesStatus,
                FromDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ToDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Status = model.Status,
                CreateBy = model.CreateBy ?? entryPrimary,
                UpdateBy = entryPrimary,
                CreateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                var spResults = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC dbo.ICC_Advertisement @MasterType, @Trantype, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "Advertisement"),
                        new SqlParameter("@Trantype", "A"),
                        new SqlParameter("@EntryPrimary", entryPrimary ?? (object)DBNull.Value),
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                var result = spResults.FirstOrDefault();
                return Json(new
                {
                    success = result?.Code == 200,
                    message = result?.Message ?? "Error processing advertisement."
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                TempData["ErrorMessage"] = "Unexpected error occurred.";
                return View("AddAdvertise", model);
            }
        }

    }
}
