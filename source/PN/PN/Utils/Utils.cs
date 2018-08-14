using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace PN.Utils
{
    /// <summary>
    /// Some usefull utils methods.
    /// </summary>
    public class Utils
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

            public class Base64
            {
                public static string ToString(byte[] inArray) => Convert.ToBase64String(inArray);
                public static byte[] ToBytes(string s) => Convert.FromBase64String(s);
            }
        }
        
        public class Debug
        {
            /// <summary>
            /// Run <paramref name="act"/> and calculate and return time, which was spend on running it.
            /// <para>If <paramref name="preString"/> is not null, then logging time via Debug.WriteLine.</para>
            /// </summary>
            public static long CalculateMethodTimeExecution(Action act, string preString = null)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                act();

                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;

                if (preString != null)
                    Log($"{preString} = {elapsedMs} ms");

                return elapsedMs;
            }

            #region Logger

            /// <summary>
            /// Logging <paramref name="ex"/> via Debug.WriteLine.
            /// </summary>
            public static void Log(Exception ex, bool writeInConsole = false)
            {
                Log(ex?.ToString(), writeInConsole);
            }

            /// <summary>
            /// Logging <paramref name="obj"/> via Debug.WriteLine.
            /// </summary>
            public static void Log(object obj, bool writeInConsole = false)
            {
                Log(obj == null ? "null" : obj.ToString(), writeInConsole);
            }

            /// <summary>
            /// Logging <paramref name="str"/> via Debug.WriteLine.
            /// </summary>
            public static void Log(string str, bool writeInConsole = false)
            {
                System.Diagnostics.Debug.WriteLine($"{DateTime.Now}: {str}");
                if (writeInConsole)
                    Console.WriteLine($"{DateTime.Now}: {str}");
            }

            #endregion

            /// <summary>
            /// Try to execute <paramref name="act"/>. Logging error if some exception during the execution.
            /// </summary>
            public static Exception TryExecute(Action act, bool writeInConsole = false)
            {
                try
                {
                    act?.Invoke();
                    return null;
                }
                catch (Exception ex)
                {
                    Log(ex, writeInConsole);
                    return ex;
                }
            }
        }

        /// <summary>
        /// Represents system utils which are should used only in library.
        /// </summary>
        internal class Internal
        {
            /// <summary>
            /// Create new object of type = <paramref name="type"/>. May returns null if <paramref name="type"/> has no paramless constructor.
            /// </summary>
            internal static object CreateDefaultObject(Type type, bool allowNull = false)
            {
                try { return type.IsValueType || allowNull == false ? Activator.CreateInstance(type) : null; }
                catch { return null; }
            }

            /// <summary>
            /// Trying to set <paramref name="value"/> in <paramref name="prop_name"/> to object <paramref name="instance"/>. 
            /// <paramref name="TryParse"/> is used if you need to set dynamic <paramref name="value"/> to <paramref name="prop_name"/> parsed from JSON.
            /// </summary>
            internal static object TrySetValue(ref object instance, object value, string prop_name, bool TryParse = false)
            {
                try
                {
                    instance
                        .GetType()
                        .GetProperty(prop_name)
                        .SetValue(instance, TryParse ? InternalNewtonsoft.Json.Linq.JObject.Parse((string)value) : value);
                }
                catch { }
                return instance;
            }

            /// <summary>
            /// Process <paramref name="strToProcess"/>, search intenal {SOME_STRING} constructions 
            /// and try to replace it via value of the property of object <paramref name="model"/> with name == SOME_STRING.
            /// </summary>
            internal static string ProcessComplexString(string strToProcess, object model)
            {
                if (string.IsNullOrWhiteSpace(strToProcess))
                    return string.Empty;

                MatchCollection matches = Regex.Matches(strToProcess, @"\{[\w]+\}");

                var str = strToProcess.Substring(0, matches.Count > 0 ? matches[0].Index : strToProcess.Length);

                for (int i = 0; i < matches.Count; i++)
                {
                    Match m = matches[i];

                    str += model.GetType().GetProperty(m.Value.Substring(1, m.Length - 2))?.GetValue(model)?.ToString() ?? m.Value;

                    var indexOfLastMatchChar = m.Index + m.Length;

                    var nextClearPartLength = -indexOfLastMatchChar + (i + 1 < matches.Count ? matches[i + 1].Index : strToProcess.Length);

                    str += strToProcess.Substring(indexOfLastMatchChar, nextClearPartLength);
                }

                return str;
            }
        }
    }
}
