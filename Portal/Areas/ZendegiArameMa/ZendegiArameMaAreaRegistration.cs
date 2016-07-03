using System.Web.Mvc;

namespace Portal.Areas.ZendegiArameMa
{
    public class ZendegiArameMaAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "ZendegiArameMa";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "ZendegiArameMa_default",
                "ZendegiArameMa/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}