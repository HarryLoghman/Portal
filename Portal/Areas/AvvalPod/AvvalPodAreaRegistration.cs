using System.Web.Mvc;

namespace Portal.Areas.AvvalPod
{
    public class AvvalPodAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "AvvalPod";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "AvvalPod_default",
                "AvvalPod/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}