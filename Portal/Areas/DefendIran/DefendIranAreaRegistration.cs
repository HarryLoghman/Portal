using System.Web.Mvc;

namespace Portal.Areas.DefendIran
{
    public class DefendIranAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "DefendIran";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "DefendIran_default",
                "DefendIran/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}