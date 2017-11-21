using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.Bambora
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IRouteBuilder routeBuilder)
        {
            //payment result
            routeBuilder.MapRoute("Plugin.Payments.Bambora.ResultHandler",
                 "Plugins/PaymentBambora/ResultHandler",
                 new { controller = "PaymentBambora", action = "ResultHandler" });

            //response notification
            routeBuilder.MapRoute("Plugin.Payments.Bambora.ResponseNotificationHandler",
                 "Plugins/PaymentBambora/ResponseNotificationHandler",
                 new { controller = "PaymentBambora", action = "ResponseNotificationHandler" });
        }

        public int Priority
        {
            get { return 0; }
        }
    }
}
