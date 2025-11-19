using Microsoft.AspNetCore.Mvc;
using ETicketNewUI.Models;
using QRCoder;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.IdentityModel.Abstractions;
using System.Text.RegularExpressions;
using System.Globalization;

namespace ETicketNewUI.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly TicketDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AttendanceController(TicketDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ===================== ADMIN SIDE =====================

        public async Task<IActionResult> Index(
            string status = "Planned,Ongoing",
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            int pageIndex = 0,
            int pageSize = 500000000)
        {
            var token = GetTokenData();
            if (token == null) return RedirectToAction("Login", "Logout");

            var sessions = await _context.AttendanceHeaderResults
                .FromSqlInterpolated($@"
            EXEC ICC_GET_AttendanceList 
                @UserId = {token.UserId}, 
                @Status = {status}, 
                @DateFrom = {dateFrom}, 
                @DateTo = {dateTo}, 
                @PageIndex = {pageIndex}, 
                @PageSize = {pageSize}")
                .ToListAsync();

            return View(sessions);
        }



        // Create a new session (header only)
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var tokenData = GetTokenData();
            if (tokenData == null)
                return RedirectToAction("Login", "Logout");

            ViewBag.BranchClientList = await GetBranchClientListAsync(tokenData);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(AttendanceHeader model)
        {
            // Decode token
            var tokenData = GetTokenData();
            string user = tokenData?.UserId.ToString() ?? "Admin";

            decimal latitude = string.IsNullOrEmpty(model.Latitude?.ToString())
               ? 0
               : Convert.ToDecimal(model.Latitude);

            decimal longitude = string.IsNullOrEmpty(model.Longitude?.ToString())
                ? 0
                : Convert.ToDecimal(model.Longitude);


            // Set audit info
            model.CreatedBy = user;
            model.CreatedDate = DateTime.Now;
            model.UpdatedBy = model.CreatedBy;
            model.UpdatedDate = DateTime.Now;
            model.Latitude = latitude;
            model.Longitude = longitude;

            // Save header first to get AttendanceID
            _context.AttendanceHeaders.Add(model);
            await _context.SaveChangesAsync();

            // Format Latitude and Longitude
            string lat = latitude.ToString("0.00") + "0000";
            string lng = longitude.ToString("0.00") + "0000";

            // Generate unique QR code link
            model.QrCodeLink = $"{Request.Scheme}://{Request.Host}/Eticket/Attendance/CheckIn?DocEntry={model.AttendanceID.ToString()+model.CreatedBy.ToString()+model.ProjectID.ToString()+model.Status.ToString()+lat+lng}";

            // Generate QR Code image
            string qrFolder = Path.Combine(_env.WebRootPath, "qrcodes");
            Directory.CreateDirectory(qrFolder);

            string fileName = $"session_{model.AttendanceID}.png";
            string filePath = Path.Combine(qrFolder, fileName);

            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(model.QrCodeLink, QRCodeGenerator.ECCLevel.Q);
                using (QRCode qrCode = new QRCode(qrCodeData))
                using (Bitmap qrCodeImage = qrCode.GetGraphic(20))
                {
                    qrCodeImage.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                }
            }

            model.QrCodeImagePath = "/Eticket/qrcodes/" + fileName;

            // Update header with QR code info
            _context.AttendanceHeaders.Update(model);
            await _context.SaveChangesAsync();

            // ✅ Mark success so SweetAlert will trigger
            TempData["Created"] = true;

            // Redirect back to Create (not Index!)
            return RedirectToAction("Create");
        }


        // GET: Edit Attendance
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var tokenData = GetTokenData();
            if (tokenData == null) return RedirectToAction("Login", "Eticket/Logout");

            // Get header
            var header = await _context.AttendanceHeaders.FindAsync(id);
            if (header == null) return NotFound();

            // Get all details for this AttendanceID
            var details = await _context.AttendanceDetails
                                        .Where(d => d.AttendanceID == id)
                                        .ToListAsync();

            // If no details exist, initialize an empty list
            if (details == null || !details.Any())
            {
                details = new List<AttendanceDetail>();
            }

            // If no details exist, create an empty list
            details ??= new List<AttendanceDetail>();

            // Now you can safely use details in your view or further processing



            ViewBag.BranchClientList = await GetBranchClientListAsync(tokenData);

            // Pass details via ViewBag or a view model
            ViewBag.AttendanceDetails = details;

            return View(header);
        }

        // GET: Delete Attendance Detail
        [HttpGet]
        public async Task<IActionResult> DeleteDetail(int id)
        {
            var detail = await _context.AttendanceDetails.FindAsync(id);
            if (detail == null) return NotFound();

            _context.AttendanceDetails.Remove(detail);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Participant removed successfully!";
            // Redirect back to Edit page for the header
            return RedirectToAction("Edit", new { id = detail.AttendanceID });
        }


        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            // Get header
            var header = await _context.AttendanceHeaders.FirstOrDefaultAsync(h => h.AttendanceID == id);
            if (header == null) return NotFound();

            // Get all related details
            var details = await _context.AttendanceDetails
                                        .Where(d => d.AttendanceID == id)
                                        .ToListAsync();

            // Delete details first
            if (details.Any())
                _context.AttendanceDetails.RemoveRange(details);

            // Delete header
            _context.AttendanceHeaders.Remove(header);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Attendance session and all participants deleted successfully!";
            return RedirectToAction("Index");
        }


        // POST: Edit Attendance
        [HttpPost]
        public async Task<IActionResult> Edit(int id, AttendanceHeader model)
        {
            var tokenData = GetTokenData();
            string user = tokenData?.UserId.ToString() ?? "Admin";

            var header = await _context.AttendanceHeaders.FindAsync(id);
            if (header == null) return NotFound();

            // Update editable fields only (do NOT touch QR)
            header.Remark  = model.Remark;
            header.ProjectID = model.ProjectID;
            header.Status = model.Status;
            header.TrainingDate = model.TrainingDate;
            header.StartDateTime = model.StartDateTime;
            header.EndDateTime = model.EndDateTime;
            header.Latitude = model.Latitude;
            header.Longitude = model.Longitude;
            header.UpdatedBy = user;
            header.UpdatedDate = DateTime.Now;

            _context.AttendanceHeaders.Update(header);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
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

        // ===================== PARTICIPANT SIDE =====================

        [HttpGet]
        public IActionResult CheckIn(string DocEntry)  // id = AttendanceID
        {
            var header = _context.AttendanceHeaders.FirstOrDefault(x => x.AttendanceID.ToString()+x.CreatedBy.ToString()+x.ProjectID.ToString()+x.Status.ToString() + x.Latitude.ToString() + x.Longitude.ToString() == DocEntry);
            if (header == null)
                return NotFound();

            ViewBag.Header = header;
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> CheckIn(IFormCollection form)
        {
            try
            {
                int attendanceId = int.Parse(form["id"]);

                // Get the session header first
                var header = _context.AttendanceHeaders.FirstOrDefault(h => h.AttendanceID == attendanceId);
                if (header == null)
                {
                    ViewBag.Result = new { Success = false, Message = "Attendance session not found." };
                    return View();
                }

                // Check if session is active
                var now = DateTime.Now;
                if (now < header.StartDateTime)
                {
                    ViewBag.Result = new { Success = false, Message = "Attendance session has not started yet." };
                    ViewBag.Header = header;
                    return View();
                }
                if (now > header.EndDateTime)
                {
                    ViewBag.Result = new { Success = false, Message = "Attendance session has already ended." };
                    ViewBag.Header = header;
                    return View();
                }

                // Proceed with submission
                string participantName = form["participantName"];
                string gender = form["gender"];
                string remark = form["remark"];
                string deviceName = form["deviceName"];
                decimal? latitude = !string.IsNullOrEmpty(form["latitude"]) ? decimal.Parse(form["latitude"]) : null;
                decimal? longitude = !string.IsNullOrEmpty(form["longitude"]) ? decimal.Parse(form["longitude"]) : null;
                string photoData = form["photoData"];
                string position = form["position"];

                string photoPath = null;

                if (!string.IsNullOrEmpty(photoData))
                {
                    var base64 = photoData.Split(',')[1];
                    var bytes = Convert.FromBase64String(base64);

                    var fileName = $"photo_{attendanceId}_{DateTime.Now:yyyyMMddHHmmss}.png";
                    var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "photos");

                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    var fullPath = Path.Combine(folderPath, fileName);
                    await System.IO.File.WriteAllBytesAsync(fullPath, bytes);

                    photoPath = $"/Eticket/uploads/photos/{fileName}";
                }

                var detail = new AttendanceDetail
                {
                    AttendanceID = attendanceId,
                    ParticipantName = participantName,
                    PicturePath = photoPath,
                    PictureTakenDate = DateTime.Now,
                    Latitude = Convert.ToDecimal(latitude),
                    Longitude = Convert.ToDecimal(longitude),
                    DeviceName = deviceName,
                    Remark = remark,
                    Position = position
                };

                _context.AttendanceDetails.Add(detail);
                await _context.SaveChangesAsync();

                // Success
                ViewBag.Result = new { Success = true, Message = "Check-in completed!" };
                ViewBag.Header = header;
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Result = new { Success = false, Message = $"Error: {ex.Message}" };

                var attendanceId = int.TryParse(form["id"], out int idVal) ? idVal : 0;
                var header = _context.AttendanceHeaders.FirstOrDefault(h => h.AttendanceID == attendanceId);
                ViewBag.Header = header;

                return View();
            }
        }


    }
}
