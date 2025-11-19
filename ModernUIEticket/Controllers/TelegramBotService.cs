using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ETicketNewUI.Models;

namespace WebFront.Services
{
    public class TelegramBotService
    {
        private readonly TelegramBotClient _botClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private CancellationTokenSource _cts;

        public TelegramBotService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;

            // Initialize bot with your token
            _botClient = new TelegramBotClient("7807510270:AAGx3KsiOxuD4YKi88u7nijNVl_W2_iaoDc");
        }

        public void Start()
        {
            Trace.WriteLine("Starting Telegram bot service...");

            _cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() // Listen to all update types
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                _cts.Token
            );
        }

        public void Stop()
        {
            Trace.WriteLine("Stopping Telegram bot service...");
            _cts?.Cancel();
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message == null) return;

            var message = update.Message;

            // Handle /start command
            if (!string.IsNullOrEmpty(message.Text) && message.Text.StartsWith("/start"))
            {
                string parameter = message.Text.Replace("/start", "").Trim();
                if (!string.IsNullOrEmpty(parameter))
                {
                    // Store info in DB using raw SQL
                    await StoreUserInfoAsync(message.From.Id, message.From.Username, parameter, cancellationToken);

                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: $"Welcome eTicket System '{message.From.Username}'" +
                              $"\nYour UserID eTicket Is '{parameter}'" +
                              "\nThank you for using eTicket!",
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Welcome to the eTicket bot!",
                        cancellationToken: cancellationToken
                    );
                }
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Trace.WriteLine($"Telegram bot error: {exception.Message}");
            return Task.CompletedTask;
        }

        private async Task StoreUserInfoAsync(long userId, string username, string nameID, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();

                var sql = @"
                    IF NOT EXISTS (SELECT 1 FROM TelegramUser WHERE UserID = {0} AND ETicketUserID = {2})
                    BEGIN
                        INSERT INTO TelegramUser (UserID, Username, ETicketUserID, lastSyncDate)
                        VALUES ({0}, {1}, {2}, {3})
                    END
                    ELSE
                    BEGIN
                        UPDATE TelegramUser
                        SET Username = {1}, ETicketUserID = {2}, lastSyncDate = {3}
                        WHERE UserID = {0}
                    END
                ";

                await db.Database.ExecuteSqlRawAsync(sql,
                    userId,
                    username ?? (object)DBNull.Value,
                    nameID,
                    DateTime.Now
                );

                Trace.WriteLine($"Stored user info: UserID={userId}, Username={username}, ETicketID={nameID}");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error storing Telegram user info: {ex.Message}");
            }
        }

        // Optional: Send message to a user manually
        public async Task SendMessageToUser(long chatId, string message)
        {
            await _botClient.SendTextMessageAsync(chatId, message);
        }
    }
}
