using System.Net;

namespace KomeTube.Kernel.Models
{
    /// <summary>
    /// CookieConfigData
    /// </summary>
    public class CookieConfigData
    {
        /// <summary>
        /// Use cookies
        /// </summary>
        public bool UseCookies { get; set; } = false;

        /// <summary>
        /// CookieContainer
        /// </summary>
        public CookieContainer CookieContainer { get; set; } = new CookieContainer();
    }
}