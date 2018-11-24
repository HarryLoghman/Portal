using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Portal.Areas.Tools.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        // GET: Tools/Users
        public ActionResult Index()
        {
            return View();
        }
        private PortalEntities db = new PortalEntities();

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Users_Read([DataSourceRequest]DataSourceRequest request)
        {
            //DataSourceResult result = db.AspNetUsers.ToDataSourceResult(request, users => new
            //{
            //    Id = users.Id,
            //    name = users.Name,
            //    lastName = users.Lastname,
            //    userId = users.UserName,


            //});
            //return Json(result, JsonRequestBehavior.AllowGet);
            List<myAspNetUsers> myUsers = new List<myAspNetUsers>();
            var vw = db.vw_AspNetUsers.ToList();
            myAspNetUsers my;
            for (int i = 0; i <= vw.Count - 1; i++)
            {
                my = new myAspNetUsers();
                my.Id = vw[i].Id;
                my.lastName = vw[i].Lastname;
                my.name = vw[i].Name;
                my.userName = vw[i].UserName;
                my.roles = (vw[i].roles == null ? null : vw[i].roles.Split(',').ToList());
                my.services = (vw[i].Services == null ? null : vw[i].Services.Split(',').ToList());
                my.aggregators = (vw[i].Aggregators == null ? null : vw[i].Aggregators.Split(',').ToList());
                myUsers.Add(my);
            }
            DataSourceResult result = myUsers.ToDataSourceResult(request, users => new
            {
                Id = users.Id,
                name = users.name,
                lastName = users.lastName,
                userName = users.userName,
                roles = users.roles,
                services = users.services,
                aggregators = users.aggregators
            });
            return Json(result, JsonRequestBehavior.AllowGet);

            //DataSourceResult result = db.vw_AspNetUsers.ToDataSourceResult(request, users => new
            //{
            //    Id = users.Id,
            //    name = users.Name,
            //    lastName = users.Lastname,
            //    userName = users.UserName,
            //    roles = users.roles.Split(',').ToList(),
            //    services = users.Services,
            //    aggregators = users.Aggregators,
            //});
            //return Json(result, JsonRequestBehavior.AllowGet);


        }
        [AcceptVerbs(HttpVerbs.Get)]
        [Authorize(Roles = "Admin")]
        public ActionResult Users_ChangePassword(string id)
        {
            if (string.IsNullOrEmpty(id)) return View("Users_ChangePassword", null);
            if (!db.AspNetUsers.Any(o => o.Id == id)) return View("Users_ChangePassword", null);
            return View("Users_ChangePassword", new Models.User_ChangePasswordViewModel(id.ToString()));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Users_ChangePassword(Models.User_ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            model.SuccessfullySaved = false;
            var userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            var result = userManager.RemovePassword(model.userId);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError("error", err);
                }
            }
            else
            {
                result = userManager.AddPassword(model.userId, model.NewPassword);

                if (!result.Succeeded)
                {
                    foreach (var err in result.Errors)
                    {
                        ModelState.AddModelError("error", err);
                    }
                }
                else
                {
                    var applicationUser = userManager.FindByIdAsync(model.userId).Result;
                    var res = userManager.UpdateAsync(applicationUser).Result;
                    db.SaveChanges();
                    model.SuccessfullySaved = true;
                }
            }

            return View(model);
        }


        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Users_Update([DataSourceRequest]DataSourceRequest request, [Bind] myAspNetUsers user)
        {

            if (ModelState.IsValid)
            {
                try
                {
                    bool error = false;
                    if (string.IsNullOrEmpty(user.userName))
                    {
                        ModelState.AddModelError("error", "UserName is not specified");
                        error = true;
                    }
                    else
                    {
                        int userCount = db.vw_AspNetUsers.Where(o => o.UserName == user.userName && o.Id != user.Id).Count();
                        if (userCount > 0)
                        {
                            ModelState.AddModelError("error", "There are '" + userCount.ToString() + "' users with the same UserName");
                            error = true;
                            //throw new System.Data.Entity.Validation.DbEntityValidationException("There is no user left with 'admin' role");
                        }
                        else
                        {
                            bool isUserAdmin = user.roles.Any(o => o.ToLower() == "admin");
                            if (!isUserAdmin)
                            {
                                int adminLeftCount = this.getAdminLeftCount(user.Id);// db.vw_AspNetUsers.Any(o => (o.roles == "admin" || o.roles.StartsWith("admin,") || o.roles.EndsWith(",admin") || o.roles.Contains(",admin,")) && o.Id != user.Id);
                                if (adminLeftCount <= 0)
                                {
                                    ModelState.AddModelError("error", "The application will not have any user with 'Admin' role. First define/modify a user with 'Admin' role");
                                    error = true;
                                }
                            }
                            if (!error)
                            {
                                var userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();

                                var applicationUser = userManager.FindByIdAsync(user.Id).Result;
                                applicationUser.Lastname = user.lastName;
                                applicationUser.Name = user.name;
                                applicationUser.UserName = user.userName;

                                this.setRoles(user, applicationUser);
                                //userManager.ResetPassword("admin",)
                                var result = userManager.UpdateAsync(applicationUser).Result;
                                if (result.Succeeded)
                                {

                                    this.setServices(user);
                                    this.setAggregators(user);

                                    db.SaveChanges();
                                }
                                else
                                {
                                    foreach (var err in result.Errors)
                                    {
                                        ModelState.AddModelError("error", err);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    // Retrieve the error messages as a list of strings.
                    var errorMessages = ex.EntityValidationErrors
                            .SelectMany(x => x.ValidationErrors)
                            .Select(x => x.ErrorMessage);

                    // Join the list to a single string.
                    var fullErrorMessage = string.Join("; ", errorMessages);

                    // Combine the original exception message with the new one.
                    var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                    // Throw a new DbEntityValidationException with the improved exception message.
                    ModelState.AddModelError("error", ex.Message);
                    //throw new System.Data.Entity.Validation.DbEntityValidationException(exceptionMessage, ex.EntityValidationErrors);
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("error", e.Message);
                    //throw new System.Data.Entity.Validation.DbEntityValidationException(e.Message);
                }

            }

            return Json(new[] { user }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Users_Delete([DataSourceRequest]DataSourceRequest request, [Bind] myAspNetUsers user)
        {

            //ModelState.AddModelError("error", "hadi loghmn");

            //return  Json(new[] { user }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
            //throw new HttpException("salam");
            if (ModelState.IsValid)
            {
                try
                {
                    string userId = user.Id;
                    int adminLeftCount = this.getAdminLeftCount(user.Id);
                    if (adminLeftCount <= 0)
                    {
                        ModelState.AddModelError("error", "By deleting this user, the application will not have any user with 'Admin' role. First define/modify a user with 'Admin' role");
                        //throw new System.Data.Entity.Validation.DbEntityValidationException("There is no user left with 'admin' role");
                    }
                    else
                    {
                        var userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
                        var applicationUser = userManager.FindByIdAsync(userId);
                        if (applicationUser == null)
                        {
                            ModelState.AddModelError("error", "There is no such a user with userId ='" + userId + "'");
                            //throw new System.Data.Entity.Validation.DbEntityValidationException("There is no user with userId ='" + userId + "'");
                        }
                        else
                        {
                            var services = db.AspNetUsersRolesServiceAccesses.Where(o => o.userId == userId);
                            foreach (AspNetUsersRolesServiceAccess service in services)
                            {
                                db.Entry(service).State = System.Data.Entity.EntityState.Deleted;
                            }

                            var result = userManager.DeleteAsync(applicationUser.Result).Result;
                            if (result.Succeeded)
                            {
                                db.SaveChanges();
                            }
                            else
                            {
                                foreach (var err in result.Errors)
                                {
                                    ModelState.AddModelError("error", err);
                                }
                            }

                        }
                    }
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    // Retrieve the error messages as a list of strings.
                    var errorMessages = ex.EntityValidationErrors
                            .SelectMany(x => x.ValidationErrors)
                            .Select(x => x.ErrorMessage);

                    // Join the list to a single string.
                    var fullErrorMessage = string.Join("; ", errorMessages);

                    // Combine the original exception message with the new one.
                    var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                    // Throw a new DbEntityValidationException with the improved exception message.
                    ModelState.AddModelError("error", ex);
                    //throw new System.Data.Entity.Validation.DbEntityValidationException(exceptionMessage, ex.EntityValidationErrors);
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("error", e);
                    //throw new System.Data.Entity.Validation.DbEntityValidationException(e.Message);
                }

            }

            return Json(new[] { user }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        private int getAdminLeftCount(string excludeUserId)
        {
            int adminLeftCount = db.vw_AspNetUsers.Count(o => (o.roles.ToLower() == "admin" || o.roles.ToLower().StartsWith("admin,") || o.roles.ToLower().EndsWith(",admin") || o.roles.ToLower().Contains(",admin,")) && o.Id != excludeUserId);
            return adminLeftCount;
        }

        private void setRoles(myAspNetUsers user, Models.ApplicationUser applicationUser)
        {
            List<AspNetRole> lstSelectedRolesInDB = db.AspNetRoles.Where(o => user.roles.Contains(o.Name)).ToList();

            for (int i = 0; i <= applicationUser.Roles.Count - 1; i++)
            {
                var appRole = applicationUser.Roles.ElementAt(i);
                if (lstSelectedRolesInDB.Count(o => o.Id == appRole.RoleId) == 0)
                {
                    bool removeResult = applicationUser.Roles.Remove(appRole);
                }
            }


            foreach (var dbRole in lstSelectedRolesInDB)
            {
                if (applicationUser.Roles.Count(o => o.RoleId == dbRole.Id) == 0)
                {
                    Microsoft.AspNet.Identity.EntityFramework.IdentityUserRole iRole = new Microsoft.AspNet.Identity.EntityFramework.IdentityUserRole();
                    iRole.RoleId = dbRole.Id;
                    iRole.UserId = user.Id;
                    applicationUser.Roles.Add(iRole);
                }
            }
        }

        private void setServices(myAspNetUsers user)
        {
            var dbUserServicesAccess = db.AspNetUsersRolesServiceAccesses.Where(o => o.userId == user.Id && o.serviceId != null);
            if (user.services == null)
            {
                foreach (var dbUserService in dbUserServicesAccess)
                {
                    db.Entry(dbUserService).State = System.Data.Entity.EntityState.Deleted;
                }
                return;
            }


            var currentSevicesAccess = db.Services.Where(o => user.services.Contains(o.ServiceCode));

            foreach (var dbUserService in dbUserServicesAccess)
            {
                if (!currentSevicesAccess.Any(o => o.Id == dbUserService.serviceId))
                {
                    db.Entry(dbUserService).State = System.Data.Entity.EntityState.Deleted;
                }
            }
            foreach (var currentService in currentSevicesAccess)
            {
                if (!dbUserServicesAccess.Any(o => o.serviceId == currentService.Id))
                {
                    AspNetUsersRolesServiceAccess userRolesServiceAccess = new AspNetUsersRolesServiceAccess();
                    userRolesServiceAccess.aggregatorId = null;
                    userRolesServiceAccess.roleId = null;
                    userRolesServiceAccess.serviceId = currentService.Id;
                    userRolesServiceAccess.userId = user.Id;

                    db.AspNetUsersRolesServiceAccesses.Add(userRolesServiceAccess);
                }
            }

        }

        private void setAggregators(myAspNetUsers user)
        {
            var dbUserAggregatorsAccess = db.AspNetUsersRolesServiceAccesses.Where(o => o.userId == user.Id && o.aggregatorId != null);
            if (user.aggregators == null)
            {
                foreach (var dbUserAggregator in dbUserAggregatorsAccess)
                {
                    db.Entry(dbUserAggregator).State = System.Data.Entity.EntityState.Deleted;
                }
                return;
            }
            var currentAggregatorsAccess = db.Aggregators.Where(o => user.aggregators.Contains(o.AggregatorName));

            foreach (var dbUserAggregator in dbUserAggregatorsAccess)
            {
                if (!currentAggregatorsAccess.Any(o => o.Id == dbUserAggregator.aggregatorId))
                {
                    db.Entry(dbUserAggregator).State = System.Data.Entity.EntityState.Deleted;
                }
            }
            foreach (var currentAggregator in currentAggregatorsAccess)
            {
                if (!dbUserAggregatorsAccess.Any(o => o.aggregatorId == currentAggregator.Id))
                {
                    AspNetUsersRolesServiceAccess userRolesAggregatoAccess = new AspNetUsersRolesServiceAccess();
                    userRolesAggregatoAccess.aggregatorId = currentAggregator.Id;
                    userRolesAggregatoAccess.roleId = null;
                    userRolesAggregatoAccess.serviceId = null;
                    userRolesAggregatoAccess.userId = user.Id;

                    db.AspNetUsersRolesServiceAccesses.Add(userRolesAggregatoAccess);
                }
            }

        }
        [HttpPost]
        public ActionResult Excel_Export_Save(string contentType, string base64, string fileName)
        {
            var fileContents = Convert.FromBase64String(base64);

            return File(fileContents, contentType, fileName);
        }

        [HttpPost]
        public ActionResult Pdf_Export_Save(string contentType, string base64, string fileName)
        {
            var fileContents = Convert.FromBase64String(base64);

            return File(fileContents, contentType, fileName);
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
    public class myAspNetUsers
    {
        public string Id { get; set; }
        public string name { get; set; }
        public string lastName { get; set; }

        public string userName { get; set; }

        public List<string> roles { get; set; }
        public List<string> services { get; set; }
        public List<string> aggregators { get; set; }
    }
}