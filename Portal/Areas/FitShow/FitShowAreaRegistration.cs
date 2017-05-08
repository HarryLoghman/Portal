using System.Web.Mvc;

namespace Portal.Areas.FitShow
{
    public class FitShowAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "FitShow";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "FitShow_default",
                "FitShow/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}