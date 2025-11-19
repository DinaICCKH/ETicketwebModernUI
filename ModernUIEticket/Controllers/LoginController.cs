using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using ETicketNewUI.Models;
using Microsoft.AspNetCore.Identity.Data;

namespace ETicketNewUI.Controllers
{
    public class LoginController : Controller
    {
        private readonly TicketDbContext _context;

        public LoginController(TicketDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(string username, string password, bool rememberMe)
        {
            // 1. Hash the password
            string hashedPassword = HashPassword(password);

            // 2. Secret code
            string secretCode = "Tga5vz5oWiClgzayaLMZAx7KoTGLSJGrCQkY01gNb0c=";

            // 3. Call stored procedure
            var resultList = _context.LoginResults
                .FromSqlRaw(
                    "EXEC ICC_GET_login @SecretCode, @Username, @Password",
                    new SqlParameter("@SecretCode", secretCode),
                    new SqlParameter("@Username", username),
                    new SqlParameter("@Password", hashedPassword)
                )
                .AsNoTracking()
                .ToList();

            var result = resultList.FirstOrDefault();

            // 4. Login success
            if (result != null && result.Code == 200)
            {
                // Generate token from result
                var token = GenerateToken(result);

                // Save token in session
                HttpContext.Session.SetString("UserToken", token);


                return RedirectToAction("Index", "Home");
            }

            // 5. Login failed
            ViewBag.Error = result?.Message ?? "Login failed";
            return View();
        }

        [HttpPost]
        [Route("api/login")]
        public IActionResult ApiLogin([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request?.Username) || string.IsNullOrEmpty(request?.Password))
                return BadRequest(new { Message = "Username and password are required." });

            string hashedPassword = HashPassword(request.Password);
            string secretCode = "Tga5vz5oWiClgzayaLMZAx7KoTGLSJGrCQkY01gNb0c=";

            var resultList = _context.LoginResults
             .FromSqlRaw(
                 "EXEC ICC_GET_login @SecretCode, @Username, @Password",
                 new SqlParameter("@SecretCode", secretCode),
                 new SqlParameter("@Username", request.Username),
                 new SqlParameter("@Password", hashedPassword)
             )
             .AsNoTracking()
             .ToList();

            var result = resultList.FirstOrDefault();

            if (result != null && result.Code == 200)
            {
                var token = GenerateToken(result);
                return Ok(new
                {
                    Status = "Success",
                    Token = token,
                    User = result
                });
            }

            return BadRequest(new
            {
                Status = "Failed",
                Message = result?.Message ?? "Login failed"
            });
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Clear all session
            return RedirectToAction("Index", "Login");
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (var b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        private string GenerateToken(LoginResultDto result)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(result);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        public class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }
    }
}
