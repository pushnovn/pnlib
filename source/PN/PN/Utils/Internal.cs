using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Some usefull utils methods.
/// </summary>
namespace PN.Utils
{
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
        /// Create new List with items of type = <paramref name="listItemType"/>. May returns null if <paramref name="listItemType"/> has no paramless constructor.
        /// </summary>
        internal static IList CreateList(Type listItemType)
        {
            try
            {
                Type genericListType = typeof(List<>).MakeGenericType(listItemType);
                return (IList)Activator.CreateInstance(genericListType);
            }
            catch
            {
                return null;
            }
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
                    .SetValue(instance, TryParse ? Newtonsoft.Json.Linq.JObject.Parse((string)value) : value);
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
                return strToProcess;

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

        /// <summary>
        /// Get dll from <paramref name="resourceName"/> and copy it to disk by filename path.
        /// </summary>
        internal static void WriteResourceToFile(string resourceName, string fileName)
        {
            using (var resource = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                using (var file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    resource?.CopyTo(file);
                }
            }
        }

        /// <summary>
        /// Returns all avaliable in dll resource's names.
        /// </summary>
        internal static List<string> ManifestResourceNames 
            => System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames().ToList();


        internal static PlatformID CurrentPlatform => Environment.OSVersion.Platform;
        internal static bool CurrentPlatformIsWindows => CurrentPlatform == PlatformID.Win32Windows || CurrentPlatform == PlatformID.Win32NT;
        internal static bool CurrentPlatformIsPC => CurrentPlatformIsWindows || CurrentPlatform == PlatformID.Unix || CurrentPlatform == PlatformID.MacOSX;

        internal static string GetOSInfo()
        {
            //Get Operating system information.
            OperatingSystem os = Environment.OSVersion;
            //Get version information about the os.
            Version vs = os.Version;

            //Variable to hold our return value
            string operatingSystem = "";

            if (os.Platform == PlatformID.Win32Windows)
            {
                //This is a pre-NT version of Windows
                switch (vs.Minor)
                {
                    case 0:
                        operatingSystem = "95";
                        break;
                    case 10:
                        if (vs.Revision.ToString() == "2222A")
                            operatingSystem = "98SE";
                        else
                            operatingSystem = "98";
                        break;
                    case 90:
                        operatingSystem = "Me";
                        break;
                    default:
                        break;
                }
            }
            else if (os.Platform == PlatformID.Win32NT)
            {
                switch (vs.Major)
                {
                    case 3:
                        operatingSystem = "NT 3.51";
                        break;
                    case 4:
                        operatingSystem = "NT 4.0";
                        break;
                    case 5:
                        if (vs.Minor == 0)
                            operatingSystem = "2000";
                        else
                            operatingSystem = "XP";
                        break;
                    case 6:
                        if (vs.Minor == 0)
                            operatingSystem = "Vista";
                        else if (vs.Minor == 1)
                            operatingSystem = "7";
                        else if (vs.Minor == 2)
                            operatingSystem = "8";
                        else
                            operatingSystem = "8.1";
                        break;
                    case 10:
                        operatingSystem = "10";
                        break;
                    default:
                        break;
                }
            }
            else return os.Platform.ToString();
                
            //Make sure we actually got something in our OS check
            //We don't want to just return " Service Pack 2" or " 32-bit"
            //That information is useless without the OS version.
            if (operatingSystem != "")
            {
                //Got something.  Let's prepend "Windows" and get more info.
                operatingSystem = "Windows " + operatingSystem;
                //See if there's a service pack installed.
                if (os.ServicePack != "")
                {
                    //Append it to the OS name.  i.e. "Windows XP Service Pack 3"
                    operatingSystem += " " + os.ServicePack;
                }
                //Append the OS architecture.  i.e. "Windows XP Service Pack 3 32-bit"
                //operatingSystem += " " + getOSArchitecture().ToString() + "-bit";
            }

            //Return the information we've gathered.
            return operatingSystem;
        }
    }
}