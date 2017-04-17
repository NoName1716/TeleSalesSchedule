using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TelesalesSchedule.Extensions;
using TelesalesSchedule.Models;

namespace TelesalesSchedule.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        // GET: User
        public ActionResult Index()
        {
            return RedirectToAction("List");
        }

        //
        // GET: User/List
        public ActionResult List()
        {
            using (var context = new TelesalesScheduleDbContext())
            {
                var users = context.Users.ToList();

                // Get all admins and store them for the view, using ViewBag
                var admins = GetAdminUserNames(users, context);
                ViewBag.Admins = admins;

                return View(users);
            }
        }

        //
        // GET: User/Edit
        public ActionResult Edit(string id)
        {
            // Validate id
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var context = new TelesalesScheduleDbContext())
            {
                // Get user from database
                var user = context.Users
                    .Where(u => u.Id == id)
                    .First();

                // Check if user exists
                if (user == null)
                {
                    return HttpNotFound();
                }

                // Create a view model
                var viewModel = new EditUserViewModel();
                viewModel.User = user;
                viewModel.Roles = GetUserRoles(user, context);

                // Pass the model to the view
                return View(viewModel);
            }
        }

        //
        // POST: User/Edit
        [HttpPost]
        public ActionResult Edit(string id, EditUserViewModel viewModel)
        {
            // Check if model is valid 
            if (ModelState.IsValid)
            {
                using (var context = new TelesalesScheduleDbContext())
                {
                    // Get user from database
                    var user = context.Users.FirstOrDefault(u => u.Id == id);

                    // Check if user exists
                    if (user == null)
                    {
                        return HttpNotFound();
                    }

                    // If password field is not empty, change password
                    if (!string.IsNullOrEmpty(viewModel.Password))
                    {
                        var hasher = new PasswordHasher();
                        var passwordHash = hasher.HashPassword(viewModel.Password);
                        user.PasswordHash = passwordHash;
                    }

                    // Set user properties
                    user.Email = viewModel.User.Email;
                    user.UserName = viewModel.User.Email;
                    this.SetUserRoles(viewModel, user, context);

                    // Save changes
                    context.Entry(user).State = EntityState.Modified;
                    context.SaveChanges();

                    this.AddNotification("User edited", NotificationType.INFO);

                    return RedirectToAction("List");
                }
            }

            return View(viewModel);
        }

        //
        // GET: User/Delete
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var context = new TelesalesScheduleDbContext())
            {
                // Get user from database
                var user = context.Users
                    .Where(u => u.Id.Equals(id))
                    .First();

                // Check if user exists
                if (user == null)
                {
                    return HttpNotFound();
                }

                return View(user);
            }
        }

        //
        // POST: User/Delete
        [HttpPost]
        [ActionName("Delete")]
        public ActionResult DeleteConfirmed(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var context = new TelesalesScheduleDbContext())
            {
                // Get user from database
                var user = context.Users
                    .Where(u => u.Id.Equals(id))
                    .First();

                // when deleting user to change IsDeleted = true in Employee
                var employee = context.Employees.SingleOrDefault(e => e.UserName == user.UserName);
                employee.IsDeleted = true;
                context.Entry(employee).State = EntityState.Modified;

                // Delete user and save changes
                context.Users.Remove(user);
                context.SaveChanges();

                this.AddNotification("User deleted.", NotificationType.WARNING);

                return RedirectToAction("List");
            }
        }

        private void SetUserRoles(EditUserViewModel viewModel, ApplicationUser user, TelesalesScheduleDbContext context)
        {
            // Create user manager
            var userManager = HttpContext.GetOwinContext()
                .GetUserManager<ApplicationUserManager>();

            // for each role checks weather it is selected and if the user is in the role
            foreach (var role in viewModel.Roles)
            {
                if (role.IsSelected && !userManager.IsInRole(user.Id, role.Name))
                {
                    userManager.AddToRole(user.Id, role.Name);
                }
                else if (!role.IsSelected && userManager.IsInRole(user.Id, role.Name))
                {
                    userManager.RemoveFromRole(user.Id, role.Name);
                }
            }
        }

        private List<Role> GetUserRoles(ApplicationUser user, TelesalesScheduleDbContext context)
        {
            // Create user manager
            var userManager = Request
                .GetOwinContext()
                .GetUserManager<ApplicationUserManager>();

            // Get all application roles
            var roles = context.Roles
                .Select(r => r.Name)
                .OrderBy(r => r)
                .ToList();

            // For each application role, check if the user has it
            var userRoles = new List<Role>();

            foreach (var roleName in roles)
            {
                var role = new Role { Name = roleName };

                if (userManager.IsInRole(user.Id, roleName))
                {
                    role.IsSelected = true;
                }

                userRoles.Add(role);
            }

            // Return a list with all roles
            return userRoles;
        }

        private HashSet<string> GetAdminUserNames(List<ApplicationUser> users, TelesalesScheduleDbContext context)
        {
            // Create user manager
            var userManager = new UserManager<ApplicationUser>(
                new UserStore<ApplicationUser>(context));

            var admins = new HashSet<string>();

            // Get all usernames of users with role Admin
            foreach (var user in users)
            {
                if (userManager.IsInRole(user.Id, "Admin"))
                {
                    admins.Add(user.UserName);
                }
            }

            return admins;
        }
    }
}