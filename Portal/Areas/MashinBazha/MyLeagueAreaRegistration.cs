using System.Web.Mvc;

namespace Portal.Areas.MashinBazha
{
    public class MashinBazhaAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "MashinBazha";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "MashinBazha_default",
                "MashinBazha/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}