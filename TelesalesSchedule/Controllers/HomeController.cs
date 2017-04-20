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
                ViewBag.StartDate = monday.Date.ToShortDateString();
                ViewBag.EndDate = sunday.Date.ToShortDateString();
                var computers = db.Schedules.Where(s => s.StartDate == monday.Date && s.EndDate == sunday.Date).Select(p => p.Computer).ToList();

                var comps = db.Schedules.Where(s => s.StartDate == monday.Date && s.EndDate == sunday.Date).Where(p => p.Computer != null && p.Computer.IsWorking == true).ToList();

                return View(comps);
            }
        }
        public ActionResult NextWeek()
        {
            using (var db = new TelesalesScheduleDbContext())
            {

                DateTime monday = DateTime.Now;

                while (monday.DayOfWeek != DayOfWeek.Monday)
                {
                    monday = monday.AddDays(1);
                }
                var sunday = monday.AddDays(6);
                
                ViewBag.StartDate = monday.Date.ToShortDateString();
                ViewBag.EndDate = sunday.Date.ToShortDateString();
                var computers = db.Schedules.Where(s => s.StartDate == monday.Date && s.EndDate == sunday.Date).Select(p => p.Computer).ToList();

                var comps = db.Schedules.Where(s => s.StartDate == monday.Date && s.EndDate == sunday.Date).Where(p => p.Computer != null && p.Computer.IsWorking == true).ToList();

                return View(comps);
            }
        }
        public ActionResult PreviousWeek()
        {
            using (var db = new TelesalesScheduleDbContext())
            {

                DateTime monday = DateTime.Now;

                while (monday.DayOfWeek != DayOfWeek.Monday)
                {
                    monday = monday.AddDays(-1);
                }
                
                monday = monday.AddDays(-7);
                var sunday = monday.AddDays(6);

                ViewBag.StartDate = monday.Date.ToShortDateString();
                ViewBag.EndDate = sunday.Date.ToShortDateString();
                var computers = db.Schedules.Where(s => s.StartDate == monday.Date && s.EndDate == sunday.Date).Select(p => p.Computer).ToList();

                var comps = db.Schedules.Where(s => s.StartDate == monday.Date && s.EndDate == sunday.Date).Where(p => p.Computer != null && p.Computer.IsWorking == true).ToList();

                return View(comps);
            }
        }
    }
}