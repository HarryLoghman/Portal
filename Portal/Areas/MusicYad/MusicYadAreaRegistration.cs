using System.Web.Mvc;

namespace Portal.Areas.MusicYad
{
    public class MusicYadAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "MusicYad";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "MusicYad_default",
                "MusicYad/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}