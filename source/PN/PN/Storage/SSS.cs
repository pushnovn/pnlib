using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Reflection;
using Newtonsoft.Json;
using PN.Crypt;
using System;
using System.Linq;
using System.Collections.Generic;

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
        private static object BasePrivate(object value = null, MethodData methodInfo = null)
        {
            methodInfo = methodInfo ?? GetMethodInfo();
            
            var method = GetInternalMethodByName(methodInfo.IsGet, methodInfo.ReflectedType);

            return methodInfo.IsGet ?
                   StringToObject((string)method.Method.Invoke(method.MethodInstance, new object[] { methodInfo.ReflectedType.FullName + Splitter + methodInfo.Name }), methodInfo.CryptKey, methodInfo.Type) :
                   method.Method.Invoke(method.MethodInstance, new object[] { methodInfo.ReflectedType.FullName + Splitter + methodInfo.Name, ObjectToString(value, methodInfo.CryptKey) });
        }
        
        private static MethodData GetMethodInfo()
        {
            var caller = new StackTrace().GetFrame(3).GetMethod() as MethodInfo;
            
            var propertyInfo = caller.ReflectedType.GetProperties(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                                       .FirstOrDefault(p => p.Name == caller.Name.Remove(0, 4));
            
            var useDefaultCryptKey = propertyInfo?.GetCustomAttributes()?.OfType<NeedAuthAttribute>()?.FirstOrDefault() == null;

            if (useDefaultCryptKey == false && CryptKeySettings.Any(i => i.InheritType == caller.ReflectedType) == false)
                throw new Exception($"Need first Auth for {caller.ReflectedType} class.");

            var real_key = useDefaultCryptKey ?
                $"{caller.ReflectedType.FullName}{Splitter}{caller.Name.Remove(0, 4)}" :
                CryptKeySettings.FirstOrDefault(i => i.InheritType == caller.ReflectedType).CryptKeyHash;
            
            return new MethodData()
            {
                Name = caller.Name.Remove(0, 4),
                Type = caller.ReturnType,
                ReflectedType = caller.ReflectedType,
                IsGet = caller.ReturnType != typeof(void),
                CryptKey = real_key
            };
        }

        private static T GetValueOfTypeByName<T>(String name, Type reflectedType)
        {
            var prop = reflectedType.GetProperty(name ?? string.Empty, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            var field = reflectedType.GetField(name ?? string.Empty, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            var value = prop?.GetValue(null) ?? field?.GetValue(null);

            if (prop == null && field == null)
                throw new NullReferenceException($"Field or property '{name}' not found in '{reflectedType}'.");

            if (value == null)
                throw new NullReferenceException($"Field or property '{name}' returns null.");

            if (value.GetType() != typeof(T))
                throw new ArgumentException($"Field or property '{name}' should be type of {typeof(T).FullName}, not '{value.GetType()}'.");

            return (T)value;
        }

        private static void SetValueOfStringByName<T>(string propName, string newValue)
        {
            var prop = typeof(T).GetProperty(propName ?? string.Empty, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            var field = typeof(T).GetField(propName ?? string.Empty, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (prop == null && field == null)
                throw new NullReferenceException($"Field or property '{propName}' not found in '{typeof(T)}'.");

            prop?.SetValue(null, newValue);
            field?.SetValue(null, newValue);
        }

        private static object StringToObject(string source, string keyToDecrypt, Type type)
        {
            if (string.IsNullOrEmpty(source))
                return Utils.Internal.CreateDefaultObject(type, true);

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

        private static (MethodInfo Method, object MethodInstance) GetInternalMethodByName(bool isGet, Type inheritClassType) =>
            GetInternalMethodByName(isGet ? nameof(Get) : nameof(Set), inheritClassType);

        private static (MethodInfo Method, object MethodInstance) GetInternalMethodByName(string name, Type inheritClassType)
        {
            var methodInfo = inheritClassType.GetMethod(name,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (methodInfo == null)
                throw new NotImplementedException(
                    $"Can't find {name} method implementation in your class {inheritClassType}.\n");

            var methodInstance = methodInfo.IsStatic ? null : Activator.CreateInstance(inheritClassType);

            return (methodInfo, methodInstance);
        }
        
        public static bool Auth<TInheritSSS>(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            // Сначала проверяем, существует ли наш виртуальный сейф такого типа Т
            //// Вызываем метод Get с ключом = Hash(typeof(T).FullName + "AttempSetting"), пытаясь получить AttempSetting
            var getMethod = GetInternalMethodByName(true, typeof(TInheritSSS));
            var setMethod = GetInternalMethodByName(false, typeof(TInheritSSS));

            var pathToAttempSetting = AES.SHA256Hash(typeof(TInheritSSS).FullName + "AttempSetting");
            var attempSettingString = (string) getMethod.Method.Invoke(getMethod.MethodInstance, new object[] { pathToAttempSetting });
            var attempSetting = new AttempSetting() { InheritType = typeof(TInheritSSS) };
       
            // Если AttempSetting нет, то:
            if (attempSettingString == null)
            {
                //// Делаем очистку всей базы (ClearAll) с типом Т
                ClearAll<TInheritSSS>();

                //// Cоздаём её через метод Set
                attempSetting.LastUpdateDate = AES.Encrypt(DateTime.Now.ToString(), AES.SHA256Hash(password));
                setMethod.Method.Invoke(setMethod.MethodInstance, new object[] {
                    pathToAttempSetting, ObjectToString(attempSetting, pathToAttempSetting) });

                //// Потом создаём новый экземпляр типа Settings с нужным нам паролем и типом T и запихиваем в List
                CryptKeySettings.Add(new CryptKeySetting() { InheritType = typeof(TInheritSSS), CryptKey = password });

                //// Возвращаем true
                return true;
            }


            // Если AttempSetting есть, то:
            else
            {
                attempSetting = (AttempSetting) StringToObject(attempSettingString, pathToAttempSetting, typeof(AttempSetting));
              
                //// Вызываем метод CheckPassword (внутренний):
                //// Пароль верен:
                if (CheckPassword(password, attempSetting))
                {
                    ////// Создаём новый экземпляр типа CryptKeySetting с нужным нам паролем и типом T и запихиваем в List
                    CryptKeySettings.Add(new CryptKeySetting() { InheritType = typeof(TInheritSSS), CryptKey = password });

                    ////// Обнуляем CurrentCount в AttempSetting
                    attempSetting.CurrentCount = 0;
                    setMethod.Method.Invoke(setMethod.MethodInstance, new object[] {
                        pathToAttempSetting, ObjectToString(attempSetting, pathToAttempSetting) });

                    ////// Возвращаем true
                    return true;
                }

                //// Парол НЕверен:
                else
                {
                    ////// Инкрементим CurrentCount в AttempSetting (если CurrentCount > MaxCount => ClearAll)
                    if (attempSetting.MaxCount > 0 && ++attempSetting.CurrentCount > attempSetting.MaxCount)
                    {
                        ClearAll<TInheritSSS>();
                    }

                    ////// Возвращаем false
                    return false;
                }
            }
        }

#if DEBUG
        public static bool CheckPassword<T>(string password)
        {
            var getMethod = GetInternalMethodByName(true, typeof(T));

            var pathToAttempSetting = AES.SHA256Hash(typeof(T).FullName + "AttempSetting");
            var attempSettingString = (string)getMethod.Method.Invoke(getMethod.MethodInstance, new object[] { pathToAttempSetting });

            var attempSetting = (AttempSetting)StringToObject(attempSettingString, pathToAttempSetting, typeof(AttempSetting));

            return CheckPassword(password, attempSetting);
        }
#endif

        #region Attributes

        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        public class NeedAuthAttribute : Attribute { }

        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        public class IsResistantToSoftRemovalAttribute : Attribute { }

        #endregion
        
        private static bool CheckPassword(string password, AttempSetting attempSetting)
        {
            try
            {
                AES.Decrypt(attempSetting.LastUpdateDate, AES.SHA256Hash(password));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void SetAttempsMaxCountOfEnteringPassword<T>(int max)
        {
            var getMethod = GetInternalMethodByName(true, typeof(T));

            var pathToAttempSetting = AES.SHA256Hash(typeof(T).FullName + "AttempSetting");
            var attempSettingString = (string)getMethod.Method.Invoke(getMethod.MethodInstance, new object[] { pathToAttempSetting });

            if (attempSettingString == null || CryptKeySettings.Any(i => i.InheritType == typeof(T)) == false)
                return;

            var attempSetting = (AttempSetting)StringToObject(attempSettingString, pathToAttempSetting, typeof(AttempSetting));
            attempSetting.MaxCount = max;

            var setMethod = GetInternalMethodByName(false, typeof(T));
            setMethod.Method.Invoke(setMethod.MethodInstance, new object[] {
                pathToAttempSetting, ObjectToString(attempSetting, pathToAttempSetting) });
        }

        public static void UpdatePasswordAndReCrypt<T>(string newPassword)
        {
            if (CryptKeySettings.Any(s => s.InheritType == typeof(T)) == false)
                throw new Exception($"Need first Auth for {typeof(T)} class.");

            if (string.IsNullOrEmpty(newPassword))
                return;

            var allProps = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Static);
            if (allProps == null || allProps.Count() == 0)
                return;

            var filteredProps = new List<PropertyInfo>();
            foreach (var prop in allProps)
            {
                if (prop?.GetCustomAttributes()?.OfType<NeedAuthAttribute>()?.FirstOrDefault() != null)
                {
                    filteredProps.Add(prop);
                }
            }

            if (filteredProps.Count == 0)
                return;
            
            var methodInfo = GetInternalMethodByName(nameof(Set), typeof(T));

            foreach (var prop in filteredProps)
            {
                var propNewValue = prop.GetValue(null);
                methodInfo.Method.Invoke(methodInfo.MethodInstance, new object[] {
                    typeof(T).FullName + Splitter + prop.Name, ObjectToString(prop.GetValue(null), AES.SHA256Hash(newPassword)) });
            }

            CryptKeySettings[CryptKeySettings.IndexOf(CryptKeySettings.FirstOrDefault(s => s.InheritType == typeof(T)))].CryptKey = newPassword;

            
            // ================================================================================
            var getMethod = GetInternalMethodByName(true, typeof(T));
            var setMethod = GetInternalMethodByName(false, typeof(T));

            var pathToAttempSetting = AES.SHA256Hash(typeof(T).FullName + "AttempSetting");
            var attempSettingString = (string)getMethod.Method.Invoke(getMethod.MethodInstance, new object[] { pathToAttempSetting });
               
            var attempSetting = (AttempSetting)StringToObject(attempSettingString, pathToAttempSetting, typeof(AttempSetting));
            attempSetting.LastUpdateDate = AES.Encrypt(DateTime.Now.ToString(), AES.SHA256Hash(newPassword));
            setMethod.Method.Invoke(setMethod.MethodInstance, new object[] { pathToAttempSetting, ObjectToString(attempSetting, pathToAttempSetting) });
            // ================================================================================
        }

        public static void ClearAll<TInheritSSS>(bool SoftClearing = false)
        {
            var methodInfo = GetInternalMethodByName(nameof(Set), typeof(TInheritSSS));
            
            foreach (var prop in typeof(TInheritSSS).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (SoftClearing && prop?.GetCustomAttributes()?.OfType<IsResistantToSoftRemovalAttribute>()?.FirstOrDefault() != null)
                {
                    continue;
                }

                methodInfo.Method.Invoke(methodInfo.MethodInstance, new object[] { typeof(TInheritSSS).FullName + Splitter + prop.Name, null });
            }
            
            var pathToAttempSetting = AES.SHA256Hash(typeof(TInheritSSS).FullName + "AttempSetting");
            methodInfo.Method.Invoke(methodInfo.MethodInstance, new object[] { pathToAttempSetting, null });

            CryptKeySettings.RemoveAll(s => s.InheritType == typeof(TInheritSSS));
        }

        private const string Splitter = "_+_";

        private static List<CryptKeySetting> CryptKeySettings = new List<CryptKeySetting>();

        public static Indexer<TValue, TInherit> Index<TValue, TInherit>() => new Indexer<TValue, TInherit>();
        
        public class Indexer<TValue, TInherit>
        {
            public TValue this [object key]
            {
                get => (TValue)BasePrivate(null, GetMethodData(JsonConvert.SerializeObject(key), true));
                set => BasePrivate(value, GetMethodData(JsonConvert.SerializeObject(key), false));
            }

            private MethodData GetMethodData(string name, bool IsGet = false)
            {
                return new MethodData()
                {
                    Name = name,
                    Type = typeof(TValue),//(IndexerDict[key] = value.GetType()),
                    ReflectedType = typeof(TInherit),
                    IsGet = IsGet,
                    CryptKey = name,
                };
            }
        }
    }
    
    internal class MethodData
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public Type ReflectedType { get; set; }
        public bool IsGet { get; set; }
        public string CryptKey { get; set; }
    }

    internal class AttempSetting
    {
        public Type InheritType { get; set; }
        public int MaxCount { get; set; }
        public int CurrentCount { get; set; }
        public string LastUpdateDate { get; set; }
    }
    
    internal class CryptKeySetting
    {
        public Type InheritType { get; set; }
        public string CryptKeyHash { get => AES.SHA256Hash(CryptKey); }
        public string CryptKey { get; set; }
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