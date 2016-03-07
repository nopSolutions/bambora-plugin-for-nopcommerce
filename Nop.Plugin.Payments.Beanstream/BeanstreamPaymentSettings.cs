using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.Beanstream
{
    public class BeanstreamPaymentSettings : ISettings
    {
        public int CurrencyId { get; set; }
        public string HashValue { get; set; }
        public string USMerchantId { get; set; }
        public string CanadianMerchantId { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }
        /// <summary>
        /// Additional fee
        /// </summary>
        public decimal AdditionalFee { get; set; }
    }
}
