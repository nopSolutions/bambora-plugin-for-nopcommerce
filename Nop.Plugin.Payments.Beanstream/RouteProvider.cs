using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.Beanstream
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            //payment result
            routes.MapRoute("Plugin.Payments.Beanstream.ResultHandler",
                 "Plugins/PaymentBeanstream/ResultHandler",
                 new { controller = "PaymentBeanstream", action = "ResultHandler" },
                 new[] { "Nop.Plugin.Payments.Beanstream.Controllers" }
            );

            //response notification
            routes.MapRoute("Plugin.Payments.Beanstream.ResponseNotificationHandler",
                 "Plugins/PaymentBeanstream/ResponseNotificationHandler",
                 new { controller = "PaymentBeanstream", action = "ResponseNotificationHandler" },
                 new[] { "Nop.Plugin.Payments.Beanstream.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
