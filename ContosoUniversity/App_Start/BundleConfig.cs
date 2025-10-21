using System.Web;
using System.Web.Optimization;

namespace ContosoUniversity
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            // Use the non-minified version and disable transforms
            var bootstrapBundle = new Bundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.bundle.js");
            bundles.Add(bootstrapBundle);

            // Or if you must use minified, disable minification:
            // var bootstrapBundle = new ScriptBundle("~/bundles/bootstrap").Include(
            //           "~/Scripts/bootstrap.bundle.min.js");
            // bootstrapBundle.Transforms.Clear();
            // bundles.Add(bootstrapBundle);

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/site.css"));

            // Enable optimizations but disable minification for problematic bundles
            BundleTable.EnableOptimizations = true;
        }
    }
}