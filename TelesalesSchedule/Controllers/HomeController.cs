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
                var computers = db.Schedules.Where(s => s.StartDate == monday.Date && s.EndDate == sunday.Date).Select(p => p.Computer).ToList();

                var comps = db.Schedules.Where(s => s.StartDate == monday.Date && s.EndDate == sunday.Date).Where(p => p.Computer != null && p.Computer.IsWorking == true).ToList();

                //var comps = db.Schedules.Include("Computers").Where(s => s.StartDate == monday.Date && s.EndDate == sunday.Date).Select(s => new
                //{
                //    name = s.Computer.Name,
                //    mondayShiftOne = s.MondayShiftOneEnd ?? 0 - s.MondayShiftOneStart ?? 0,
                //    mondayShiftTwo = s.MondayShiftTwoEnd ?? 0 - s.MondayShiftTwoStart ?? 0,
                //    mondayShiftThree = s.MondayShiftThreeEnd ?? 0 - s.MondayShiftThreeStart ?? 0,
                //    thuesdayShiftOne = s.ThuesdayShiftOneEnd ?? 0 - s.ThuesdayShiftOneStart ?? 0,
                //    thuesdayShiftTwo = s.ThuesdayShiftTwoEnd ?? 0 - s.ThuesdayShiftTwoStart ?? 0,
                //    thuesdayShiftThree = s.ThuesdayShiftThreeEnd ?? 0 - s.ThuesdayShiftThreeStart ?? 0,

                //}).ToList();


                return View(comps);
            }
        }
    }
}