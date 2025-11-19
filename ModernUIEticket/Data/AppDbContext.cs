using ETicketNewUI.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;

namespace ETicketNewUI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Ticket> Tickets { get; set; }  // already done
    }
}
