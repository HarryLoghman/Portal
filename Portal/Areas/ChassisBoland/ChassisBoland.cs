using System.Web.Mvc;

namespace Portal.Areas.ChassisBoland
{
    public class ChassisBolandAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "ChassisBoland";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "ChassisBoland_default",
                "ChassisBoland/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}