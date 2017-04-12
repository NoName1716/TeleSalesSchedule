using System;
using System.Linq;
using System.Web.Mvc;
using TelesalesSchedule.Models;

namespace TelesalesSchedule.Controllers
{
    public class ScheduleController : Controller
    {
        // GET: Schedule
        public ActionResult Index()
        {
            return View();
        }

        //Get: 
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(Schedule model)
        {
            using (var db = new TelesalesScheduleDbContext())
            {
                if(!ModelState.IsValid)
                {
                    ViewBag.ErrorMessage = "Please enter a date!";
                    return View();
                }
                else
                {
                    try
                    {
                        DateTime startDate = Convert.ToDateTime(model.StartDate);
                        DateTime endDate = Convert.ToDateTime(model.EndDate);
                        var starDates = db.Schedules.Select(s => s.StartDate).ToList();
                        var endDates = db.Schedules.Select(s => s.EndDate).ToList();
                        //check for existing date
                        if (starDates.Contains(startDate))
                        {
                            ViewBag.ErrorMessageStartDate = "Schedule StartDate already exist.";
                            return View();
                        }
                        else if (endDates.Contains(endDate))
                        {
                            ViewBag.ErrorMessageEndDate = "Schedule EndDate already exist.";
                            return View();
                        }
                        else if((int)startDate.DayOfWeek != 1)
                        {
                            ViewBag.ErrorMessageStartDate = "StartDate must be Monday!";
                            return View();
                        }
                        else if ((int)endDate.DayOfWeek != 0)
                        {
                            ViewBag.ErrorMessageStartDate = "EndDate must be Sunday!";
                            return View();
                        }
                        else if(startDate >= endDate)
                        {
                            ViewBag.ErrorMessage = "Invalid data.";
                            return View();
                        }
                        else
                        {
                            var schedule = new Schedule
                            {
                                StartDate = startDate,
                                EndDate = endDate
                            };
                            db.Schedules.Add(schedule);
                            db.SaveChanges();
                        }
                    }
                    catch (Exception)
                    {

                        ViewBag.ErrorMessage = "Invalid data.";
                        return View();
                    }
                    
                    
                }
            }
            return RedirectToAction("List");
        }

        public ActionResult List()
        {
            using (var db= new TelesalesScheduleDbContext())
            {
                var schedules = db.Schedules.ToList();
                return View(schedules);
            }
        }
    }
}