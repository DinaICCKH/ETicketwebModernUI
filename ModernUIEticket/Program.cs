using ETicketNewUI.Models;
using Learn.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebFront.Services; // <-- Make sure your TelegramBotService namespace is included

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// 1. Add services
builder.Services.AddScoped<CustomExceptionFilter>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<CustomExceptionFilter>();
});

// 2. Add DbContext
builder.Services.AddDbContext<TicketDbContext>(options =>
//options.UseSqlServer("Server=103.112.107.159;Database=E_Ticket;User Id=sa;Password=!QAZ2wsx#EDC;TrustServerCertificate=True"));

options.UseSqlServer("Server=103.112.107.159;Database=E_Ticket_Dev;User Id=sa;Password=!QAZ2wsx#EDC;TrustServerCertificate=True"));

// 3. Add Session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 4. Register TelegramBotService as singleton
builder.Services.AddSingleton<TelegramBotService>();

var app = builder.Build();

// 5. Configure middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Eticket/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Session must be before Authorization if you plan to use it in auth
app.UseSession();

app.UseAuthorization();

// 6. Start Telegram bot on app startup
var botService = app.Services.GetRequiredService<TelegramBotService>();
botService.Start();

// 7. Default route pointing to Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();
