using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// 문자열 확장 클래스
/// </summary>

namespace Common.Extension
{
    /// <summary>
    /// 
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsLowerEquals(this string value, string compare)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(compare))
            {
                return false;
            }
            return value.ToLower().Equals(compare.ToLower());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool EqualsIgnoreCase(this string value, string compare)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(compare))
            {
                return false;
            }
            return value.Equals(compare, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static bool ContainsIgnoreCase(this string value, string param)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(param))
            {
                return false;
            }

            return value.Contains(param, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte ToByte(this string value)
        {
            byte number = 0;
            if (string.IsNullOrEmpty(value))
                return number;

            if (false == byte.TryParse(value, out number))
            {
                DLogger.Error("StringExtension.ToByte() : {0} Parsing Error!", value);
            }
            return number;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int ToInt(this string value)
        {
            int number = -1;
            if (string.IsNullOrEmpty(value))
                return number;

            if (false == int.TryParse(value, out number))
            {
                DLogger.Error("StringExtension.ToInt() : {0} Parsing Error!", value);
            }
            return number;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static uint ToUint(this string value)
        {
            uint number = 0;
            if (string.IsNullOrEmpty(value))
                return number;

            if (false == uint.TryParse(value, out number))
            {
                DLogger.Error("StringExtension.ToInt() : {0} Parsing Error!", value);
            }
            return number;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
		public static float ToFloat(this string value)
        {
            float number = 0f;
            if (string.IsNullOrEmpty(value))
                return number;

            if (false == float.TryParse(value, out number))
            {
                DLogger.Error("StringExtension.ToFloat() : {0} Parsing Error!", value);
            }
            return number;
        }

        public static string ToFormatTime(double seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(Math.Max(0, seconds));

            if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";

            return $"{time.Minutes:D2}:{time.Seconds:D2}";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static string TruncateFileName(this string fileName, int maxLength)
        {
            if (string.IsNullOrEmpty(fileName)) return string.Empty;
            
            string nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);
            string extension = System.IO.Path.GetExtension(fileName);

            if (nameWithoutExtension.Length > maxLength)
            {
                nameWithoutExtension = nameWithoutExtension.Substring(0, maxLength) + "...";
            }

            return nameWithoutExtension + extension;
        }       
    }
}