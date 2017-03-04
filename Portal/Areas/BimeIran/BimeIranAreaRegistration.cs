using System.Web.Mvc;

namespace Portal.Areas.BimeIran
{
    public class BimeIranAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "BimeIran";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "BimeIran_default",
                "BimeIran/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}