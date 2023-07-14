using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

namespace KomeTube.Kernel.Utils
{
    /// <summary>
    /// WebBrowserUtil
    /// <para>Source: https://github.com/rubujo/YTLiveChatCatcher/blob/main/Common/BrowserManager.cs</para>
    /// <para>Original Author: Flint Charles</para>
    /// <para>Original Source: https://stackoverflow.com/a/68703365</para>
    /// <para>Original License: CC BY-SA 4.0</para>
    /// <para>CC BY-SA 4.0: https://creativecommons.org/licenses/by-sa/4.0/</para>
    /// </summary>
    public class WebBrowserUtil
    {
        /// <summary>
        /// Enum: BrowserType
        /// </summary>
        public enum BrowserType
        {
            /// <summary>
            /// Brave
            /// </summary>
            Brave = 1,
            /// <summary>
            /// Brave Beta
            /// </summary>
            BraveBeta = 2,
            /// <summary>
            /// Brave Nightly
            /// </summary>
            BraveNightly = 3,
            /// <summary>
            /// Google Chrome
            /// </summary>
            GoogleChrome = 4,
            /// <summary>
            /// Google Chrome Beta
            /// </summary>
            GoogleChromeBeta = 5,
            /// <summary>
            /// Google Chrome Canary
            /// </summary>
            GoogleChromeCanary = 6,
            /// <summary>
            /// Chromium
            /// </summary>
            Chromium = 7,
            /// <summary>
            /// Microsoft Edge
            /// </summary>
            MicrosoftEdge = 8,
            /// <summary>
            /// Microsoft Edge Insider Beta
            /// </summary>
            MicrosoftEdgeInsiderBeta = 9,
            /// <summary>
            /// Microsoft Edge Insider Dev
            /// </summary>
            MicrosoftEdgeInsiderDev = 10,
            /// <summary>
            /// Microsoft Edge Insider Canary
            /// </summary>
            MicrosoftEdgeInsiderCanary = 11,
            /// <summary>
            /// Opera
            /// </summary>
            Opera = 12,
            /// <summary>
            /// Opera Beta
            /// </summary>
            OperaBeta = 13,
            /// <summary>
            /// Opera Developer
            /// </summary>
            OperaDeveloper = 14,
            /// <summary>
            /// Opera GX
            /// </summary>
            OperaGX = 15,
            /// <summary>
            /// Opera Crypto
            /// </summary>
            OperaCrypto = 16,
            /// <summary>
            /// Vivaldi
            /// <para>*All editions are using the same parent folder</para>
            /// </summary>
            Vivaldi = 17,
            /// <summary>
            /// Mozilla Firefox
            /// <para>*All editions are using the same parent folder</para>
            /// </summary>
            MozillaFirefox = 18
        };

