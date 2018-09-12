using System.Web.Mvc;

namespace Portal.Areas.JhoobinTahChin
{
    public class JhoobinTahChinAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "JhoobinTahChin";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "JhoobinTahChin_default",
                "JhoobinTahChin/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}