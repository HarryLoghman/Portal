using System.Web.Mvc;

namespace Portal.Areas.JhoobinPorShetab
{
    public class JhoobinPorShetabAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "JhoobinPorShetab";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "JhoobinPorShetab_default",
                "JhoobinPorShetab/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}