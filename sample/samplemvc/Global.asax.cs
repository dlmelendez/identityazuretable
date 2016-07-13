using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace samplemvc
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            //ElCamino - Added to create azure tables
            ApplicationUserManager.StartupAsync();
            //safe to remove after tables are created once.
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
