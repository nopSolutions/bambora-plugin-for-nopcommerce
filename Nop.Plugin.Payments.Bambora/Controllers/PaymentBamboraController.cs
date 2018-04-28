using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.Bambora.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.Bambora.Controllers
{
    public class PaymentBamboraController : BasePaymentController
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

        public PaymentBamboraController(ILocalizationService localizationService,
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

        private IDictionary<string, string> GetParameters(IpnModel model)
        {
            var requestParams = model.Form.ToDictionary(pair => pair.Key, pair => pair.Value.ToString());
            foreach (var keyValuePair in Request.Query.Where(pair=>!requestParams.ContainsKey(pair.Key)))
            {
                requestParams.Add(keyValuePair.Key, keyValuePair.Value);
            }

            IDictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "trnApproved", requestParams["trnApproved"] },
                { "trnId", requestParams["trnId"] },
                { "messageId", requestParams["messageId"] },
                { "messageText", requestParams["messageText"] },
                { "authCode", requestParams["authCode"] },
                { "responseType", requestParams["responseType"] },
                { "trnAmount", requestParams["trnAmount"] },
                { "trnDate", requestParams["trnDate"] },
                { "trnOrderNumber", requestParams["trnOrderNumber"] },
                { "trnLanguage", requestParams["trnLanguage"] },
                { "trnCustomerName", requestParams["trnCustomerName"] },
                { "trnEmailAddress", requestParams["trnEmailAddress"] },
                { "trnPhoneNumber", requestParams["trnPhoneNumber"] },
                { "avsProcessed", requestParams["avsProcessed"] },
                { "avsId", requestParams["avsId"] },
                { "avsResult", requestParams["avsResult"] },
                { "avsPostalMatch", requestParams["avsPostalMatch"] },
                { "avsMessage", requestParams["avsMessage"] },
                { "cvdId", requestParams["cvdId"] },
                { "cardType", requestParams["cardType"] },
                { "trnType", requestParams["trnType"] },
                { "paymentMethod", requestParams["paymentMethod"] }
            };

            return parameters;
        }

        #endregion

        #region Methods

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var bamboraPaymentSettings = _settingService.LoadSetting<BamboraPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                MerchantId = bamboraPaymentSettings.MerchantId,
                HashKey = bamboraPaymentSettings.HashKey,
                AdditionalFee = bamboraPaymentSettings.AdditionalFee,
                AdditionalFeePercentage = bamboraPaymentSettings.AdditionalFeePercentage,
                ActiveStoreScopeConfiguration = storeScope
            };

            if (storeScope > 0)
            {
                model.MerchantId_OverrideForStore = _settingService.SettingExists(bamboraPaymentSettings, x => x.MerchantId, storeScope);
                model.HashKey_OverrideForStore = _settingService.SettingExists(bamboraPaymentSettings, x => x.HashKey, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(bamboraPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(bamboraPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
            }

            return View("~/Plugins/Payments.Bambora/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var bamboraPaymentSettings = _settingService.LoadSetting<BamboraPaymentSettings>(storeScope);

            //save settings
            bamboraPaymentSettings.MerchantId = model.MerchantId;
            bamboraPaymentSettings.HashKey = model.HashKey;
            bamboraPaymentSettings.AdditionalFee = model.AdditionalFee;
            bamboraPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(bamboraPaymentSettings, x => x.MerchantId, model.MerchantId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(bamboraPaymentSettings, x => x.HashKey, model.HashKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(bamboraPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(bamboraPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        public ActionResult ResultHandler(IpnModel model)
        {
            var parameters = GetParameters(model);
            int orderId;
            if (!int.TryParse(parameters["trnOrderNumber"], out orderId))
                return RedirectToRoute("HomePage");

            var order = _orderService.GetOrderById(orderId);
            if (order == null)
                return RedirectToRoute("HomePage");

            var sb = new StringBuilder();
            sb.AppendLine("Bambora payment result:");
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

        public ActionResult ResponseNotificationHandler(IpnModel model)
        {
            var parameters = GetParameters(model);
            int orderId;
            if (!int.TryParse(parameters["trnOrderNumber"], out orderId))
                return Content("");

            var sb = new StringBuilder();
            sb.AppendLine("Bambora response notification:");
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
                    _logger.Error($"Bambora response notification. {parameters["messageText"]} for the order #{orderId}");
                    return Content("");
                }

                if (!Math.Round(total, 2).Equals(Math.Round(order.OrderTotal, 2)))
                {
                    _logger.Error($"Bambora response notification. Returned order total {total} doesn't equal order total {order.OrderTotal} for the order #{orderId}");
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
                _logger.Error("Bambora response notification. Order is not found", new NopException(sb.ToString()));

            //nothing should be rendered to visitor
            return Content("");
        }

        #endregion
    }
}