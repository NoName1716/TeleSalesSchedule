using TelesalesSchedule.Migrations;
using Microsoft.Owin;
using Owin;
using System.Data.Entity;
using TelesalesSchedule.Models;

[assembly: OwinStartupAttribute(typeof(TelesalesSchedule.Startup))]
namespace TelesalesSchedule
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            Database.SetInitializer(
                new MigrateDatabaseToLatestVersion<TelesalesScheduleDbContext, Configuration>());

            ConfigureAuth(app);
        }
    }
}
