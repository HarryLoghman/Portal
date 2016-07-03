using System.Web.Mvc;

namespace Portal.Areas.Rejim
{
    public class RejimAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Rejim";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Rejim_default",
                "Rejim/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}