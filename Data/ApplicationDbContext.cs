using MedicalPractice.Models;
using MedicalPractice.Status;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MedicalPractice.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Visit> Visits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


                 foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    var clrType = property.ClrType;

                    if (clrType.IsEnum)
                    {
                        var converterType = typeof(EnumToStringConverter<>)
                            .MakeGenericType(clrType);
                        var converter = (ValueConverter)Activator.CreateInstance(converterType)!;
                        property.SetValueConverter(converter);
                    }

                    // Bool → "True"/"False" string in DB
                    if (clrType == typeof(bool) || clrType == typeof(bool?))
                    {
                        // Only apply to actual mapped columns — skip computed props
                        if (property.PropertyInfo != null
                            && property.PropertyInfo.CanWrite)
                        {
                            if (clrType == typeof(bool))
                            {
                                property.SetValueConverter(new ValueConverter<bool, string>(
                                    v => v.ToString(),
                                    v => bool.Parse(v)));
                                property.SetMaxLength(5);
                            }
                        }
                    }
                }
            }





            // ── Seed Employees (plain‑text passwords, as requested) ──
            modelBuilder.Entity<Employee>().HasData(
                new Employee
                {
                    EmployeeID = 1,
                    UserName = "admin",
                    Email = "admin@medpractice.com",
                    FullName = "System Administrator",
                    FirstName = "Admin",
                    PasswordHash = "Admin123!",            // plain text, not hashed
                    Role = UserRole.Admin,
                    IsActive = AccountStatus.Active,
                    IsLockedOut = false,
                    FailedLoginAttempts = 0,
                    MustChangePassword = false,
                    IsTwoFactorEnabled = false
                    // no lockout, reset‑pin, or 2FA data
                },
                new Employee
                {
                    EmployeeID = 2,
                    UserName = "dr.smith",
                    Email = "dr.smith@medpractice.com",
                    FullName = "Dr. John Smith",
                    FirstName = "John",
                    PasswordHash = "Doctor123!",
                    Role = UserRole.Doctor,
                    IsActive = AccountStatus.Active,
                    IsLockedOut = false,
                    FailedLoginAttempts = 0,
                    MustChangePassword = false,
                    IsTwoFactorEnabled = false
                },
                new Employee
                {
                    EmployeeID = 3,
                    UserName = "assistant.jane",
                    Email = "jane@medpractice.com",
                    FullName = "Jane Doe",
                    FirstName = "Jane",
                    PasswordHash = "Assistant123!",
                    Role = UserRole.Assistant,
                    IsActive = AccountStatus.Active,
                    IsLockedOut = false,
                    FailedLoginAttempts = 0,
                    MustChangePassword = false,
                    IsTwoFactorEnabled = false
                }
            );
        }
    }
}