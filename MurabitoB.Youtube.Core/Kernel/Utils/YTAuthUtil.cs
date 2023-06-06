using KomeTube.Kernel.Models;
using KomeTube.Kernel.YtLiveChatDataModel;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace KomeTube.Kernel.Utils
{
    /// <summary>
    /// YTAuthUtil
    /// <para>Source: https://github.com/rubujo/YTLiveChatCatcher/blob/main/YTApi/LiveChatFunction.cs</para>
    /// </summary>
    public class YTAuthUtil
    {
        /// <summary>
        /// Origin
        /// </summary>
        public static readonly string Origin = "https://www.youtube.com";

        /// <summary>
        /// Set HttpRequestMessage header
        /// <para>Original Author: Greg Beech</para>
        /// <para>Original Source: https://stackoverflow.com/a/13287224</para>
        /// <para>Original License: CC BY-SA 3.0</para>
        /// <para>CC BY-SA 3.0: https://creativecommons.org/licenses/by-sa/3.0/</para>
        /// </summary>
        /// <param name="httpRequestMessage">HttpRequestMessage</param>
        /// <param name="cookies">String, The cookies string</param>
        /// <param name="innerContextData">InnerTubeContextData, The default value is null</param>
        /// <param name="showLogInConsole">Boolean, Determine to show IHttpClientFactory logs in the console, The default value is true</param>
        public static void SetHttpRequestMessageHeader(
            HttpRequestMessage httpRequestMessage,
            string cookies,
            InnerTubeContextData innerContextData = null,
            bool showLogInConsole = true)
        {
            if (!string.IsNullOrEmpty(cookies))
            {
                CookieConfigData cookieConfigData = HttpClientUtil.GetCookieConfigData(showLogInConsole);

                // Cookies are set manually when the CookieContainer is not in use.
                if (!cookieConfigData.UseCookies)
                {
                    httpRequestMessage.Headers.Add("Cookie", cookies);
                }

                string[] cookieSet = cookies.Split(
                    new char[] { ';' },
                    StringSplitOptions.RemoveEmptyEntries);

                string sapiSid = cookieSet.FirstOrDefault(n => n.Contains("SAPISID"));

                if (!string.IsNullOrEmpty(sapiSid))
                {
                    string[] tempArray = sapiSid.Split(
                        new char[] { '=' },
                        StringSplitOptions.RemoveEmptyEntries);

                    if (tempArray.Length == 2)
                    {
                        httpRequestMessage.Headers.Authorization =
                            new AuthenticationHeaderValue(
                                "SAPISIDHASH",
                                GetSapiSidHash(tempArray[1], Origin));
                    }
                }
            }

            if (innerContextData != null)
            {
                string xGoogAuthuser = "0",
                    xGoogPageId = string.Empty;

                if (!string.IsNullOrEmpty(innerContextData.DataSyncID))
                {
                    xGoogPageId = innerContextData.DataSyncID;
                }

                if (string.IsNullOrEmpty(xGoogPageId) &&
                    !string.IsNullOrEmpty(innerContextData.DelegatedSessionID))
                {
                    xGoogPageId = innerContextData.DelegatedSessionID;
                }

                if (!string.IsNullOrEmpty(xGoogPageId))
                {
                    httpRequestMessage.Headers.Add("X-Goog-Pageid", xGoogPageId);
                }

                if (!string.IsNullOrEmpty(innerContextData.IDToken))
                {
                    httpRequestMessage.Headers.Add("X-Youtube-Identity-Token", innerContextData.IDToken);
                }

                if (!string.IsNullOrEmpty(innerContextData.SessionIndex))
                {
                    xGoogAuthuser = innerContextData.SessionIndex;
                }

                httpRequestMessage.Headers.Add("X-Goog-Authuser", xGoogAuthuser);
                httpRequestMessage.Headers.Add("X-Goog-Visitor-Id", innerContextData.Context.Client.VisitorData);
                httpRequestMessage.Headers.Add("X-Youtube-Client-Name", innerContextData.InnertubeContextClientName.ToString());
                httpRequestMessage.Headers.Add("X-Youtube-Client-Version", innerContextData.InnertubeClientVersion);
                httpRequestMessage.Headers.Referrer = httpRequestMessage.RequestUri;
            }

            httpRequestMessage.Headers.Add("Origin", Origin);
            httpRequestMessage.Headers.Add("X-Origin", Origin);
        }

        /// <summary>
        /// Get SAPISIDHASH string
        /// </summary>
        /// <param name="sapiSid">String, SAPISID</param>
        /// <param name="origin">String, origin</param>
        /// <returns>String</returns>
        public static string GetSapiSidHash(string sapiSid, string origin)
        {
            long unixTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();

            return $"{unixTimestamp}_{GetSHA1Hash($"{unixTimestamp} {sapiSid} {origin}")}";
        }

        /// <summary>
        /// Get SHA-1 hash string
        /// </summary>
        /// <param name="value">String, value</param>
        /// <returns>String</returns>
        public static string GetSHA1Hash(string value)
        {
            using (SHA1Managed sHA1Managed = new SHA1Managed())
            {
                byte[] bytes = sHA1Managed.ComputeHash(Encoding.UTF8.GetBytes(value));

                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}