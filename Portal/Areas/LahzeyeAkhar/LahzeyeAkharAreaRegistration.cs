using System.Web.Mvc;

namespace Portal.Areas.LahzeyeAkhar
{
    public class LahzeyeAkharAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "LahzeyeAkhar";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "LahzeyeAkhar_default",
                "LahzeyeAkhar/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}