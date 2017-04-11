using Microsoft.AspNet.Identity.EntityFramework;
using System.Data.Entity;

namespace TelesalesSchedule.Models
{
    public class TelesalesScheduleDbContext : IdentityDbContext<ApplicationUser>
    {
        public TelesalesScheduleDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public virtual DbSet<Employee> Employees { get; set; }

        public virtual DbSet<Schedule> Schedules { get; set; }

        public virtual DbSet<Computer> Computers { get; set; }

        public static TelesalesScheduleDbContext Create()
        {
            return new TelesalesScheduleDbContext();
        }
    }
}