using System.Web.Mvc;

namespace Portal.Areas.JhoobinDambel
{
    public class JhoobinDambelAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "JhoobinDambel";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "JhoobinDambel_default",
                "JhoobinDambel/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}