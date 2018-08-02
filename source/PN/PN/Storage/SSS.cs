using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Reflection;
using Newtonsoft.Json;
using System.Text;
using PN.Crypt;
using System;
using System.Linq.Expressions;
using System.Linq;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace PN.Storage
{
    public abstract class SSS
    {
        #region Extern methods

        protected abstract string Get(string key);
        protected abstract void Set(string key, string value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected static dynamic Base() => BasePrivate();

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected static void Base(object value) => BasePrivate(value);

        #endregion

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static dynamic BasePrivate(object value = null)
        {
            var methodInfo = GetMethodInfo();
            
            var meth = methodInfo.ReflectedType.GetMethod(methodInfo.IsGet ? "Get" : "Set", 
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

            if (meth == null)
                throw new NotImplementedException(
                    $"Can't find {(methodInfo.IsGet ? "Get" : "Set")} method implementation in your class {methodInfo.ReflectedType}.\n" +
                    $"Your inheriting class should implement {typeof(ISSS).FullName} interface.\n" +
                    $"Accessibility Level of {typeof(ISSS).FullName} interface methods can be either public or not public (protected, etc).");

            var instance = meth.IsStatic ? null : Activator.CreateInstance(methodInfo.ReflectedType);

            if (methodInfo.IsGet)
            {
                return StringToObject((string)meth.Invoke(instance, new object[] { methodInfo.Name }), methodInfo.CryptKey, methodInfo.Type);
            }
            else
            {
                return meth.Invoke(instance, new object[] { methodInfo.Name, ObjectToString(value, methodInfo.CryptKey) });
            }
        }
        
        private static (string Name, Type Type, Type ReflectedType, bool IsGet, string CryptKey) GetMethodInfo()
        {
            var caller = new StackTrace().GetFrame(3).GetMethod() as MethodInfo;
            
            var propertyInfo = caller.ReflectedType.GetProperties(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                                       .FirstOrDefault(p => p.Name == caller.Name.Remove(0, 4));
            
            var reflectedKey = propertyInfo?.ReflectedType.GetCustomAttributes()?.OfType<CryptKeyAttribute>()?.FirstOrDefault()?.Key;
            var propertyKey = propertyInfo?.GetCustomAttributes()?.OfType<CryptKeyAttribute>()?.FirstOrDefault()?.Key;
            var useDefaultCryptKey = propertyInfo?.GetCustomAttributes()?.OfType<DefaultCryptKeyAttribute>()?.FirstOrDefault() != null;

            var real_key = 
                (useDefaultCryptKey ? caller.Name.Remove(0, 4) : null) ??
                GetValueOfStringByName<string>(propertyKey, caller.ReflectedType) ??
                GetValueOfStringByName<string>(reflectedKey, caller.ReflectedType) ??
                caller.Name.Remove(0, 4);

            return (caller.Name.Remove(0, 4), caller.ReturnType, caller.ReflectedType, caller.ReturnType != typeof(void), real_key);
        }

        private static T GetValueOfStringByName<T>(String name, Type reflectedType)
        {
            var prop = reflectedType.GetProperty(name ?? string.Empty, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            var field = reflectedType.GetField(name ?? string.Empty, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            var value = prop?.GetValue(null) ?? field?.GetValue(null);

            if (prop == null && field == null)
                throw new NullReferenceException($"Field or property '{name}' not found in '{reflectedType}'.");

            if (value == null)
                throw new NullReferenceException($"Field or property '{name}' returns null.");

            if (value.GetType() != typeof(string))
                throw new ArgumentException($"Field or property '{name}' should be type of string, not '{value.GetType()}'.");

            return (T)value;
        }

        private static dynamic StringToObject(string source, string keyToDecrypt, Type type)
        {
            if (string.IsNullOrEmpty(source))
                return Utils.Utils.Internal.CreateDefaultObject(type, true);

            var decrypt = AES.Decrypt(source, keyToDecrypt);
            return JsonConvert.DeserializeObject(decrypt, type);
        }

        private static string ObjectToString(object value, string keyToEncrypt)
        {
            if (value == null)
                return null;

            var json = JsonConvert.SerializeObject(value);
            return AES.Encrypt(json, keyToEncrypt);
        }

        private static string CryptoKey { get; set; }
        public static void Init(string keyToEncrypt)
        {
            CryptoKey = AES.SHA256Hash(keyToEncrypt);
        }

        #region Attributes

        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        protected class CryptKeyAttribute : Attribute
        {
            public string Key { get; set; }
            public CryptKeyAttribute(string key) { Key = key; }
        }

        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        protected class DefaultCryptKeyAttribute : Attribute { }

        #endregion

        public interface ISSS
        {
            string Get(string key);
            void Set(string key, string value);
        }

    }

    //public interface ISSS
    //{
    //    string GetByKey(string key);
    //}

    ///// <summary>
    ///// Secure Storage Service
    ///// </summary>
    //public abstract class SSS<T> where T : ISSS, new()
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

    //    //public static string GetByKey(string key)
    //    //{
    //    //    return "";

    //    //    // throw new NotImplementedException("Need override this method according to project/platform specific implementation.");
    //    //}

            


    //    public static string Example { get => Get(); set => Set(value); }
    //}

}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member