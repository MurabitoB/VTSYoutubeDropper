using System.Collections.Generic;
using System.Net.Http;
using System.Net;

namespace KomeTube.Kernel.Utils
{
    /// <summary>
    /// ClientHintsUtil
    /// <para>Source: https://github.com/rubujo/YTLiveChatCatcher/blob/main/Common/Utils/ClientHintsUtil.cs</para>
    /// </summary>
    public class ClientHintsUtil
    {
        /// <summary>
        /// Client Hints
        /// <para>Reference 1: https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers#client_hints</para>
        /// <para>Reference 2: https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers#fetch_metadata_request_headers</para>
        /// </summary>
        private static readonly Dictionary<string, string> KeyValues = new Dictionary<string, string>()
        {
            // TODO: It needs to be adjusted with UserAgent string.
            { "Sec-CH-Prefers-Reduced-Motion", string.Empty },
            { "Sec-CH-UA", string.Empty },
            { "Sec-CH-UA-Arch", string.Empty },
            { "Sec-CH-UA-Bitness",string.Empty },
            // Deprecated.
            //{ "Sec-CH-UA-Full-Version", string.Empty },
            { "Sec-CH-UA-Full-Version-List", string.Empty },
            { "Sec-CH-UA-Mobile",string.Empty },
            { "Sec-CH-UA-Model", string.Empty },
            { "Sec-CH-UA-Platform", string.Empty },
            { "Sec-CH-UA-Platform-Version", string.Empty },
            { "Sec-Fetch-Site", string.Empty },
            { "Sec-Fetch-Mode", string.Empty },
            // TODO: Sec-Fetch-User is not currently used.
            //{ "Sec-Fetch-User", string.Empty },
            { "Sec-Fetch-Dest", string.Empty }
        };

        /// <summary>
        /// Set Client Hints headers
        /// </summary>
        /// <param name="webHeaderCollection">HttpClient</param>
        public static void SetClientHints(WebHeaderCollection webHeaderCollection)
        {
            if (webHeaderCollection == null)
            {
                return;
            }

            foreach (KeyValuePair<string, string> item in KeyValues)
            {
                if (!string.IsNullOrEmpty(item.Value))
                {
                    webHeaderCollection.Add(item.Key, item.Value);
                }
            }
        }

        /// <summary>
        /// Set Client Hints headers
        /// </summary>
        /// <param name="httpClient">HttpClient</param>
        public static void SetClientHints(HttpClient httpClient)
        {
            if (httpClient == null)
            {
                return;
            }

            foreach (KeyValuePair<string, string> item in KeyValues)
            {
                if (!string.IsNullOrEmpty(item.Value))
                {
                    httpClient.DefaultRequestHeaders.Add(item.Key, item.Value);
                }
            }
        }
    }
}