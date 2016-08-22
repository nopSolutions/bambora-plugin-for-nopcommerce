using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.Beanstream
{
    public class BeanstreamPaymentSettings : ISettings
    {
        /// <summary>
        /// Gets or Beanstream sets merchant ID
        /// </summary>
        public string MerchantId { get; set; }

        /// <summary>
        /// Gets or sets hash key
        /// </summary>
        public string HashKey { get; set; }

        /// <summary>
        /// Gets or sets an additional fee
        /// </summary>
        public decimal AdditionalFee { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }
    }
}
