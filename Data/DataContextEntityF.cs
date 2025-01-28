using DotnetAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DotnetAPI.Data
{
    public class DataContextEntityF(IConfiguration config) : DbContext
    {
        private readonly IConfiguration _config = config;

        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<UserSalary> UserSalary { get; set; }
        public virtual DbSet<UserJobInfo> UserJobInfo { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured == false)
            {
                optionsBuilder.UseSqlServer(
                    _config.GetConnectionString("DefaultConnection"),
                    optionsBuilder => optionsBuilder.EnableRetryOnFailure()
                );
            }
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Default Schema is DBO, so we are explicitly setting
            modelBuilder.HasDefaultSchema("TutorialAppSchema");
            var userEntity = modelBuilder.Entity<User>();
            // EF by defaults takes the name of the model and since our model is User and
            // the name of our table in DB is Users, so we are setting it
            userEntity.ToTable("Users", "TutorialAppSchema");
            userEntity.HasKey(u => u.UserId);

            var userSalaryEntity = modelBuilder.Entity<UserSalary>();
            userSalaryEntity.HasKey(u => u.UserId);

            var userJobInfoEntity = modelBuilder.Entity<UserJobInfo>();
            userJobInfoEntity.HasKey(u => u.UserId);

        }


    }
}