        /// <summary>
        /// Get Cookies
        /// </summary>
        /// <param name="browserType">BrowserType</param>
        /// <param name="profileName">String, The profile name</param>
        /// <param name="hostKey">String, The host key</param>
        /// <returns>List&lt;CookieData&gt;</returns>
        public static List<CookieData> GetCookies(
            BrowserType browserType,
            string profileName,
            string hostKey)
        {
            bool isCustomProfilePath = false;

            // Determines if the path is a custom profile.
            if (Path.IsPathRooted(profileName))
            {
                isCustomProfilePath = true;
            }

            List<CookieData> outputData = new List<CookieData>();

            string cookieFilePath = string.Empty;

            if (browserType == BrowserType.MozillaFirefox)
            {
                cookieFilePath = isCustomProfilePath ?
                    profileName :
                    Path.Combine(
                        $@"C:\Users\{Environment.UserName}\AppData\Roaming\",
                        GetPartialPath(browserType));

                DirectoryInfo directoryInfo = new DirectoryInfo(cookieFilePath);

                if (directoryInfo.Exists)
                {
                    string portableProfilePath = Path.Combine(cookieFilePath, "cookies.sqlite");

                    if (File.Exists(portableProfilePath))
                    {
                        cookieFilePath = portableProfilePath;
                    }
                    else
                    {
                        DirectoryInfo[] diDirectories = directoryInfo.GetDirectories();
                        DirectoryInfo diTargetDirectory = diDirectories.FirstOrDefault(n => n.Name == profileName) ??
                            diDirectories.FirstOrDefault();

                        // Theoretically, diTargetDirectory should not be null.
                        cookieFilePath = Path.Combine(
                            cookieFilePath,
                            $@"{diTargetDirectory?.Name}\cookies.sqlite");
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(profileName))
                {
                    profileName = "Default";
                }

                cookieFilePath = isCustomProfilePath ?
                    Path.Combine(profileName, @"Network\Cookies") :
                    Path.Combine(
                        $@"C:\Users\{Environment.UserName}\AppData\Local\{GetPartialPath(browserType)}\User Data",
                        $@"{profileName}\Network\Cookies");
            }

            Debug.WriteLine($"Cookie file path: {cookieFilePath}");

            if (File.Exists(cookieFilePath))
            {
                outputData = QuerySQLiteDB(browserType, cookieFilePath, hostKey);
            }

            return outputData;
        }

        /// <summary>
        /// Get partial path
        /// </summary>
        /// <param name="browserType">BrowserType</param>
        /// <returns>String</returns>
        public static string GetPartialPath(BrowserType browserType)
        {
            string partialPath;

            switch (browserType)
            {
                case BrowserType.Brave:
                    partialPath = @"BraveSoftware\Brave-Browser";

                    break;
                case BrowserType.BraveBeta:
                    partialPath = @"BraveSoftware\Brave-Browser-Beta";

                    break;
                case BrowserType.BraveNightly:
                    partialPath = @"BraveSoftware\Brave-Browser-Nightly";

                    break;
                case BrowserType.GoogleChrome:
                    partialPath = @"Google\Chrome";

                    break;
                case BrowserType.GoogleChromeBeta:
                    partialPath = @"Google\Chrome Beta";

                    break;
                case BrowserType.GoogleChromeCanary:
                    partialPath = @"Google\Chrome SxS";

                    break;
                case BrowserType.Chromium:
                    partialPath = "Chromium";

                    break;
                case BrowserType.MicrosoftEdge:
                    partialPath = @"Microsoft\Edge";

                    break;
                case BrowserType.MicrosoftEdgeInsiderBeta:
                    partialPath = @"Microsoft\Edge Beta";

                    break;
                case BrowserType.MicrosoftEdgeInsiderDev:
                    partialPath = @"Microsoft\Edge Dev";

                    break;
                case BrowserType.MicrosoftEdgeInsiderCanary:
                    partialPath = @"Microsoft\Edge SxS";

                    break;
                case BrowserType.Opera:
                    partialPath = @"Opera Software\Opera Stable";

                    break;
                case BrowserType.OperaBeta:
                    partialPath = @"Opera Software\Opera Next";

                    break;
                case BrowserType.OperaDeveloper:
                    partialPath = @"Opera Software\Opera Developer";

                    break;
                case BrowserType.OperaGX:
                    partialPath = @"Opera Software\Opera GX Stable";

                    break;
                case BrowserType.OperaCrypto:
                    partialPath = @"Opera Software\Opera Crypto Stable";

                    break;
                case BrowserType.Vivaldi:
                    partialPath = @"Vivaldi";

                    break;
                case BrowserType.MozillaFirefox:
                    partialPath = @"Mozilla\Firefox\Profiles";

                    break;
                default:
                    partialPath = @"Google\Chrome";

                    break;
            }

            return partialPath;
        }

        /// <summary>
        /// Query SQLite database
        /// </summary>
        /// <param name="browserType">BrowserType</param>
        /// <param name="cookieFilePath">String, The cookie file path</param>
        /// <param name="hostKey">String, host key</param>
        /// <returns>List&lt;CookieData&gt;</returns>
        private static List<CookieData> QuerySQLiteDB(
            BrowserType browserType,
            string cookieFilePath,
            string hostKey)
        {
            List<CookieData> outputData = new List<CookieData>();

            try
            {
                using (SqliteConnection sqliteConnection = new SqliteConnection($"Data Source={cookieFilePath}"))
                using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
                {
                    string rawTSQL;

                    switch (browserType)
                    {
                        case BrowserType.MozillaFirefox:
                            rawTSQL = "SELECT [name], [value], [host] FROM [moz_cookies]";

                            break;
                        default:
                            rawTSQL = "SELECT [name], [encrypted_value], [host_key] FROM [cookies]";

                            break;
                    }

                    string rawWhereClauseTSQL;

                    switch (browserType)
                    {
                        case BrowserType.MozillaFirefox:
                            //rawWhereClauseTSQL = $" WHERE [host] = LIKE '%{hostKey}%'";
                            rawWhereClauseTSQL = $" WHERE [host] = '{hostKey}'";

                            break;
                        default:
                            //rawWhereClauseTSQL = $" WHERE [host_key] LIKE '%{hostKey}%'";
                            rawWhereClauseTSQL = $" WHERE [host_key] = '{hostKey}'";

                            break;
                    }

                    if (!string.IsNullOrEmpty(hostKey))
                    {
                        rawTSQL += rawWhereClauseTSQL;
                    }

                    sqliteCommand.CommandText = rawTSQL;

                    sqliteConnection.Open();

                    using (SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader())
                    {
                        byte[] key;

                        switch (browserType)
                        {
                            case BrowserType.MozillaFirefox:
                                key = Array.Empty<byte>();

                                break;
                            default:
                                key = AesGcm256.GetKey(browserType);

                                break;
                        };

                        while (sqliteDataReader.Read())
                        {
                            if (!outputData.Any(a => a.Name == sqliteDataReader.GetString(0)))
                            {
                                string value = string.Empty;

                                switch (browserType)
                                {
                                    case BrowserType.MozillaFirefox:
                                        value = sqliteDataReader.GetString(1);

                                        break;
                                    default:
                                        byte[] encryptedData = GetBytes(sqliteDataReader, 1);
                                        byte[] nonce, ciphertextTag;

                                        AesGcm256.Prepare(encryptedData, out nonce, out ciphertextTag);

                                        value = AesGcm256.Decrypt(ciphertextTag, key, nonce);

                                        break;
                                }

                                outputData.Add(new CookieData()
                                {
                                    HostKey = sqliteDataReader.GetString(2),
                                    Name = sqliteDataReader.GetString(0),
                                    Value = value
                                });
                            }
                        }
                    }

                    sqliteConnection.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return outputData;
        }

        /// <summary>
        /// Get byte[]
        /// </summary>
        /// <param name="sqliteDataReader">SqliteDataReader</param>
        /// <param name="columnIndex">Int32</param>
        /// <returns>byte[]</returns>
        private static byte[] GetBytes(SqliteDataReader sqliteDataReader, int columnIndex)
        {
            const int CHUNK_SIZE = 2 * 1024;

            byte[] buffer = new byte[CHUNK_SIZE];

            long bytesRead;
            long fieldOffset = 0;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                while ((bytesRead = sqliteDataReader.GetBytes(columnIndex, fieldOffset, buffer, 0, buffer.Length)) > 0)
                {
                    memoryStream.Write(buffer, 0, (int)bytesRead);

                    fieldOffset += bytesRead;
                }

                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// CookieData
        /// </summary>
        public class CookieData
        {
            /// <summary>
            /// Name
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// HostKey
            /// </summary>
            public string HostKey { get; set; }

            /// <summary>
            /// Value
            /// </summary>
            public string Value { get; set; }
        }

        /// <summary>
        /// AesGcm256
        /// </summary>
        public class AesGcm256
        {
            /// <summary>
            /// Get key
            /// </summary>
            /// <param name="browserType">BrowserType</param>
            /// <returns>String</returns>
            public static byte[] GetKey(BrowserType browserType)
            {
                //string sR = string.Empty;
                //string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                string path = $@"C:\Users\{Environment.UserName}\AppData\Local\{GetPartialPath(browserType)}\User Data\Local State";
                string v = File.ReadAllText(path);

                dynamic jsonData = JsonConvert.DeserializeObject<dynamic>(v);

                string key = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "os_crypt.encrypted_key", string.Empty));

                byte[] src = Convert.FromBase64String(key);
                byte[] encryptedKey = src.Skip(5).ToArray();
                byte[] decryptedKey = ProtectedData.Unprotect(encryptedKey, null, DataProtectionScope.CurrentUser);

                return decryptedKey;
            }

            /// <summary>
            /// Decrypt
            /// </summary>
            /// <param name="encryptedBytes">byte[]</param>
            /// <param name="key">byte[]</param>
            /// <param name="iv">byte[]</param>
            /// <returns>String</returns>
            public static string Decrypt(byte[] encryptedBytes, byte[] key, byte[] iv)
            {
                string sR = string.Empty;

                try
                {
                    GcmBlockCipher cipher = new GcmBlockCipher(new AesEngine());
                    AeadParameters parameters = new AeadParameters(new KeyParameter(key), 128, iv, null);

                    cipher.Init(false, parameters);

                    byte[] plainBytes = new byte[cipher.GetOutputSize(encryptedBytes.Length)];

                    int retLen = cipher.ProcessBytes(encryptedBytes, 0, encryptedBytes.Length, plainBytes, 0);

                    cipher.DoFinal(plainBytes, retLen);

                    sR = Encoding.UTF8.GetString(plainBytes).TrimEnd("\r\n\0".ToCharArray());
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }

                return sR;
            }

            /// <summary>
            /// Prepare
            /// </summary>
            /// <param name="encryptedData">byte[]</param>
            /// <param name="nonce">byte[]</param>
            /// <param name="ciphertextTag">byte[]</param>
            public static void Prepare(byte[] encryptedData, out byte[] nonce, out byte[] ciphertextTag)
            {
                nonce = new byte[12];
                ciphertextTag = new byte[encryptedData.Length - 3 - nonce.Length];

                Array.Copy(encryptedData, 3, nonce, 0, nonce.Length);
                Array.Copy(encryptedData, 3 + nonce.Length, ciphertextTag, 0, ciphertextTag.Length);
            }
        }
    }
}