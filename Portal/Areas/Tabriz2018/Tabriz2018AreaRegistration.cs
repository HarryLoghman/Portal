using System.Web.Mvc;

namespace Portal.Areas.Tabriz2018
{
    public class Tabriz2018AreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Tabriz2018";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Tabriz2018_default",
                "Tabriz2018/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}