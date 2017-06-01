using System.Web.Mvc;

namespace Portal.Areas.MenchBaz
{
    public class MenchBazAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "MenchBaz";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "MenchBaz_default",
                "MenchBaz/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}