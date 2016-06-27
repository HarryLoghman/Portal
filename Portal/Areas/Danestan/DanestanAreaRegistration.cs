using System.Web.Mvc;

namespace Portal.Areas.Danestan
{
    public class DanestanAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Danestan";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Danestan_default",
                "Danestan/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}