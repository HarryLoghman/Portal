using System.Web.Mvc;

namespace Portal.Areas.ShahreKalameh
{
    public class ShahreKalamehAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "ShahreKalameh";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "ShahreKalameh_default",
                "ShahreKalameh/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}