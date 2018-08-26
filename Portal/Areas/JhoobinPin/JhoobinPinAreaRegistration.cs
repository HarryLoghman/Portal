using System.Web.Mvc;

namespace Portal.Areas.JhoobinPin
{
    public class JhoobinPinAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "JhoobinPin";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "JhoobinPin_default",
                "JhoobinPin/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}