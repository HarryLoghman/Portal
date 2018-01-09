using System.Web.Mvc;

namespace Portal.Areas.ShenoYad500
{
    public class ShenoYad500AreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "ShenoYad500";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "ShenoYad500_default",
                "ShenoYad500/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}