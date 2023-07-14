using KomeTube.Kernel.Utils;
using System.Collections.Generic;
using System.Linq;

namespace KomeTube.Kernel
{
    /// <summary>
    /// CustomFunction
    /// </summary>
    public class CustomFunction
    {
        /// <summary>
        /// Get List&lt;WebBrowserUtil.CookieData&gt;
        /// </summary>
        /// <param name="browserType">WebBrowserUtil.BrowserType</param>
        /// <param name="profileFolderName">String, The profile folder name or path</param>
        /// <returns>List&lt;WebBrowserUtil.CookieData&gt;</returns>
        public static List<WebBrowserUtil.CookieData> GetCookiesList(
            WebBrowserUtil.BrowserType browserType,
            string profileFolderName = "")
        {
            return WebBrowserUtil.GetCookies(
                browserType,
                profileFolderName,
                ".youtube.com");
        }

        /// <summary>
        /// Get Cookies string
        /// </summary>
        /// <param name="browserType">WebBrowserUtil.BrowserType</param>
        /// <param name="profileFolderName">String, The profile folder name or path</param>
        /// <returns>String</returns>
        public static string GetCookies(
            WebBrowserUtil.BrowserType browserType,
            string profileFolderName = "")
        {
            List<WebBrowserUtil.CookieData> cookies = WebBrowserUtil.GetCookies(
                browserType,
                profileFolderName,
                ".youtube.com");

            if (cookies.Any())
            {
                return string.Join(";", cookies.Select(n => $"{n.Name}={n.Value}"));
            }
            else
            {
                return string.Empty;
            }
        }
    }
}