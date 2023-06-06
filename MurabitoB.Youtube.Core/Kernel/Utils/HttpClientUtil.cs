using KomeTube.Kernel.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace KomeTube.Kernel.Utils
{
    /// <summary>
    /// HttpClientUtil
    /// <para>Original Author: Ruikai Feng</para>
    /// <para>Original Source: https://stackoverflow.com/a/72009193</para>
    /// <para>Original License: CC BY-SA 4.0</para>
    /// <para>CC BY-SA 4.0: https://creativecommons.org/licenses/by-sa/4.0/</para>
    /// </summary>
    public class HttpClientUtil
    {
        /// <summary>
        /// User-Agent string
        /// </summary>
        private static readonly string UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:59.0) Gecko/20100101 Firefox/59.0";

        /// <summary>
        /// IServiceProvider
        /// </summary>
        private static IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// Init IServiceProvider
        /// </summary>
        /// <param name="serviceProvider">IServiceProvider</param>
        /// <param name="showLogInConsole">Boolean, Determine to show IHttpClientFactory logs in the console, The default value is true</param>
        public static void InitIServiceProvider(
            IServiceProvider serviceProvider,
            bool showLogInConsole = true)
        {
            if (serviceProvider == null)
            {
                IHostBuilder hostBuilder = Host.CreateDefaultBuilder();

                hostBuilder.ConfigureServices(services =>
                {
                    // Original Author: Nkosi
                    // Original Source: https://stackoverflow.com/a/59685250
                    // Original License: CC BY-SA 4.0
                    // CC BY-SA 4.0: https://creativecommons.org/licenses/by-sa/4.0/
                    CookieConfigData cookieConfigData = new CookieConfigData()
                    {
                        // Ref: https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines
                        UseCookies = false
                    };

                    services.AddHttpClient(Options.DefaultName, configureClient =>
                    {
                        if (configureClient.DefaultRequestHeaders.UserAgent.Any())
                        {
                            configureClient.DefaultRequestHeaders.UserAgent.Clear();
                        }

                        bool canTryParseAdd = configureClient.DefaultRequestHeaders
                            .UserAgent.TryParseAdd(UserAgent);

                        if (canTryParseAdd)
                        {
                            ClientHintsUtil.SetClientHints(configureClient);
                        }
                        else
                        {
                            Debug.WriteLine($"[InitIServiceProvider] Can't try parse add User-Agent string, value: {UserAgent}");
                        }
                    })
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                    {
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                        AllowAutoRedirect = true,
                        CookieContainer = cookieConfigData.CookieContainer,
                        UseCookies = cookieConfigData.UseCookies
                    });
                    services.AddSingleton(cookieConfigData);

                    if (!showLogInConsole)
                    {
                        // Original Author: Stephen Lautier
                        // Original Source: https://stackoverflow.com/a/52970073
                        // Original License: CC BY-SA 4.0
                        // CC BY-SA 4.0: https://creativecommons.org/licenses/by-sa/4.0/
                        services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
                    }
                });

                ServiceProvider = hostBuilder.Build().Services;
            }
            else
            {
                ServiceProvider = serviceProvider;
            }
        }

        /// <summary>
        /// Get IHttpClientFactory
        /// </summary>
        /// <param name="showLogInConsole">Boolean, Determine to show IHttpClientFactory logs in the console, The default value is true</param>
        /// <returns>IHttpClientFactory</returns>
        public static IHttpClientFactory GetClientFactory(bool showLogInConsole = true)
        {
            if (ServiceProvider == null)
            {
                InitIServiceProvider(ServiceProvider, showLogInConsole);
            }

            return ServiceProvider?.GetServices<IHttpClientFactory>()
                ?.FirstOrDefault();
        }

        /// <summary>
        /// Get CookieConfigData
        /// </summary>
        /// <param name="showLogInConsole">Boolean, Determine to show IHttpClientFactory logs in the console, The default value is true</param>
        /// <returns>CookieConfigData</returns>
        public static CookieConfigData GetCookieConfigData(bool showLogInConsole = true)
        {
            if (ServiceProvider == null)
            {
                InitIServiceProvider(ServiceProvider, showLogInConsole);
            }

            return ServiceProvider?.GetServices<CookieConfigData>()
                ?.FirstOrDefault();
        }

        /// <summary>
        /// Create HttpClient
        /// </summary>
        /// <param name="showLogInConsole">Boolean, Determine to show IHttpClientFactory logs in the console, The default value is true</param>
        /// <returns>HttpClient</returns>
        public static HttpClient CreateClient(bool showLogInConsole = true) =>
            GetClientFactory(showLogInConsole).CreateClient();
    }
}