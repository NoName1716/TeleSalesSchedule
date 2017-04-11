using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TelesalesSchedule.Models;

namespace TelesalesSchedule.Controllers.Admin
{
    public class EmployeeController : Controller
    {
        // GET: Employee
        //public ActionResult Index()
        //{
        //    return View();
        //}
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ActionName("Register")]
        public ActionResult Register(Employee employee)
        {

            using (var db = new TelesalesScheduleDbContext())
            {
                var emoloyees = db.Employees.Select(e => e.FullName).ToList();
                if (emoloyees.Contains(employee.FullName))
                {
                    ViewBag.ErrorMessage = "Employee already exist!";
                    return View();
                }
                else
                {
                    var emp = new Employee
                    {
                        FullName = employee.FullName,
                        BirthDay = Convert.ToDateTime(employee.BirthDay),
                        FullTimeAgent = employee.FullTimeAgent,
                        IsDeleted = employee.IsDeleted,
                        Manager = db.Employees.Where(m => m.FullName == employee.Manager.FullName).FirstOrDefault(),
                        SaveDeskAgent = employee.SaveDeskAgent,
                        UserName = employee.UserName,
                        SeniorSpecialist = employee.SeniorSpecialist
                    };
                    db.Employees.Add(emp);
                    db.SaveChanges();
                }
                
            }

            return RedirectToAction("Index", "Home");
        }

    }
}