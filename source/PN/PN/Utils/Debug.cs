using System;
using System.Collections.Generic;
using System.Text;

namespace PN.Utils
{
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

}
