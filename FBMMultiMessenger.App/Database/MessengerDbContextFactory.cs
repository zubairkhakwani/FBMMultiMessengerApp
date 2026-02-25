using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Database
{
    public class MessengerDbContextFactory : IDesignTimeDbContextFactory<MessengerDbContext>
    {
        public MessengerDbContext CreateDbContext(string[] args)
        {
            //This is only used by EF tooling at design time (migrations, scaffolding etc.) — it never runs in your actual app. The dummy design_time.db path doesn't matter, it's never actually created.
            var options = new DbContextOptionsBuilder<MessengerDbContext>()
                .UseSqlite("Data Source=design_time.db")
                .Options;

            return new MessengerDbContext(options);
        }
    }
}
