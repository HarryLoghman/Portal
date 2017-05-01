using System.Web.Mvc;

namespace Portal.Areas.ShenoYad
{
    public class ShenoYadAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "ShenoYad";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "ShenoYad_default",
                "ShenoYad/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}