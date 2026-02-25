using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Database.Models
{
    public class Account
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string FbAccountId { get; set; }
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
