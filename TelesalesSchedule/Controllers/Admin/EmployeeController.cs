using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using TelesalesSchedule.Extensions;
using TelesalesSchedule.Models;


namespace TelesalesSchedule.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class EmployeeController : Controller
    {
        // GET: Employee/Create
        //public ActionResult Index()
        //{
        //    return View();
        //}
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Employee/Create
        [HttpPost]
        [ActionName("Create")]
        public ActionResult Create(EditEmployeeViewModel employee)
        {
            if (ModelState.IsValid)
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
                            SaveDeskAgent = employee.SaveDeskAgent,
                            UserName = employee.UserName,
                            SeniorSpecialist = employee.SeniorSpecialist,
                            Manager = db.Employees.Where(e => e.FullName == employee.ManagerFullName).FirstOrDefault()
                    };
                        
                        db.Employees.Add(emp);
                        db.SaveChanges();

                        this.AddNotification("Employee created.", NotificationType.SUCCESS);
                    }

                    return RedirectToAction("List");
                }
            }
            
            return View(employee);
        }

        //
        // GET: Employee/List
        public ActionResult List()
        {
            using (var context = new TelesalesScheduleDbContext())
            {
                var employees = context.Employees
                    .Include(e => e.Manager)
                    .ToList();

                return View(employees);
            }
        }

        //
        // GET: Employee/Edit
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var context = new TelesalesScheduleDbContext())
            {
                // Get employee from database
                var employee = context.Employees
                    .Where(e => e.Id == id)
                    .First();

                // Check if employee exists
                if (employee == null)
                {
                    return HttpNotFound();
                }

                var viewModel = new EditEmployeeViewModel();
                viewModel.Id = employee.Id;
                viewModel.FullName = employee.FullName;
                viewModel.UserName = employee.UserName;
                viewModel.ManagerId = employee.ManagerId;
                viewModel.ManagerFullName = employee.Manager == null ? null : employee.Manager.FullName;
                viewModel.BirthDay = employee.BirthDay;
                viewModel.FullTimeAgent = employee.FullTimeAgent;
                viewModel.SaveDeskAgent = employee.SaveDeskAgent;
                viewModel.SeniorSpecialist = employee.SeniorSpecialist;
                viewModel.IsDeleted = employee.IsDeleted;

                if (employee.Manager != null)
                {
                    viewModel.ManagerFullName = employee.Manager.FullName;
                }

                return View(viewModel);
            }
        }

        //
        // POST: Employee/Edit
        [HttpPost]
        public ActionResult Edit(EditEmployeeViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                using (var context = new TelesalesScheduleDbContext())
                {
                    var employee = context.Employees
                        .FirstOrDefault(e => e.Id == viewModel.Id);

                    if (employee == null)
                    {
                        return HttpNotFound();
                    }

                    employee.FullName = viewModel.FullName;
                    employee.UserName = viewModel.UserName;
                    employee.Manager = context.Employees.FirstOrDefault(m => m.FullName == viewModel.ManagerFullName);
                    employee.BirthDay = viewModel.BirthDay;
                    employee.FullTimeAgent = viewModel.FullTimeAgent;
                    employee.SaveDeskAgent = viewModel.SaveDeskAgent;
                    employee.SeniorSpecialist = viewModel.SeniorSpecialist;
                    employee.IsDeleted = viewModel.IsDeleted;                

                    context.Entry(employee).State = EntityState.Modified;
                    context.SaveChanges();

                    this.AddNotification("Employee edited.", NotificationType.INFO);
                    
                    return RedirectToAction("List");
                }
            }

            return View(viewModel); 
        }
    }
}
