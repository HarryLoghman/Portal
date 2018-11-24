using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity.EntityFramework;
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
    public class RolesController : Controller
    {
        // GET: Tools/Roles
        public ActionResult Index()
        {
            return View();
        }
        private PortalEntities db = new PortalEntities();

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Roles_Read([DataSourceRequest]DataSourceRequest request)
        {

            List<myAspNetRoles> myRoles = new List<myAspNetRoles>();
            var vw = db.vw_AspNetRoles.ToList();
            myAspNetRoles my;
            for (int i = 0; i <= vw.Count - 1; i++)
            {
                my = new myAspNetRoles();
                my.Id = vw[i].Id;
                my.name = vw[i].Name;
                my.users = (vw[i].users == null ? null : vw[i].users.Split(',').ToList());
                my.services = (vw[i].Services == null ? null : vw[i].Services.Split(',').ToList());
                my.aggregators = (vw[i].Aggregators == null ? null : vw[i].Aggregators.Split(',').ToList());
                myRoles.Add(my);
            }
            DataSourceResult result = myRoles.ToDataSourceResult(request, users => new
            {
                Id = users.Id,
                name = users.name,
                users = users.users,
                services = users.services,
                aggregators = users.aggregators
            });
            return Json(result, JsonRequestBehavior.AllowGet);

        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Roles_Update([DataSourceRequest]DataSourceRequest request, [Bind] myAspNetRoles role)
        {

            if (ModelState.IsValid)
            {
                try
                {
                    bool error = false;
                    if (role.name.ToLower() == "admin" && role.users.Count == 0)
                    {
                        ModelState.AddModelError("error", "The application will not have any user with 'Admin' role. 'Admin' role should have at least one member");
                        error = true;
                    }
                    else { }
                    bool isUserAdmin = role.users.Any(o => o.ToLower() == "admin");

                    if (!error)
                    {
                        var roleManager = HttpContext.GetOwinContext().Get<ApplicationRoleManager>();

                        var applicationRole = roleManager.FindByIdAsync(role.Id).Result;

                        this.setUsers(role);
                        //userManager.ResetPassword("admin",)

                        this.setServices(role);
                        this.setAggregators(role);

                        db.SaveChanges();


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

            return Json(new[] { role }.ToDataSourceResult(request, ModelState), JsonRequestBehavior.AllowGet);
        }

        private void setUsers(myAspNetRoles role)
        {
            List<AspNetUser> lstSelectedUsersInDB = db.AspNetUsers.Where(o => role.users.Contains(o.UserName)).ToList();
            List<vw_AspNetUserRoles> lstUsersInDb = db.vw_AspNetUserRoles.Where(o => o.RoleId == role.Id).ToList();

            List<string> lstShouldDeleteUsersFromRole = lstUsersInDb.Where(o => !lstSelectedUsersInDB.Any(p => p.Id == o.UserId)).Select(o => o.UserId).ToList();
            var userManager = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            for (int i = 0; i <= lstShouldDeleteUsersFromRole.Count - 1; i++)
            {
                var applicationUser = userManager.FindByIdAsync(lstShouldDeleteUsersFromRole[i]).Result;
                for (int j = 0; j <= applicationUser.Roles.Count - 1; j++)
                {
                    var appRole = applicationUser.Roles.ElementAt(j);
                    if (appRole.RoleId == role.Id)
                    {
                        bool removeResult = applicationUser.Roles.Remove(appRole);
                        if (removeResult)
                        {
                            var result = userManager.UpdateAsync(applicationUser).Result;
                        }
                    }
                }
            }
            List<string> lstShouldAddUsersToRole = lstSelectedUsersInDB.Where(o => !lstUsersInDb.Any(p => p.UserId == o.Id)).Select(o => o.Id).ToList();
            for (int i = 0; i <= lstShouldAddUsersToRole.Count - 1; i++)
            {
                var applicationUser = userManager.FindByIdAsync(lstShouldAddUsersToRole[i]).Result;
                Microsoft.AspNet.Identity.EntityFramework.IdentityUserRole iRole = new Microsoft.AspNet.Identity.EntityFramework.IdentityUserRole();
                iRole.RoleId = role.Id;
                iRole.UserId = applicationUser.Id;
                applicationUser.Roles.Add(iRole);
                var result = userManager.UpdateAsync(applicationUser).Result;
            }


        }

        private void setServices(myAspNetRoles role)
        {
            var dbRoleServicesAccess = db.AspNetUsersRolesServiceAccesses.Where(o => o.roleId == role.Id && o.serviceId != null);
            if (role.services == null)
            {
                foreach (var dbRoleService in dbRoleServicesAccess)
                {
                    db.Entry(dbRoleService).State = System.Data.Entity.EntityState.Deleted;
                }
                return;
            }
            var currentSevicesAccess = db.Services.Where(o => role.services.Contains(o.ServiceCode));

            foreach (var dbRoleService in dbRoleServicesAccess)
            {
                if (!currentSevicesAccess.Any(o => o.Id == dbRoleService.serviceId))
                {
                    db.Entry(dbRoleService).State = System.Data.Entity.EntityState.Deleted;
                }
            }
            foreach (var currentService in currentSevicesAccess)
            {
                if (!dbRoleServicesAccess.Any(o => o.serviceId == currentService.Id))
                {
                    AspNetUsersRolesServiceAccess userRolesServiceAccess = new AspNetUsersRolesServiceAccess();
                    userRolesServiceAccess.aggregatorId = null;
                    userRolesServiceAccess.roleId = role.Id;
                    userRolesServiceAccess.serviceId = currentService.Id;
                    userRolesServiceAccess.userId = null;

                    db.AspNetUsersRolesServiceAccesses.Add(userRolesServiceAccess);
                }
            }

        }

        private void setAggregators(myAspNetRoles role)
        {
            var dbRoleAggregatorsAccess = db.AspNetUsersRolesServiceAccesses.Where(o => o.roleId == role.Id && o.aggregatorId != null);
            if (role.aggregators == null)
            {
                foreach (var dbRoleAggregator in dbRoleAggregatorsAccess)
                {
                    db.Entry(dbRoleAggregator).State = System.Data.Entity.EntityState.Deleted;
                }
                return;
            }
            var currentAggregatorsAccess = db.Aggregators.Where(o => role.aggregators.Contains(o.AggregatorName));

            foreach (var dbUserAggregator in dbRoleAggregatorsAccess)
            {
                if (!currentAggregatorsAccess.Any(o => o.Id == dbUserAggregator.aggregatorId))
                {
                    db.Entry(dbUserAggregator).State = System.Data.Entity.EntityState.Deleted;
                }
            }
            foreach (var currentAggregator in currentAggregatorsAccess)
            {
                if (!dbRoleAggregatorsAccess.Any(o => o.aggregatorId == currentAggregator.Id))
                {
                    AspNetUsersRolesServiceAccess userRolesAggregatorAccess = new AspNetUsersRolesServiceAccess();
                    userRolesAggregatorAccess.aggregatorId = currentAggregator.Id;
                    userRolesAggregatorAccess.roleId = role.Id;
                    userRolesAggregatorAccess.serviceId = null;
                    userRolesAggregatorAccess.userId = null;

                    db.AspNetUsersRolesServiceAccesses.Add(userRolesAggregatorAccess);
                }
            }

        }
    }
    public class myAspNetRoles
    {
        public string Id { get; set; }
        public string name { get; set; }

        public List<string> users { get; set; }
        public List<string> services { get; set; }
        public List<string> aggregators { get; set; }
    }
}