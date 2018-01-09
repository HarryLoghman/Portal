using System.Web.Mvc;

namespace Portal.Areas.BehAmooz500
{
    public class BehAmooz500AreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "BehAmooz500";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "BehAmooz500_default",
                "BehAmooz500/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}