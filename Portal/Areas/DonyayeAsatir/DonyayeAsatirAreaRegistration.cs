using System.Web.Mvc;

namespace Portal.Areas.DonyayeAsatir
{
    public class DonyayeAsatirAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "DonyayeAsatir";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "DonyayeAsatir_default",
                "DonyayeAsatir/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}