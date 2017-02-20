using System.Web.Mvc;

namespace Portal.Areas.JabehAbzar
{
    public class JabehAbzarAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "JabehAbzar";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "JabehAbzar_default",
                "JabehAbzar/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}