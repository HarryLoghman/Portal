using System.Web.Mvc;

namespace Portal.Areas.Achar
{
    public class AcharAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Achar";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Achar_default",
                "Achar/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}