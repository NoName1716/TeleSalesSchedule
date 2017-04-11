using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace TelesalesSchedule.Models
{
    public class TelesalesScheduleDbContext : IdentityDbContext<ApplicationUser>
    {
        public TelesalesScheduleDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static TelesalesScheduleDbContext Create()
        {
            return new TelesalesScheduleDbContext();
        }
    }
}