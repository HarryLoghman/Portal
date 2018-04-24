using System.Web.Mvc;

namespace Portal.Areas.PorShetab
{
    public class PorShetabAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "PorShetab";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "PorShetab_default",
                "PorShetab/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}