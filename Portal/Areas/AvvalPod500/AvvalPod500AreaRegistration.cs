using System.Web.Mvc;

namespace Portal.Areas.AvvalPod500
{
    public class AvvalPod500AreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "AvvalPod500";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "AvvalPod500_default",
                "AvvalPod500/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}