using System.Web.Mvc;

namespace Portal.Areas.BimeKarbala
{
    public class BimeKarbalaAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "BimeKarbala";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "BimeKarbala_default",
                "BimeKarbala/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}