using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LocalServices.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<LocalServices.Models.Category> Categories { get; set; }
        public DbSet<LocalServices.Models.Facility> Facilities { get; set; }
        public DbSet<LocalServices.Models.Service> Services { get; set; }
    }
}
