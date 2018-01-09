using System.Web.Mvc;

namespace Portal.Areas.Tamly500
{
    public class Tamly500AreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Tamly500";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Tamly500_default",
                "Tamly500/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}