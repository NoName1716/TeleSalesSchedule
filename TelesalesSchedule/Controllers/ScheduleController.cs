using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
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
        public ActionResult Create(Schedule model, int? id)
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
                        if (User.IsInRole("Admin"))
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
                            else if ((int)startDate.DayOfWeek != 1)
                            {
                                ViewBag.ErrorMessageStartDate = "StartDate must be Monday!";
                                return View();
                            }
                            else if ((int)endDate.DayOfWeek != 0)
                            {
                                ViewBag.ErrorMessageStartDate = "EndDate must be Sunday!";
                                return View();
                            }
                            else if (startDate >= endDate)
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

        //
        // GET: Schedule/Edit
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var context = new TelesalesScheduleDbContext())
            {
                var schedule = context.Schedules
                    .FirstOrDefault(s => s.Id == id);

                if (schedule == null)
                {
                    return HttpNotFound();
                }

                var model = new ScheduleViewModel();
                model.Id = schedule.Id;
                model.StartDate = schedule.StartDate;
                model.EndDate = schedule.EndDate;

                model.MondayShiftOneStart = schedule.MondayShiftOneStart;
                model.MondayShiftOneEnd = schedule.MondayShiftOneEnd;
                model.MondayShiftTwoStart = schedule.MondayShiftTwoStart;
                model.MondayShiftTwoEnd = schedule.MondayShiftTwoEnd;
                model.MondayShiftThreeStart = schedule.MondayShiftThreeStart;
                model.MondayShiftThreeEnd = schedule.MondayShiftThreeEnd;

                model.ThuesdayShiftOneStart = schedule.ThuesdayShiftOneStart;
                model.ThuesdayShiftOneEnd = schedule.ThuesdayShiftOneEnd;
                model.ThuesdayShiftTwoStart = schedule.ThuesdayShiftTwoStart;
                model.ThuesdayShiftTwoEnd = schedule.ThuesdayShiftTwoEnd;
                model.ThuesdayShiftThreeStart = schedule.ThuesdayShiftThreeStart;
                model.ThuesdayShiftThreeEnd = schedule.ThuesdayShiftThreeEnd;

                model.WednesdayShiftOneStart = schedule.WednesdayShiftOneStart;
                model.WednesdayShiftOneEnd = schedule.WednesdayShiftOneEnd;
                model.WednesdayShiftTwoStart = schedule.WednesdayShiftTwoStart;
                model.WednesdayShiftTwoEnd = schedule.WednesdayShiftTwoEnd;
                model.WednesdayShiftThreeStart = schedule.WednesdayShiftThreeStart;
                model.WednesdayShiftThreeEnd = schedule.WednesdayShiftThreeEnd;

                model.ThursdayShiftOneStart = schedule.ThursdayShiftOneStart;
                model.ThursdayShiftOneEnd = schedule.ThursdayShiftOneEnd;
                model.ThursdayShiftTwoStart = schedule.ThursdayShiftTwoStart;
                model.ThursdayShiftTwoEnd = schedule.ThursdayShiftTwoEnd;
                model.ThursdayShiftThreeStart = schedule.ThursdayShiftThreeStart;
                model.ThursdayShiftThreeEnd = schedule.ThursdayShiftThreeEnd;

                model.FridayShiftOneStart = schedule.FridayShiftOneStart;
                model.FridayShiftOneEnd = schedule.FridayShiftOneEnd;
                model.FridayShiftTwoStart = schedule.FridayShiftTwoStart;
                model.FridayShiftTwoEnd = schedule.FridayShiftTwoEnd;
                model.FridayShiftThreeStart = schedule.FridayShiftThreeStart;
                model.FridayShiftThreeEnd = schedule.FridayShiftThreeEnd;

                model.SaturdayShiftOneStart = schedule.SaturdayShiftOneStart;
                model.SaturdayShiftOneEnd = schedule.SaturdayShiftOneEnd;
                model.SaturdayShiftTwoStart = schedule.SaturdayShiftTwoStart;
                model.SaturdayShiftTwoEnd = schedule.SaturdayShiftTwoEnd;
                model.SaturdayShiftThreeStart = schedule.SaturdayShiftThreeStart;
                model.SaturdayShiftThreeEnd = schedule.SaturdayShiftThreeEnd;

                model.SundayShiftOneStart = schedule.SundayShiftOneStart;
                model.SundayShiftOneEnd = schedule.SundayShiftOneEnd;
                model.SundayShiftTwoStart = schedule.SundayShiftTwoStart;
                model.SundayShiftTwoEnd = schedule.SundayShiftTwoEnd;
                model.SundayShiftThreeStart = schedule.SundayShiftThreeStart;
                model.SundayShiftThreeEnd = schedule.SundayShiftThreeEnd;

                return View(model);
            }
        }

        //
        // POST: Schedule/Edit
        [HttpPost]
        public ActionResult Edit(ScheduleViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (var context = new TelesalesScheduleDbContext())
                {
                    var schedule = context.Schedules
                        .FirstOrDefault(
                        s => s.Id == model.Id &&
                        s.StartDate == model.StartDate &&
                        s.EndDate == model.EndDate);

                    string shiftValue = Request.Form["shift"];

                    if (shiftValue == "First")
                    {
                        if (model.MondayShiftOneStart != null)
                        {
                            schedule.MondayShiftOneStart = model.MondayShiftOneStart;
                        }
                        if (model.MondayShiftOneEnd != null)
                        {
                            schedule.MondayShiftOneEnd = model.MondayShiftOneEnd;
                        }

                        if (model.ThuesdayShiftOneStart != null)
                        {
                            schedule.ThuesdayShiftOneStart = model.ThuesdayShiftOneStart;
                        }
                        if (model.ThuesdayShiftOneEnd != null)
                        {
                            schedule.ThuesdayShiftOneEnd = model.ThuesdayShiftOneEnd;
                        }

                        if (model.WednesdayShiftOneStart != null)
                        {
                            schedule.WednesdayShiftOneStart = model.WednesdayShiftOneStart;
                        }
                        if (model.WednesdayShiftOneEnd != null)
                        {
                            schedule.WednesdayShiftOneEnd = model.WednesdayShiftOneEnd;
                        }

                        if (model.ThursdayShiftOneStart != null)
                        {
                            schedule.ThursdayShiftOneStart = model.ThursdayShiftOneStart;
                        }
                        if (model.ThursdayShiftOneEnd != null)
                        {
                            schedule.ThursdayShiftOneEnd = model.ThursdayShiftOneEnd;
                        }

                        if (model.FridayShiftOneStart != null)
                        {
                            schedule.FridayShiftOneStart = model.FridayShiftOneStart;
                        }
                        if (model.FridayShiftOneEnd != null)
                        {
                            schedule.FridayShiftOneEnd = model.FridayShiftOneEnd;
                        }

                        if (model.SaturdayShiftOneStart != null)
                        {
                            schedule.SaturdayShiftOneStart = model.SaturdayShiftOneStart;
                        }
                        if (model.SaturdayShiftOneEnd != null)
                        {
                            schedule.SaturdayShiftOneEnd = model.SaturdayShiftOneEnd;
                        }

                        if (model.SundayShiftOneStart != null)
                        {
                            schedule.SundayShiftOneStart = model.SundayShiftOneStart;
                        }
                        if (model.SundayShiftOneEnd != null)
                        {
                            schedule.SundayShiftOneEnd = model.SundayShiftOneEnd;
                        }
                    }
                    else if (shiftValue == "Second")
                    {
                        if (model.MondayShiftTwoStart != null)
                        {
                            schedule.MondayShiftTwoStart = model.MondayShiftTwoStart;
                        }
                        if (model.MondayShiftTwoEnd != null)
                        {
                            schedule.MondayShiftTwoEnd = model.MondayShiftTwoEnd;
                        }

                        if (model.ThuesdayShiftTwoStart != null)
                        {
                            schedule.ThuesdayShiftTwoStart = model.ThuesdayShiftTwoStart;
                        }
                        if (model.ThuesdayShiftTwoEnd != null)
                        {
                            schedule.ThuesdayShiftTwoEnd = model.ThuesdayShiftTwoEnd;
                        }

                        if (model.WednesdayShiftTwoStart != null)
                        {
                            schedule.WednesdayShiftTwoStart = model.WednesdayShiftTwoStart;  
                        }
                        if (model.WednesdayShiftTwoEnd != null)
                        {
                            schedule.WednesdayShiftTwoEnd = model.WednesdayShiftTwoEnd;
                        }

                        if (model.ThursdayShiftTwoStart != null)
                        {
                            schedule.ThursdayShiftTwoStart = model.ThursdayShiftTwoStart;
                        }
                        if (model.ThursdayShiftTwoEnd != null)
                        {
                            schedule.ThursdayShiftTwoEnd = model.ThursdayShiftTwoEnd;
                        }

                        if (model.FridayShiftTwoStart != null)
                        {
                            schedule.FridayShiftTwoStart = model.FridayShiftTwoStart; 
                        }
                        if (model.FridayShiftTwoEnd != null)
                        {
                            schedule.FridayShiftTwoEnd = model.FridayShiftTwoEnd;
                        }

                        if (model.SaturdayShiftTwoStart != null)
                        {
                            schedule.SaturdayShiftTwoStart = model.SaturdayShiftTwoStart;
                        }
                        if (model.SaturdayShiftTwoEnd != null)
                        {
                            schedule.SaturdayShiftTwoEnd = model.SaturdayShiftTwoEnd;
                        }

                        if (model.SundayShiftTwoStart != null)
                        {
                            schedule.SundayShiftTwoStart = model.SundayShiftTwoStart;
                        }
                        if (model.SundayShiftTwoEnd != null)
                        {
                            schedule.SundayShiftTwoEnd = model.SundayShiftTwoEnd;
                        }
                    }
                    else if (shiftValue == "Third")
                    {
                        if (model.MondayShiftTwoStart != null)
                        {
                            schedule.MondayShiftTwoStart = model.MondayShiftTwoStart;
                        }
                        if (model.MondayShiftTwoEnd != null)
                        {
                            schedule.MondayShiftTwoEnd = model.MondayShiftTwoEnd;
                        }

                        if (model.ThuesdayShiftTwoStart != null)
                        {
                            schedule.ThuesdayShiftTwoStart = model.ThuesdayShiftTwoStart;
                        }
                        if (model.ThuesdayShiftTwoEnd != null)
                        {
                            schedule.ThuesdayShiftTwoEnd = model.ThuesdayShiftTwoEnd;
                        }

                        if (model.WednesdayShiftTwoStart != null)
                        {
                            schedule.WednesdayShiftTwoStart = model.WednesdayShiftTwoStart;
                        }
                        if (model.WednesdayShiftTwoEnd != null)
                        {
                            schedule.WednesdayShiftTwoEnd = model.WednesdayShiftTwoEnd;
                        }

                        if (model.ThursdayShiftTwoStart != null)
                        {
                            schedule.ThursdayShiftTwoStart = model.ThursdayShiftTwoStart;
                        }
                        if (model.ThursdayShiftTwoEnd != null)
                        {
                            schedule.ThursdayShiftTwoEnd = model.ThursdayShiftTwoEnd;
                        }

                        if (model.FridayShiftTwoStart != null)
                        {
                            schedule.FridayShiftTwoStart = model.FridayShiftTwoStart;
                        }
                        if (model.FridayShiftTwoEnd != null)
                        {
                            schedule.FridayShiftTwoEnd = model.FridayShiftTwoEnd;
                        }

                        if (model.SaturdayShiftTwoStart != null)
                        {
                            schedule.SaturdayShiftTwoStart = model.SaturdayShiftTwoStart;
                        }
                        if (model.SaturdayShiftTwoEnd != null)
                        {
                            schedule.SaturdayShiftTwoEnd = model.SaturdayShiftTwoEnd;
                        }

                        if (model.SundayShiftTwoStart != null)
                        {
                            schedule.SundayShiftTwoStart = model.SundayShiftTwoStart;
                        }
                        if (model.SundayShiftTwoEnd != null)
                        {
                            schedule.SundayShiftTwoEnd = model.SundayShiftTwoEnd;
                        }
                    }



                    schedule.MondayShiftThreeStart = model.MondayShiftThreeStart;
                    schedule.MondayShiftThreeEnd = model.MondayShiftThreeEnd;



                  
                    schedule.ThuesdayShiftThreeStart = model.ThuesdayShiftThreeStart;
                    schedule.ThuesdayShiftThreeEnd = model.ThuesdayShiftThreeEnd;



                    
                    schedule.WednesdayShiftThreeStart = model.WednesdayShiftThreeStart;
                    schedule.WednesdayShiftThreeEnd = model.WednesdayShiftThreeEnd;


                   
                    schedule.ThursdayShiftThreeStart = model.ThursdayShiftThreeStart;
                    schedule.ThursdayShiftThreeEnd = model.ThursdayShiftThreeEnd;


                 
                    schedule.FridayShiftThreeStart = model.FridayShiftThreeStart;
                    schedule.FridayShiftThreeEnd = model.FridayShiftThreeEnd;


                  
                    schedule.SaturdayShiftThreeStart = model.SaturdayShiftThreeStart;
                    schedule.SaturdayShiftThreeEnd = model.SaturdayShiftThreeEnd;


                    
                    schedule.SundayShiftThreeStart = model.SundayShiftThreeStart;
                    schedule.SundayShiftThreeEnd = model.SundayShiftThreeEnd;

                    context.Entry(schedule).State = EntityState.Modified;
                    context.SaveChanges();

                    return RedirectToAction("List");
                }
            }

            return View(model);
        }
    }
}