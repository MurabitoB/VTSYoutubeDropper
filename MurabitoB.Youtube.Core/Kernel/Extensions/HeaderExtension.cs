using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;

namespace KomeTube.Kernel.Extensions
{
    /// <summary>
    /// Extension methods for logging header values
    /// </summary>
    public static class HeaderExtension
    {
        /// <summary>
        /// Logging header values
        /// </summary>
        /// <param name="httpClient">HttpClient</param>
        /// <param name="useConsole">Boolean, Show log in console, The default is false</param>
        public static void LogHeaders(this HttpClient httpClient, bool useConsole = false)
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (KeyValuePair<string, IEnumerable<string>> header in httpClient.DefaultRequestHeaders)
            {
                stringBuilder.AppendLine($"{header.Key}: {string.Join(";", header.Value)}");
            }

            string message = stringBuilder.ToString();

            if (string.IsNullOrEmpty(message))
            {
                message = "No header values.";
            }

            if (useConsole)
            {
                Console.WriteLine(message);
            }
            else
            {
                Debug.WriteLine(message);
            }
        }

        /// <summary>
        /// Logging header values
        /// </summary>
        /// <param name="httpRequestMessage">HttpRequestMessage</param>
        /// <param name="useConsole">Boolean, Show log in console, The default is false</param>
        public static void LogHeaders(this HttpRequestMessage httpRequestMessage, bool useConsole = false)
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (KeyValuePair<string, IEnumerable<string>> header in httpRequestMessage.Headers)
            {
                stringBuilder.AppendLine($"{header.Key}: {string.Join(";", header.Value)}");
            }

            string message = stringBuilder.ToString();

            if (string.IsNullOrEmpty(message))
            {
                message = "No header values.";
            }

            if (useConsole)
            {
                Console.WriteLine(message);
            }
            else
            {
                Debug.WriteLine(message);
            }
        }

        /// <summary>
        /// Logging header values
        /// </summary>
        /// <param name="httpResponseMessage">HttpResponseMessage</param>
        /// <param name="useConsole">Boolean, Show log in console, The default is false</param>
        public static void LogHeaders(this HttpResponseMessage httpResponseMessage, bool useConsole = false)
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (KeyValuePair<string, IEnumerable<string>> header in httpResponseMessage.Headers)
            {
                stringBuilder.AppendLine($"{header.Key}: {string.Join(";", header.Value)}");
            }

            string message = stringBuilder.ToString();

            if (string.IsNullOrEmpty(message))
            {
                message = "No header values.";
            }

            if (useConsole)
            {
                Console.WriteLine(message);
            }
            else
            {
                Debug.WriteLine(message);
            }
        }
    }
}
