using System;
using System.Text;

namespace TestEasy.Azure.Helpers
{
    /// <summary>
    ///     Encoding helper API
    /// </summary>
    public static class Base64EncodingHelper
    {
        /// <summary>
        ///     Encode to base64 string
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public static string EncodeToBase64String(string original)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(original));
        }

        /// <summary>
        ///     Decode from base64 string
        /// </summary>
        /// <param name="encoded"></param>
        /// <returns></returns>
        public static string DecodeFromBase64String(string encoded)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        }

        /// <summary>
        ///     Encode string to base64
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string EncodeToBase64(this string s)
        {
            return EncodeToBase64(s);
        }

        /// <summary>
        ///     Decode from base64 string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string DecodeFromBase64(this string s)
        {
            return DecodeFromBase64String(s);
        }
    }
}
