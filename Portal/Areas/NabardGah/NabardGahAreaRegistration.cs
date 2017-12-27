using System.Web.Mvc;

namespace Portal.Areas.NabardGah
{
    public class NabardGahAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "NabardGah";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "NabardGah_default",
                "NabardGah/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}