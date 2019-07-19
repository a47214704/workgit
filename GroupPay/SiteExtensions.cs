using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;

namespace GroupPay
{
    public static class SiteExtensions
    {
        public static AuthenticationBuilder AddUserToken(this AuthenticationBuilder builder, string scheme, Action<UserTokenAuthenticationOptions> configureOptions)
        {
            builder.AddScheme<UserTokenAuthenticationOptions, UserTokenAuthenticationHandler>(scheme, configureOptions);
            return builder;
        }

        public static DateTimeOffset RoundDay(this DateTimeOffset dateTime, TimeZoneInfo timeZone)
        {
            DateTimeOffset dt = TimeZoneInfo.ConvertTime(dateTime, timeZone);
            return new DateTimeOffset(dt.Date, dt.Offset);
        }

        public static DateTimeOffset RoundWeek(this DateTimeOffset dateTime, TimeZoneInfo timeZone)
        {
            DateTimeOffset dt = TimeZoneInfo.ConvertTime(dateTime, timeZone);
            int dayOfWeek = (int)dt.DayOfWeek;
            if (dayOfWeek == 0)
            {
                dayOfWeek = 7;
            }

            return new DateTimeOffset(dt.Date.AddDays(-1 * (dayOfWeek - 1)), dt.Offset);
        }

        public static DateTimeOffset RoundMonth(this DateTimeOffset dateTime, TimeZoneInfo timeZone)
        {
            DateTimeOffset dt = TimeZoneInfo.ConvertTime(dateTime, timeZone);
            return new DateTimeOffset(dt.Date.AddDays(-1 * (dt.Day - 1)), dt.Offset);
        }

        public static int ToDateValue(this DateTimeOffset dateTime, TimeZoneInfo timeZone)
        {
            DateTimeOffset targetDateTime = TimeZoneInfo.ConvertTime(dateTime, timeZone);
            return targetDateTime.Year * 10000 + targetDateTime.Month * 100 + targetDateTime.Day;
        }

        public static string ToTimeZoneDateTime(this string dateTime, int timeZoneOffset)
        {
            return $"{dateTime}+0{timeZoneOffset}00";
        }

        public static long ToTimeStamp(this string dateTimeString, int timeZoneOffset)
        {
            long timestamp = 0;
            if (!string.IsNullOrEmpty(dateTimeString) && DateTime.TryParse(dateTimeString.ToTimeZoneDateTime(timeZoneOffset), out DateTime dateTime))
            {
                timestamp = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
            }
            return timestamp;
        }

        public static bool[] ToBitBool(this int bits,int types)
        {
            List<bool> BitBool = new List<bool>();
            foreach (char i in Convert.ToString(bits, 2).PadLeft(types))
            {
                BitBool.Add(i == '1');
            }
            return BitBool.ToArray();
        }

        public static int ToBitInt(this bool[] bits)
        {
            int bi = 0;
            if (bits != null)
            { 
                for (int b = 0; b < bits.Length; b++)
                {
                    bi += (int)Math.Pow(2, bits.Length - b - 1) * (bits[b] ? 1 : 0);
                }
            }
            return bi;
        }

        public static bool IsPhoneFormat(this string value)
        {
            Regex rgx = new Regex(@"^13[\d]{9}$|^14[5,7]{1}\d{8}$|^15[^4]{1}\d{8}$|^17[0,6,7,8]{1}\d{8}$|^18[\d]{9}$|^19[\d]{9}$");
            return rgx.IsMatch(value);
        }

        public static byte[] UnsafeEncrypt(this string plainText, string key)
        {
            // Check arguments.
            if (string.IsNullOrEmpty(plainText))
            {
                throw new ArgumentNullException(nameof(plainText));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            byte[] keyInBytes = Encoding.UTF8.GetBytes(key);
            byte[] encrypted;

            if (keyInBytes.Length != 16)
            {
                throw new ArgumentException("invalid key length", nameof(key));
            }

            // Create an Rijndael object
            // with the specified key and IV.
            using (Rijndael rijAlg = Rijndael.Create())
            {
                rijAlg.KeySize = 128;
                rijAlg.Key = keyInBytes;
                rijAlg.IV = keyInBytes;
                rijAlg.Mode = CipherMode.ECB;
                rijAlg.Padding = PaddingMode.PKCS7;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor();

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }

                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Return the encrypted bytes from the memory stream.
            return encrypted;
        }

        public static Image Resize(this Image current, int maxWidth, int maxHeight)
        {
            int width, height;
            if (current.Width > current.Height)
            {
                width = maxWidth;
                height = Convert.ToInt32(current.Height * maxHeight / (double)current.Width);
            }
            else
            {
                width = Convert.ToInt32(current.Width * maxWidth / (double)current.Height);
                height = maxHeight;
            }

            var canvas = new Bitmap(width, height);

            using (var graphics = Graphics.FromImage(canvas))
            {
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.DrawImage(current, 0, 0, width, height);
            }

            return canvas;
        }
    }
}
