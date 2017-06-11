using System.Web.Mvc;

namespace Portal.Areas.AvvalYad
{
    public class AvvalYadAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "AvvalYad";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "AvvalYad_default",
                "AvvalYad/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}