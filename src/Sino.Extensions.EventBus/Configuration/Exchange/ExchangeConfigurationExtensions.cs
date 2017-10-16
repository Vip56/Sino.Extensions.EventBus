namespace Sino.Extensions.EventBus.Configuration
{
    public static class ExchangeConfigurationExtensions
    {
        public static bool IsDefaultExchange(this ExchangeConfiguration configuration)
        {
            return string.IsNullOrEmpty(configuration.ExchangeName);
        }
    }
}
