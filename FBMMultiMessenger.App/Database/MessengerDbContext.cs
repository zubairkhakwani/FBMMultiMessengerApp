using FBMMultiMessenger.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Database
{
    public class MessengerDbContext : DbContext
    {
        public MessengerDbContext(DbContextOptions<MessengerDbContext> options) : base(options)
        {

        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<ChatMessages> ChatMessages { get; set; }
        public DbSet<SyncMeta> SyncMeta { get; set; }
    }
}
