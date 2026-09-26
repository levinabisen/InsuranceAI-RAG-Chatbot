using InsuranceAI.Api.Model;
using Microsoft.EntityFrameworkCore;

namespace InsuranceAI.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<ChatMessage> ChatMessages { get; set; }

        public DbSet<DocumentChunk> DocumentChunks { get; set; }
    }
}
