using System.Web.Mvc;

namespace Portal.Areas.SepidRood
{
    public class SepidRoodAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "SepidRood";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "SepidRood_default",
                "SepidRood/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}