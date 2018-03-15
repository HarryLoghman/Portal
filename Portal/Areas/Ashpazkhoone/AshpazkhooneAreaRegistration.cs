using System.Web.Mvc;

namespace Portal.Areas.Ashpazkhoone
{
    public class AshpazkhooneAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Ashpazkhoone";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Ashpazkhoone_default",
                "Ashpazkhoone/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}