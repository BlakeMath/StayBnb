using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StayBnb.Models;

namespace StayBnb.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; } = null!;

        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Configure the ApplicationUser entity
            builder.Entity<ApplicationUser>().ToTable("ApplicationUsers");

            // Set the primary key for ApplicationUser to be the same as IdentityUser's Id
            builder.Entity<ApplicationUser>().Property(user => user.Id).ValueGeneratedNever();

            // Configure the relationship between ApplicationUser and IdentityUser
            builder.Entity<ApplicationUser>().HasOne<IdentityUser>().WithOne().HasForeignKey<ApplicationUser>(user => user.Id).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
