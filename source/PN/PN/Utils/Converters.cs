using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Linq;

namespace PN.Utils
{
    public static class Converters
    {
        /// <summary>
        /// Convert <paramref name="input"/> to byte array.
        /// </summary>
        public static byte[] StreamToBytes(Stream input)
        {
            if (input.CanSeek)
                input.Position = 0;

            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        public static string BytesToString(byte[] bytes) => bytes == null ? null : Encoding.UTF8.GetString(bytes);

        public static byte[] StringToBytes(string str) => str == null ? null : Encoding.UTF8.GetBytes(str);

        /// <summary>
        /// Convert double value to short string with K, M or B version, where K = 1 000, M = 1 000 000 and B = 1 000 000 000.
        /// </summary>
        public static string NumberToShortFormattedString(double num)
        {
            if (num >= 1000 * 1000 * 1000)
                return (num / (1000 * 1000 * 1000)).ToString("0.#B");

            if (num >= 100 * 1000 * 1000)
                return (num / (1000 * 1000)).ToString("0M");

            if (num >= 1000 * 1000)
                return (num / (1000 * 1000)).ToString("#.0M");

            if (num >= 100 * 1000)
                return (num / 1000).ToString("0k");

            if (num >= 1000)
                return (num / 1000).ToString("#.0k");

            return num.ToString();
        }

        public static T FromByteArray<T>(byte[] rawValue)
        {
            GCHandle handle = GCHandle.Alloc(rawValue, GCHandleType.Pinned);
            T structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return structure;
        }

        public static byte[] ToByteArray(object value, int maxLength = int.MaxValue)
        {
            int rawsize = Marshal.SizeOf(value);
            byte[] rawdata = new byte[rawsize];
            GCHandle handle =
                GCHandle.Alloc(rawdata,
                GCHandleType.Pinned);
            Marshal.StructureToPtr(value,
                handle.AddrOfPinnedObject(),
                false);
            handle.Free();
            if (maxLength < rawdata.Length)
            {
                byte[] temp = new byte[maxLength];
                Array.Copy(rawdata, temp, maxLength);
                return temp;
            }
            else
            {
                return rawdata;
            }
        }

        #region String - Object - String

        public static T StringToObject<T>(string source) => (T) StringToObject(source, typeof(T));

        public static object StringToObject(string source, Type type)
        {
            if (string.IsNullOrEmpty(source))
                return Internal.CreateDefaultObject(type, true);

            var decrypt = Crypt.AES.Decrypt(source, keyword);
            return Newtonsoft.Json.JsonConvert.DeserializeObject(decrypt, type);
        }

        public static string ObjectToString(object value)
        {
            if (value == null)
                return null;

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(value);
            return Crypt.AES.Encrypt(json, keyword);
        }

        static string keyword = "String-Object-String";

        #endregion

        public static object[] ConvertArrayWithSingleListToArrayOfItems(object[] data)
        {
            var values = new List<object>();

            foreach (var dat in data ?? new object[0])
            {
                if (dat is IEnumerable enumerable && enumerable is string == false)
                {
                    foreach (object obj in enumerable)
                    {
                        values.Add(obj);
                    }
                }
                else
                {
                    values.Add(dat);
                }
            }
            
            return values.ToArray();
        }


        static string eng = "qwertyuiop[]asdfghjkl;'zxcvbnm,.";
        static string rus = "йцукенгшщзхъфывапролджэячсмитьбю";
        public static string SwitchKeyboard(string input)
        {
            input = (input ?? string.Empty).ToLower().Trim();

            var src_dict = Regex.IsMatch(input, @"\p{IsCyrillic}") ? rus : eng;
            var dst_dict = Regex.IsMatch(input, @"\p{IsCyrillic}") ? eng : rus;

            var output = string.Empty;

            for (int i = 0; i < input.Length; i++)
            {
                var indexOf = src_dict.IndexOf(input[i]);
                output += indexOf >= 0 ? dst_dict[indexOf] : input[i];
            }

            return output;
        }

        public class Base64
        {
            public static string ToString(byte[] inArray) => Convert.ToBase64String(inArray);
            public static byte[] ToBytes(string s) => Convert.FromBase64String(s);
        }
    }

}
