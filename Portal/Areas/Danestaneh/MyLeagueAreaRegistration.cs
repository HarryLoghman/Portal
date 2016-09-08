using System.Web.Mvc;

namespace Portal.Areas.Danestaneh
{
    public class DanestanehAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Danestaneh";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Danestaneh_default",
                "Danestaneh/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}