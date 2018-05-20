using System.Web.Mvc;

namespace Portal.Areas.TajoTakht
{
    public class TajoTakhtAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "TajoTakht";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "TajoTakht_default",
                "TajoTakht/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}