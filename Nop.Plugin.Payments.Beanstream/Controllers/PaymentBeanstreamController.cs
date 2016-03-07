using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.Beanstream.Models;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.Beanstream.Controllers
{
    public class PaymentBeanstreamController : BasePaymentController
    {
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ILogger _logger;

        public PaymentBeanstreamController(IWorkContext workContext,
            IStoreService storeService, 
            ISettingService settingService, 
            IOrderService orderService, 
            IOrderProcessingService orderProcessingService, 
            ILogger logger)
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._logger = logger;
        }
        
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var beanstreamPaymentSettings = _settingService.LoadSetting<BeanstreamPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                CurrencyId = beanstreamPaymentSettings.CurrencyId,
                USMerchantId = beanstreamPaymentSettings.USMerchantId,
                CanadianMerchantId = beanstreamPaymentSettings.CanadianMerchantId,
                HashValue = beanstreamPaymentSettings.HashValue,
                AdditionalFee = beanstreamPaymentSettings.AdditionalFee,
                AdditionalFeePercentage = beanstreamPaymentSettings.AdditionalFeePercentage
            };

            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.CurrencyId_OverrideForStore = _settingService.SettingExists(beanstreamPaymentSettings, x => x.CurrencyId, storeScope);
                model.USMerchantId_OverrideForStore = _settingService.SettingExists(beanstreamPaymentSettings, x => x.USMerchantId, storeScope);
                model.CanadianMerchantId_OverrideForStore = _settingService.SettingExists(beanstreamPaymentSettings, x => x.CanadianMerchantId, storeScope);
                model.HashValue_OverrideForStore = _settingService.SettingExists(beanstreamPaymentSettings, x => x.HashValue, storeScope);
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
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var beanstreamPaymentSettings = _settingService.LoadSetting<BeanstreamPaymentSettings>(storeScope);

            //save settings
            beanstreamPaymentSettings.CurrencyId = model.CurrencyId;
            beanstreamPaymentSettings.HashValue = model.HashValue;
            beanstreamPaymentSettings.USMerchantId = model.USMerchantId;
            beanstreamPaymentSettings.CanadianMerchantId = model.CanadianMerchantId;
            beanstreamPaymentSettings.AdditionalFee = model.AdditionalFee;
            beanstreamPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            if (model.CurrencyId_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(beanstreamPaymentSettings, x => x.CurrencyId, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(beanstreamPaymentSettings, x => x.CurrencyId, storeScope);

            if (model.HashValue_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(beanstreamPaymentSettings, x => x.HashValue, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(beanstreamPaymentSettings, x => x.HashValue, storeScope);

            if (model.USMerchantId_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(beanstreamPaymentSettings, x => x.USMerchantId, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(beanstreamPaymentSettings, x => x.USMerchantId, storeScope);

            if (model.CanadianMerchantId_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(beanstreamPaymentSettings, x => x.CanadianMerchantId, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(beanstreamPaymentSettings, x => x.CanadianMerchantId, storeScope);

            if (model.AdditionalFee_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(beanstreamPaymentSettings, x => x.AdditionalFee, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(beanstreamPaymentSettings, x => x.AdditionalFee, storeScope);

            if (model.AdditionalFeePercentage_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(beanstreamPaymentSettings, x => x.AdditionalFeePercentage, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(beanstreamPaymentSettings, x => x.AdditionalFeePercentage, storeScope);

            //now clear settings cache
            _settingService.ClearCache();

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
            var warnings = new List<string>();
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }

        private IDictionary<String, String> GetParameters(NameValueCollection requestParams)
        {
            IDictionary<String, String> parameters = new Dictionary<String, String>();

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

        [ValidateInput(false)]
        public ActionResult ResultHandler()
        {
            var parameters = GetParameters(Request.Params);
            var orderId = int.Parse(parameters["trnOrderNumber"]);
            var order = _orderService.GetOrderById(orderId);

            if (order != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Beanstream payment result:");
                foreach (var parameter in parameters)
                {
                    sb.AppendLine(parameter.Key + ": " + parameter.Value);
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
            else
            {
                return RedirectToRoute("HomePage");
            }
        }

        [ValidateInput(false)]
        public ActionResult ResponseNotificationHandler()
        {
            var parameters = GetParameters(Request.Params);
            var sb = new StringBuilder();
            sb.AppendLine("Beanstream response notification:");
            foreach (var parameter in parameters)
            {
                sb.AppendLine(parameter.Key + ": " + parameter.Value);
            }

            var orderId = int.Parse(parameters["trnOrderNumber"]);
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
                var total = decimal.Parse(parameters["trnAmount"]);
                if (!Math.Round(total, 2).Equals(Math.Round(order.OrderTotal, 2)))
                {
                    string errorStr = string.Format("Beanstream response notification. Returned order total {0} doesn't equal order total {1}", total, order.OrderTotal);
                    _logger.Error(errorStr);

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
            {
                _logger.Error("Beanstream response notification. Order is not found", new NopException(sb.ToString()));
            }

            //nothing should be rendered to visitor
            return Content("");
        }
    }
}