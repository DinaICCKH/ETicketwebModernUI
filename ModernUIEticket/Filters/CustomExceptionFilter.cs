using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Learn.Filters
{
    public class CustomExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<CustomExceptionFilter> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CustomExceptionFilter( ILogger<CustomExceptionFilter> logger, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }
        public void OnException(ExceptionContext context)
        {
            var exception = context.Exception;
            var httpContext = _httpContextAccessor.HttpContext;

            string controllerName = context.RouteData.Values["controller"]?.ToString() ?? "UnknownController";
            string actionName = context.RouteData.Values["action"]?.ToString() ?? "UnknownAction";
            string timestamp = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

            string userName = httpContext.User.Identity?.IsAuthenticated == true ? httpContext.User.Identity.Name : "Anonymous";

            string logMessage = $@"
                                ============================================================
                                Timestamp       : {timestamp}
                                User            : {userName}
                                Controller      : {controllerName}
                                Action          : {actionName}
                                Message         : {exception.Message}
                                StackTrace      : {exception.StackTrace}
                                InnerException  : {exception.InnerException?.Message}
                                ============================================================";

            try
            {
                string logFolderConfig = _configuration["LogFolderPath"] ?? "Logs";
                string logDir = Path.Combine(Directory.GetCurrentDirectory(), logFolderConfig);

                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                string logFile = Path.Combine(logDir, $"Log_{DateTime.Today:dd-MM-yyyy}.txt");

                File.AppendAllText(logFile, logMessage + Environment.NewLine);
            }
            catch
            {
                _logger.LogError("Failed to write exception log to file.");
            }

            _logger.LogError(exception, "Unhandled exception in {Controller}/{Action}", controllerName, actionName);

            bool isAjax = httpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (isAjax)
            {
                context.Result = new JsonResult(new
                {
                    data = (object)null,
                    success = false,
                    message = exception.Message
                })
                {
                    StatusCode = 500
                };
            }
            else
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }

            context.ExceptionHandled = true;
        }
    }
}
