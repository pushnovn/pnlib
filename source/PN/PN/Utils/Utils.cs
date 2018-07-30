using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PN.Utils
{
    /// <summary>
    /// Some usefull utils methods.
    /// </summary>
    public class Utils
    {
        /// <summary>
        /// Convert <paramref name="input"/> to byte array.
        /// </summary>
        public static byte[] StreamToByteArray(Stream input)
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


        /// <summary>
        /// Logging <paramref name="ex"/> via Debug.WriteLine.
        /// </summary>
        public static void Log(Exception ex)
        {
            Log(ex?.ToString());
        }

        /// <summary>
        /// Logging <paramref name="obj"/> via Debug.WriteLine.
        /// </summary>
        public static void Log(object obj)
        {
            Log(obj == null ? "null" : obj.ToString());
        }

        /// <summary>
        /// Logging <paramref name="str"/> via Debug.WriteLine.
        /// </summary>
        public static void Log(string str)
        {
            System.Diagnostics.Debug.WriteLine($"{DateTime.Now}: {str}");
        }


        /// <summary>
        /// Try to execute <paramref name="act"/>. Logging error if some exception during the execution.
        /// </summary>
        public static Exception TryExecute(Action act)
        {
            try
            {
                act?.Invoke();
                return null;
            }
            catch (Exception ex)
            {
                Log(ex);
                return ex;
            }
        }
        

        /// <summary>
        /// Convert double value to short string with K, M or B version, where K = 1 000, M = 1 000 000 and B = 1 000 000 000.
        /// </summary>
        public static string NumberFormat(double num)
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
    }
}
