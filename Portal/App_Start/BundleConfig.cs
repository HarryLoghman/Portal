using System.Web.Optimization;

namespace Portal
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/respond.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/signin.css",
                      "~/Content/site.css"));

            bundles.Add(new ScriptBundle("~/bundles/kendojs").Include(
                    "~/Scripts/kendo/2016.2.504/jquery.min.js",
                    "~/Scripts/kendo/2016.2.504/angular.min.js",
                    "~/Scripts/kendo/2016.2.504/jszip.min.js",
                    "~/Scripts/kendo/2016.2.504/kendo.all.min.js",
                    "~/Scripts/kendo/2016.2.504/kendo.aspnetmvc.min.js",
                    "~/Scripts/kendo/2016.2.504/fa-IR.js",
                    "~/Scripts/kendo/2016.2.504/messages/kendo.messages.fa-IR.js"
                    ));

            bundles.Add(new ScriptBundle("~/bundles/Portaljs").Include(
            "~/Scripts/Portal.js",
            "~/Scripts/jdate.min.js"));

            bundles.Add(new StyleBundle("~/Content/PortalStyles").Include(
            "~/Content/Portal.css"
            ));

            bundles.Add(new StyleBundle("~/Content/kendo/2016.2.504/Styles").Include(
                    "~/Content/kendo/2016.2.504/kendo.common-material.min.css",
                    "~/Content/kendo/2016.2.504/kendo.mobile.all.min.css",
                    "~/Content/kendo/2016.2.504/kendo.dataviz.min.css",
                    "~/Content/kendo/2016.2.504/kendo.materialblack.min.css",
                    "~/Content/kendo/2016.2.504/kendo.dataviz.materialblack.min.css",
                    "~/Content/kendo/2016.2.504/kendo.rtl.min.css"
                    ));
        }
    }
}
