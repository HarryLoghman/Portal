using System.Web.Mvc;

namespace Portal.Areas.TahChin
{
    public class TahChinAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "TahChin";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "TahChin_default",
                "TahChin/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}