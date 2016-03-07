using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.Beanstream.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Beanstream.Fields.CurrencyId")]
        public int CurrencyId { get; set; }
        public bool CurrencyId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Beanstream.Fields.HashValue")]
        public string HashValue { get; set; }
        public bool HashValue_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Beanstream.Fields.MerchantId")]
        public string USMerchantId { get; set; }
        public bool USMerchantId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Beanstream.Fields.MerchantId")]
        public string CanadianMerchantId { get; set; }
        public bool CanadianMerchantId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Beanstream.Fields.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Beanstream.Fields.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }
    }
}