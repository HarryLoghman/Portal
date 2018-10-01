using System.Web.Mvc;

namespace Portal.Areas.Halghe
{
    public class HalgheAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Halghe";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Halghe_default",
                "Halghe/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}