using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Linq;
using System;

using Newtonsoft.Json;

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;

namespace PN.Storage
{
    /// <summary>
    /// Secure Storage Service
    /// </summary>
    //public class SSS
    //{
    //    public static void ClearAll(bool soft = false)
    //    {
    //        var fields = typeof(SSS).GetProperties(BindingFlags.Public | BindingFlags.Static)
    //                                .Select(f => f.Name)
    //                                .ToList();

    //        // TODO: Add soft deleting attribute

    //        foreach (var field in fields)
    //        {
    //            if (CrossSecureStorage.Current.HasKey(field))
    //            {
    //                CrossSecureStorage.Current.DeleteKey(field);
    //            }
    //        }
    //    }
        
    //    private class C
    //    {
    //        public static string ToString(byte[] bytes)
    //        {
    //            return Encoding.UTF8.GetString(bytes);
    //        }
    //        public static string ToString(System.IObservable<byte[]> IObytes)
    //        {
    //            return ToString(IObytes.Wait());
    //        }
    //        public static byte[] ToArray(string str)
    //        {
    //            return Encoding.UTF8.GetBytes(str);
    //        }
    //    }

    //    private static dynamic R(string str, string name, Type type)
    //    {
    //        try
    //        {
    //            var decrypt = CryptServiceNew.Decrypt(str, name);
    //            return JsonConvert.DeserializeObject(decrypt, type);
    //        }
    //        catch (Exception ex)
    //        {
    //            try
    //            {
    //                Debug.WriteLine(ex.ToString());
    //                var decrypt = CryptServiceNew.Decrypt(str, name);
    //                return JsonConvert.DeserializeObject(decrypt, type);
    //            }
    //            catch (Exception ex2)
    //            {
    //                Debug.WriteLine(ex2.ToString());
    //                return null;
    //            }
    //        }
    //    }

    //    private static string S(dynamic value, string name)
    //    {
    //        try
    //        {
    //            var json = JsonConvert.SerializeObject(value);
    //            var encrypt = CryptServiceNew.Encrypt(json, name);
    //            return encrypt;
    //        }
    //        catch (Exception ex)
    //        {
    //            Debug.WriteLine(ex.ToString());
    //            try
    //            {
    //                var json = JsonConvert.SerializeObject(value);
    //                var encrypt = CryptServiceNew.Encrypt(json, name);
    //                return encrypt;
    //            }
    //            catch (Exception ex2)
    //            {
    //                Debug.WriteLine(ex2.ToString());
    //                return null;
    //            }
    //        }
    //    }


    //    private static dynamic Get([CallerMemberName]string propertyName = "")
    //    {
    //        var propertyType = typeof(SSS).GetProperties().FirstOrDefault(f => f.Name == propertyName).PropertyType;

    //        if (DetectIfAppRunningOnRealIphone)
    //        {
    //            if (!CrossSecureStorage.Current.HasKey(propertyName))
    //                return null;

    //            return R(CrossSecureStorage.Current.GetValue(propertyName), propertyName, propertyType);
    //        }
    //        else
    //        {
    //            try
    //            {
    //                return R(C.ToString(Akavache.BlobCache.Secure.Get(propertyName)), propertyName, propertyType);
    //            }
    //            catch (Exception ex)
    //            {
    //                if (!(ex is KeyNotFoundException))
    //                    Debug.WriteLine(ex.ToString());
    //                return null;
    //            }
    //        }
    //    }

    //    private static void Set(dynamic value, [CallerMemberName]string propertyName = "")
    //    {
    //        if (DetectIfAppRunningOnRealIphone)
    //        {
    //            if (value == null)
    //            {
    //                CrossSecureStorage.Current.DeleteKey(propertyName);
    //            }
    //            else
    //            {
    //                CrossSecureStorage.Current.SetValue(propertyName, S(value, propertyName));
    //            }
    //        }
    //        else
    //        {
    //            if (value == null)
    //            {
    //                Akavache.BlobCache.Secure.Invalidate(propertyName);
    //            }
    //            else
    //            {
    //                Akavache.BlobCache.Secure.Insert(propertyName, C.ToArray(S(value, propertyName)));
    //            }
    //        }
    //    }

    //    public static string Example { get => Get(); set => Set(value); }
    //}

}
