using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Kaakateeya.Customer.NotificationService
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {
                NotificationService notificationService = serviceProvider.GetService<NotificationService>();
                await notificationService.DeleteAllDisabledEndpoints();
                await notificationService.CreateAndSaveMobileEndpoints();
                await notificationService.PublishMessage();
            }
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            services.AddTransient<NotificationService>().AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddNLog("nlog.config");
            });

        }
    }
}
