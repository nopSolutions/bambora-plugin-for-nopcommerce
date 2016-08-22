using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.Beanstream.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.Beanstream.Controllers
{
    public class PaymentBeanstreamController : BasePaymentController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderService _orderService;
        private readonly ISettingService _settingService;
        private readonly IStoreService _storeService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public PaymentBeanstreamController(ILocalizationService localizationService,
            ILogger logger,
            IOrderProcessingService orderProcessingService,
            IOrderService orderService,
            ISettingService settingService,
            IStoreService storeService,
            IWorkContext workContext)
        {
            this._localizationService = localizationService;
            this._logger = logger;
            this._orderProcessingService = orderProcessingService;
            this._orderService = orderService;
            this._settingService = settingService;
            this._storeService = storeService;
            this._workContext = workContext;
        }

        #endregion

        #region Utilities

        private IDictionary<string, string> GetParameters(NameValueCollection requestParams)
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>();

            parameters.Add("trnApproved", requestParams["trnApproved"]);
            parameters.Add("trnId", requestParams["trnId"]);
            parameters.Add("messageId", requestParams["messageId"]);
            parameters.Add("messageText", requestParams["messageText"]);
            parameters.Add("authCode", requestParams["authCode"]);
            parameters.Add("responseType", requestParams["responseType"]);
            parameters.Add("trnAmount", requestParams["trnAmount"]);
            parameters.Add("trnDate", requestParams["trnDate"]);
            parameters.Add("trnOrderNumber", requestParams["trnOrderNumber"]);
            parameters.Add("trnLanguage", requestParams["trnLanguage"]);
            parameters.Add("trnCustomerName", requestParams["trnCustomerName"]);
            parameters.Add("trnEmailAddress", requestParams["trnEmailAddress"]);
            parameters.Add("trnPhoneNumber", requestParams["trnPhoneNumber"]);
            parameters.Add("avsProcessed", requestParams["avsProcessed"]);
            parameters.Add("avsId", requestParams["avsId"]);
            parameters.Add("avsResult", requestParams["avsResult"]);
            parameters.Add("avsPostalMatch", requestParams["avsPostalMatch"]);
            parameters.Add("avsMessage", requestParams["avsMessage"]);
            parameters.Add("cvdId", requestParams["cvdId"]);
            parameters.Add("cardType", requestParams["cardType"]);
            parameters.Add("trnType", requestParams["trnType"]);
            parameters.Add("paymentMethod", requestParams["paymentMethod"]);

            return parameters;
        }

        #endregion

        #region Methods

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var beanstreamPaymentSettings = _settingService.LoadSetting<BeanstreamPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                MerchantId = beanstreamPaymentSettings.MerchantId,
                HashKey = beanstreamPaymentSettings.HashKey,
                AdditionalFee = beanstreamPaymentSettings.AdditionalFee,
                AdditionalFeePercentage = beanstreamPaymentSettings.AdditionalFeePercentage,
                ActiveStoreScopeConfiguration = storeScope
            };

            if (storeScope > 0)
            {
                model.MerchantId_OverrideForStore = _settingService.SettingExists(beanstreamPaymentSettings, x => x.MerchantId, storeScope);
                model.HashKey_OverrideForStore = _settingService.SettingExists(beanstreamPaymentSettings, x => x.HashKey, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(beanstreamPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(beanstreamPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
            }

            return View("~/Plugins/Payments.Beanstream/Views/PaymentBeanstream/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var beanstreamPaymentSettings = _settingService.LoadSetting<BeanstreamPaymentSettings>(storeScope);

            //save settings
            beanstreamPaymentSettings.MerchantId = model.MerchantId;
            beanstreamPaymentSettings.HashKey = model.HashKey;
            beanstreamPaymentSettings.AdditionalFee = model.AdditionalFee;
            beanstreamPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(beanstreamPaymentSettings, x => x.MerchantId, model.MerchantId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(beanstreamPaymentSettings, x => x.HashKey, model.HashKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(beanstreamPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(beanstreamPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            return View("~/Plugins/Payments.Beanstream/Views/PaymentBeanstream/PaymentInfo.cshtml");
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            return new List<string>();
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            return new ProcessPaymentRequest();
        }        

        [ValidateInput(false)]
        public ActionResult ResultHandler()
        {
            var parameters = GetParameters(Request.Params);
            int orderId;
            if (!int.TryParse(parameters["trnOrderNumber"], out orderId))
                return RedirectToRoute("HomePage");

            var order = _orderService.GetOrderById(orderId);
            if (order == null)
                return RedirectToRoute("HomePage");

            var sb = new StringBuilder();
            sb.AppendLine("Beanstream payment result:");
            foreach (var parameter in parameters)
            {
                sb.AppendFormat("{0}: {1}{2}", parameter.Key, parameter.Value, Environment.NewLine);
            }

            //order note
            order.OrderNotes.Add(new OrderNote()
            {
                Note = sb.ToString(),
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });
            _orderService.UpdateOrder(order);

            return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
        }

        [ValidateInput(false)]
        public ActionResult ResponseNotificationHandler()
        {
            var parameters = GetParameters(Request.Params);
            int orderId;
            if (!int.TryParse(parameters["trnOrderNumber"], out orderId))
                return Content("");

            var sb = new StringBuilder();
            sb.AppendLine("Beanstream response notification:");
            foreach (var parameter in parameters)
            {
                sb.AppendFormat("{0}: {1}{2}", parameter.Key, parameter.Value, Environment.NewLine);
            }

            var order = _orderService.GetOrderById(orderId);
            if (order != null)
            {
                //order note
                order.OrderNotes.Add(new OrderNote()
                {
                    Note = sb.ToString(),
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });
                _orderService.UpdateOrder(order);

                //validate order total
                decimal total;
                if (!decimal.TryParse(parameters["trnAmount"], out total))
                {
                    _logger.Error(string.Format("Beanstream response notification. {0} for the order #{1}", parameters["messageText"], orderId));
                    return Content("");
                }

                if (!Math.Round(total, 2).Equals(Math.Round(order.OrderTotal, 2)))
                {
                    _logger.Error(string.Format("Beanstream response notification. Returned order total {0} doesn't equal order total {1} for the order #{2}",
                        total, order.OrderTotal, orderId));
                    return Content("");
                }

                //change order status
                if (parameters["trnApproved"].Equals("1"))
                {
                    if (_orderProcessingService.CanMarkOrderAsPaid(order))
                    {
                        order.AuthorizationTransactionId = parameters["trnId"];
                        _orderService.UpdateOrder(order);
                        _orderProcessingService.MarkOrderAsPaid(order);
                    }
                }
            }
            else
                _logger.Error("Beanstream response notification. Order is not found", new NopException(sb.ToString()));

            //nothing should be rendered to visitor
            return Content("");
        }

        #endregion
    }
}