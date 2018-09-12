using System.Web.Mvc;

namespace Portal.Areas.JhoobinMusicYad
{
    public class JhoobinMusicYadAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "JhoobinMusicYad";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "JhoobinMusicYad_default",
                "JhoobinMusicYad/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}