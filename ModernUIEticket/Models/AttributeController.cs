// Updated controller code
using ETicketNewUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;

namespace ETicketNewUI.Controllers
{
    public class AttributeController : Controller
    {
        private readonly TicketDbContext _context;

        public AttributeController(TicketDbContext context)
        {
            _context = context;
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

            var attributes = await _context.AttributeList
                .FromSqlInterpolated($"EXEC ICC_GET_AttributeRule @EntryPrimary={token.UserId}, @JsonBody={""}, @Type={"ALL"}")
                .ToListAsync();

            return View(attributes);
        }

        public IActionResult AddAttribute()
        {
            return View(new AttributeRule());
        }

        #region Create
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create(AttributeRule model)
        {
            var token = GetTokenData();
            if (token == null)
                return Json(new { success = false, message = "Session expired. Please log in again." });

            string entryPrimary = token.UserId.ToString();

            model.CreateBy = token.UserId.ToString();
            model.UpdateBy = token.UserId.ToString();
            model.CreatedDate = DateTime.Now;
            model.UpdatedDate = DateTime.Now;
            model.Active = model.Active ?? "A";

            var jsonBody = new
            {
                Mode = "Add",
                AttributeId = -1,
                Code = model.Code,
                Description = model.Description,
                Type = model.Type,
                Active = model.Active,
                CreatedDate = model.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedDate = model.UpdatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                CreateBy = model.CreateBy,
                UpdateBy = model.UpdateBy
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                var result = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC ICC_AttributeRule @MasterType, @TranType, @EntryPrimary, @JsonBody",
                        new SqlParameter("@MasterType", "AttributeRule"),
                        new SqlParameter("@TranType", "Add"),
                        new SqlParameter("@EntryPrimary", entryPrimary),
                        new SqlParameter("@JsonBody", jsonString))
                    .AsNoTracking()
                    .ToListAsync();

                var res = result.FirstOrDefault();
                if (res != null && res.Code == 200)
                    return Json(new { success = true, message = res.Message });

                return Json(new { success = false, message = res?.Message ?? "Error saving attribute." });
            }
            catch
            {
                return Json(new { success = false, message = "Unexpected error while creating attribute." });
            }
        }

        #endregion

        public async Task<IActionResult>EditAttribute(int id)
        {
            var token = GetTokenData();
            if (token == null)
                return RedirectToAction("Login", "Logout");

            var data = await _context.AttributeRules
                .FromSqlInterpolated($"EXEC ICC_GET_AttributeByID @ID={id}")
                .ToListAsync();

            var model = data.FirstOrDefault();
            if (model == null)
                return NotFound();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAttribute(AttributeRule model)
        {
            var token = GetTokenData();
            if (token == null)
                return Json(new { success = false, message = "Session expired. Please log in again." });

            model.UpdateBy = token.UserId.ToString();
            model.UpdatedDate = DateTime.Now;

            string entryPrimary = token.UserId.ToString();

            var jsonBody = new
            {
                Mode = "Update",
                AttributeId = model.AttributeId,
                Code = model.Code,
                Description = model.Description,
                Type = model.Type,
                Active = model.Active,
                CreatedDate = model.UpdatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedDate = model.UpdatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                CreateBy = model.CreateBy,
                UpdateBy = model.UpdateBy
            };

            var jsonString = JsonConvert.SerializeObject(jsonBody);

            try
            {
                var result = await _context.Set<SpResult>()
                   .FromSqlRaw("EXEC ICC_AttributeRule @MasterType, @TranType, @EntryPrimary, @JsonBody",
                       new SqlParameter("@MasterType", "AttributeRule"),
                       new SqlParameter("@TranType", "Update"),
                       new SqlParameter("@EntryPrimary", entryPrimary),
                       new SqlParameter("@JsonBody", jsonString))
                   .AsNoTracking()
                   .ToListAsync();

                var res = result.FirstOrDefault();
                if (res != null && res.Code == 200)
                {
                    return Json(new { success = true, message = res.Message });
                }

                return Json(new { success = false, message = res?.Message ?? "Error updating attribute." });
            }
            catch (Exception ex)
            {
                // Optional: log the exception
                return Json(new { success = false, message = "Unexpected error while updating attribute." });
            }
        }

    }
}