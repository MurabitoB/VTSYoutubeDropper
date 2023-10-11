using System;
using System.Diagnostics;

namespace KomeTube.Kernel
{
    public class JsonHelper
    {
        /// <summary>
        /// Try to get the json data value and not throw exception when there is not the key. Return default value  if there is not the key.
        /// </summary>
        /// <param name="jsonData">Raw json data.</param>
        /// <param name="key">Try string of json data key.</param>
        /// <param name="defaultValue">If there is not the key, return this value.</param>
        /// <returns>Return default value if there is not the key.</returns>
        public static object TryGetValue(dynamic jsonData, string key, object defaultValue = null)
        {
            object ret;

            try
            {
                ret = jsonData[key];

                if (ret == null)
                {
                    ret = defaultValue;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Try get value error: {0}, key: {1}", ex.Message, key));

                return defaultValue;
            }

            return ret;
        }

        /// <summary>
        /// Try to get the json data value and not throw exception when there is not the key. Return default value  if there is not the key.
        /// </summary>
        /// <param name="jsonData">Raw json data.</param>
        /// <param name="idx">Try index of json data array.</param>
        /// <param name="defaultValue">If there is not the key, return this value.</param>
        /// <returns>Return default value if there is not the key.</returns>
        public static object TryGetValue(dynamic jsonData, int idx, object defaultValue = null)
        {
            object ret;

            try
            {
                ret = jsonData[idx];

                if (ret == null)
                {
                    ret = defaultValue;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Try get valuez error: {0}, index: {1}", ex.Message, idx));

                return defaultValue;
            }

            return ret;
        }

        /// <summary>
        ///  Try to get the json data value by XPath string
        /// </summary>
        /// <param name="jsonData">dynamic</param>
        /// <param name="xPath">String, XPath string</param>
        /// <param name="defaultValue">Object, The default value is null</param>
        /// <returns>Object</returns>
        public static object TryGetValueByXPath(dynamic jsonData, string xPath, object defaultValue = null)
        {
            object ret = jsonData;

            string[] keys = xPath.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string k in keys)
            {
                int idx = -1;

                if (int.TryParse(k, out idx))
                {
                    ret = TryGetValue(ret, idx);
                }
                else
                {
                    ret = TryGetValue(ret, k);
                }

                if (ret == null)
                {
                    ret = defaultValue;

                    break;
                }
            }

            return ret;
        }
    }
}