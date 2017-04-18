using System;
using System.Linq;
using System.Web.Mvc;
using TelesalesSchedule.Models;

namespace TelesalesSchedule.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            using (var db = new TelesalesScheduleDbContext())
            {

                DateTime monday = DateTime.Now;

                while (monday.DayOfWeek != DayOfWeek.Monday)
                {
                    monday = monday.AddDays(-1);
                }
                var sunday = monday.AddDays(6);
                //for test only
                monday = new DateTime(2017, 04, 24);
                sunday = new DateTime(2017, 04, 30);
                var computers = db.Computers.SelectMany(s => s.Schedules).Where(p => p.StartDate == monday.Date && p.EndDate == sunday.Date).ToList();
                
                return View(computers);
            }
        }
    }
}