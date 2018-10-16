using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PN.Storage
{
    public class SQLite
    {
        public static IList Get(Type type)
        {
            return (IList) Worker.ExecuteQuery(null, type);
        }

        public static List<T> Get<T>()
        {
            return (List<T>)Worker.ExecuteQuery(null, typeof(T));
        }

        public static int GetCount<T>()
        {
            return (int)Worker.ExecuteQuery(null, typeof(T));
        }

        public static object Set(params object[] data)
        {
            return Worker.ExecuteQuery(data);
        }

        public static void Update(params object[] data)
        {
            Worker.ExecuteQuery(data);
        }

        public static void Delete(params object[] data)
        {
            Worker.ExecuteQuery(data);
        }

        public static void Truncate<T>(params object[] data)
        {
            Worker.ExecuteQuery(data, typeof(T));
        }

        public static void ExecuteString(params object[] data)
        {
            Worker.ExecuteQuery(data, null);
        }


        public static string PathToDB { get; set; }


        public static WhereCondition WhereAND(string propertyName, Is operation, params object[] parameters)
        {
            return WherePrivate(WhereCondition.ConditionTypes.AND, propertyName, operation, parameters);
        }
        public static WhereCondition WhereOR(string propertyName, Is operation, params object[] parameters)
        {
            return WherePrivate(WhereCondition.ConditionTypes.OR, propertyName, operation, parameters);
        }
        private static WhereCondition WherePrivate(WhereCondition.ConditionTypes condType, string propertyName, Is operation, params object[] parameters)
        {
            return new WhereCondition()
            {
                Conditions = new List<WhereCondition.Condition>()
                {
                    new WhereCondition.Condition
                    {
                        PropertyName = propertyName,
                        Operation = operation,
                        Parameters = parameters,
                    },
                },
                ConditionType = condType,
            };
        }


        internal class Worker
        {
            // Исполняем любой запрос к БД
            internal static object ExecuteQuery(object[] data = null, Type resultType = null, WhereCondition where = null)
            {
                var commandName = GetCurrentMethodName();

                // Проверяем по сути пароль
                if (!CheckConnection())
                    return null;

                if (commandName.Equals("Truncate") == false)
                {
                    // Проверяем, не подсунули ли нам пустоту вместо данных для работы
                    if ((commandName.Contains("Get") == false && commandName.Contains("Delete") == false) && ((data == null) || (data.Count() < 1)))
                        return null;

                    if ((data ?? new object[0]).Any(d => d == null))
                        return null;
                }

                if (resultType == null && (data ?? new object[0]).Count() == 0)
                    return null;

                resultType = resultType ?? data[0].GetType();
                var tableName = '"' + (resultType.GetCustomAttribute<SQLiteNameAttribute>()?.Name ?? resultType.Name + "s") + '"';
                var props = resultType.GetProperties(bindingFlags).Where(prop => prop.GetCustomAttribute<SQLiteIgnoreAttribute>() == null).ToList();
                
                // Открываем соединение к БД (после запроса автоматом закроем его)
                using (SQLiteConnection conn = GetConnection())
                {
                    conn.Open();
                    var command = conn.CreateCommand();

                    switch (commandName)
                    {
                        #region Get/GetCount method implementation

                        case "Get":
                            command.CommandText = $"SELECT * FROM {tableName} {CreateWherePartOfSqlRequest(where, props)}";
                            return GetResultsFromDB(command, resultType, props);

                        case "GetCount":
                            command.CommandText = $"SELECT COUNT(*) FROM {tableName} {CreateWherePartOfSqlRequest(where, props)}";
                            using (SQLiteDataReader sqliteDataReader = command.ExecuteReader())
                            {
                                sqliteDataReader.Read();
                                return sqliteDataReader.GetInt32(0);
                            }

                        #endregion

                        #region Set method implementation

                        case "Set":

                            var tableToInsertPartOfQuery = $"INSERT INTO {tableName}";

                            props.Remove(props.FirstOrDefault(prop => prop.Name.ToLower() == "id"));

                            var strWithFields = $" ({string.Join(",", props.Select(prop => GetPropertyNameInTable(prop)).ToArray()).TrimEnd(',')}) values ";
                            var strWithValues = string.Empty;
                            foreach (var obj in data)
                            {
                                strWithValues += '(';
                                foreach (var prop in props)
                                {
                                    var value = prop.GetValue(obj, null);
                                    value = (value != null && value is string) ? (value as string).Replace("\'", "\'\'") : value;
                                    
                                    if (prop.PropertyType.IsValueType == false && prop.PropertyType != typeof(string))
                                        value = ObjectToString(value);
                                    
                                    strWithValues += value == null ? "NULL," : $"'{value}',";
                                }

                                strWithValues = strWithValues.TrimEnd(',') + "),";
                            }

                            strWithValues = strWithValues.TrimEnd(',') + ";";
                            
                            command.CommandText = tableToInsertPartOfQuery + strWithFields + strWithValues;

                            command.ExecuteNonQuery();
                            break;

                        #endregion

                        #region Update method implementation

                        case "Update":

                            var tableToInsertPartOfQueryForUpdateMethod = $"UPDATE {tableName} SET ";

                            props.Remove(props.FirstOrDefault(prop => prop.Name.ToLower() == "id"));

                            var idsEnum = string.Empty;
                            var groupString = string.Empty;
                            foreach (var prop in props)
                            {
                                groupString += GetPropertyNameInTable(prop) + " = CASE id ";
                                foreach (var obj in data)
                                {
                                    var value = prop.GetValue(obj, null);
                                    value = value != null && value is string ? (value as string).Replace("\'", "\'\'") : value;

                                    var id = resultType.GetProperty("id", bindingFlags | BindingFlags.IgnoreCase).GetValue(obj, null);
                                    
                                    if (prop.PropertyType.IsValueType == false && prop.PropertyType != typeof(string))
                                        value = ObjectToString(value);

                                    groupString += $"WHEN {id} THEN " + (value == null ? "NULL " : $"'{value}' ");

                                    idsEnum += id + ",";
                                }

                                groupString += "END,";
                            }
                            
                            groupString = $"{groupString.TrimEnd(',')} WHERE id IN ({idsEnum.TrimEnd(',')});";

                            command.CommandText = tableToInsertPartOfQueryForUpdateMethod + groupString;
                            command.ExecuteNonQuery();

                            break;

                        #endregion

                        #region Delete method implementation

                        case "Delete":
                            command.CommandText = $"DELETE FROM {tableName} {CreateWherePartOfSqlRequest(where, props)}";

                            command.ExecuteNonQuery();
                            break;

                        case "DeleteOld":
                            var idsEnumForDeleteMethod = string.Empty;
                            foreach (var obj in data)
                            {
                                idsEnumForDeleteMethod += resultType.GetProperty("id", bindingFlags | BindingFlags.IgnoreCase).GetValue(obj, null) + ",";
                            }

                            idsEnumForDeleteMethod = idsEnumForDeleteMethod.TrimEnd(',');

                            command.CommandText = $"DELETE FROM {tableName} WHERE id IN ({idsEnumForDeleteMethod});";

                            command.ExecuteNonQuery();
                            break;
                        #endregion

                        #region Truncate method implementation

                        case "Truncate":
                            //   TRUNCATE[TABLE] tbl_name
                            // command.CommandText = String.Format("SELECT * FROM {0};", resultType.Name + 's');
                            command.CommandText = string.Format($"DELETE FROM {tableName}");

                            command.ExecuteNonQuery();
                            break;

                        #endregion

                        #region ExecuteQuery method implementation

                        case "ExecuteString":
                            
                            command.CommandText = data[0].ToString();

                            command.ExecuteNonQuery();
                            break;

                            #endregion
                    }

                    return null;
                }
            }

            private static string CreateWherePartOfSqlRequest(WhereCondition where, List<PropertyInfo> props)
            {
                if (where == null)
                    return ";";

                var commandText = string.Empty;

                for (int i = 0; i < where.Conditions.Count; i++)
                {
                    var condition = where.Conditions[i];

                    if (condition.Parameters.Length < 1)
                        continue;

                    commandText += i == 0 ? " WHERE (" : string.Empty;

                    var quote = condition.Parameters[0] is string ? "'" : string.Empty;
                    switch (condition.Operation)
                    {
                        case Is.BiggerThen:
                        case Is.LessThen:
                        case Is.Equals:
                        case Is.NotEquals:
                            if (condition.Parameters.Length > 0)
                                commandText += $"{condition.PropertyName} " +
                                                WhereCondition.SimpleOperators[condition.Operation] +
                                               $" {quote}{condition.Parameters[0]}{quote}";
                            break;

                        case Is.Like:
                            if (condition.Parameters.Length > 0)
                                commandText += $"{condition.PropertyName} LIKE {quote}%{condition.Parameters[0]}%{quote}";
                            break;

                        case Is.Between:
                            if (condition.Parameters.Length > 1)
                                commandText += $"{condition.PropertyName} BETWEEN {condition.Parameters[0]} AND {condition.Parameters[1]}";
                            break;

                        case Is.In:
                            if (condition.Parameters.Length > 0)
                            {
                                commandText += $"{condition.PropertyName} IN (";
                                var qut = condition.Parameters[0] is List<string> ? "'" : string.Empty;
                                foreach (var arrItem in condition.Parameters[0] as IList)
                                    commandText += $"{qut}{arrItem}{qut},";
                                commandText = commandText.TrimEnd(',') + ")";
                            }

                            break;

                        case Is.Contains:
                            if (string.IsNullOrEmpty(condition.PropertyName))
                                foreach (var prop in props)
                                    foreach (var subStr in condition.Parameters)
                                        commandText += " " + GetPropertyNameInTable(prop) + " LIKE '%" + subStr + "%' OR";
                            else
                                foreach (var arrItem in (condition.Parameters[0] is IList)
                                        ? condition.Parameters[0] as IList
                                        : condition.Parameters)
                                    commandText += " " + condition.PropertyName + " LIKE '%" + arrItem + "%' OR";

                            if (commandText.Trim().EndsWith("OR"))
                                commandText = commandText.Trim().Remove(commandText.Length - 3);
                            break;

                        case Is.ContainsAnythingFrom:
                            foreach (var prop in props)
                                foreach (var subStr in condition.Parameters)
                                    commandText += $" {GetPropertyNameInTable(prop)} LIKE '%{subStr}%' OR";

                            if (commandText.Trim().EndsWith("OR"))
                                commandText = commandText.Trim().Remove(commandText.Length - 3);
                            break;

                        case Is.LimitedBy:
                            where.Limit = (int)condition.Parameters[0];
                            break;

                        case Is.Reversed:
                            where.Reverse = (bool)condition.Parameters[0];
                            break;
                    }

                    if (condition.Operation != Is.Reversed && condition.Operation != Is.LimitedBy)
                        commandText += $") {where.ConditionType} (";
                }

                if (where.Conditions.Count != 0)
                    commandText = commandText.Trim().Substring(0, commandText.Length - $" {where.ConditionType} (".Length);

                if (where.Reverse)
                    commandText += " ORDER BY id DESC";

                commandText += where.Limit > -1 ? $" LIMIT {where.Limit};" : ";";

                return commandText;
            }

            private static BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic |
                                                       BindingFlags.Static | BindingFlags.Instance |
                                                       BindingFlags.DeclaredOnly;

            static IList GetResultsFromDB(SQLiteCommand command, Type resultType, List<PropertyInfo> props)
            {
                using (SQLiteDataReader sqliteDataReader = command.ExecuteReader())
                {
                    // Создаём лист типов, которые надо вернуть
                    var result = CreateList(resultType);
                    // Пробегаемся по всем строкам результата
                    while (sqliteDataReader.Read())
                    {
                        // Для каждой строки создаём свой объект нужного нам типа
                        var resObj = Activator.CreateInstance(resultType);

                        foreach (var prop in props)
                        {
                            //Заносим значения ячеек в наш новый объект
                            try
                            {
                                var propertyNameInTable = GetPropertyNameInTable(prop);
                                var objectFromSQLiteDataReader = sqliteDataReader[propertyNameInTable];

                                var objectToSetToProperty = (prop.PropertyType.IsValueType || prop.PropertyType == typeof(string)) ?
                                    Convert.ChangeType(objectFromSQLiteDataReader, prop.PropertyType) :
                                    StringToObject((string)objectFromSQLiteDataReader, prop.PropertyType);

                                prop.SetValue(resObj, objectFromSQLiteDataReader != DBNull.Value ? objectToSetToProperty : null, null);
                            }
                            catch (Exception ex)
                            {
                                prop.SetValue(resObj, GetDefaultValue(prop.PropertyType), null);
                            }
                        }

                        // И, наконец, добавляем полученный объект в лист с результатами
                        result.Add(resObj);
                    }

                    return result;
                }
            }

            static object StringToObject(string source, Type type)
            {
                if (string.IsNullOrEmpty(source))
                    return Utils.Utils.Internal.CreateDefaultObject(type, true);

                var decrypt = Crypt.AES.Decrypt(source, "SQLite");
                return InternalNewtonsoft.Json.JsonConvert.DeserializeObject(decrypt, type);
            }

            static string ObjectToString(object value)
            {
                if (value == null)
                    return null;

                var json = InternalNewtonsoft.Json.JsonConvert.SerializeObject(value);
                return Crypt.AES.Encrypt(json, "SQLite");
            }

            // Get the type of calling method
            // It's may be Get | Set | Update | Delete
            private static string GetCurrentMethodName()
            {
                StackTrace st = new StackTrace();

                // frame 1 = ExecuteQuery
                // frame 2 = Get | Set | Update | Delete
                StackFrame sf = st.GetFrame(2);
                return sf.GetMethod().Name;
            }


            static object GetDefaultValue(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

            static IList CreateList(Type listItemType)
            {
                Type genericListType = typeof(List<>).MakeGenericType(listItemType);
                return (IList)Activator.CreateInstance(genericListType);
            }
            
            static string GetPropertyNameInTable(PropertyInfo prop) => prop.GetCustomAttribute<SQLiteNameAttribute>()?.Name ?? prop.Name;

            static SQLiteConnection GetConnection()
            {
                if (Utils.Utils.Internal.CurrentPlatformIsWindows)
                {
                    if (Directory.Exists("x64") == false)
                    {
                        Directory.CreateDirectory("x64");
                    }

                    if (Directory.Exists("x86") == false)
                    {
                        Directory.CreateDirectory("x86");
                    }

                    //if (File.Exists("System.Data.SQLite.dll") == false)
                    //{
                    //    Utils.Utils.Internal.WriteResourceToFile("PN.SQLiteDlls.System.Data.SQLite.dll", "System.Data.SQLite.dll");
                    //}

                    if (File.Exists("x64/SQLite.Interop.dll") == false)
                    {
                        Utils.Utils.Internal.WriteResourceToFile("PN.SQLiteDlls.x64.SQLite.Interop.dll", "x64/SQLite.Interop.dll");
                    }

                    if (File.Exists("x86/SQLite.Interop.dll") == false)
                    {
                        Utils.Utils.Internal.WriteResourceToFile("PN.SQLiteDlls.x86.SQLite.Interop.dll", "x86/SQLite.Interop.dll");
                    }
                }

                var path = Path.GetFullPath(PathToDB ?? throw new ArgumentException("Path to DB (SQLite) is not set!"));
                // var pass = string.Empty;

                return new SQLiteConnection($"Data Source={path};Version=3;");
            }

            static bool CheckConnection()
            {
                try
                {
                    using (SQLiteConnection conn = GetConnection())
                    {
                        conn.Open();
                        using (SQLiteCommand command = new SQLiteCommand("PRAGMA schema_version;", conn))
                        {
                            var ret = command.ExecuteScalar();
                        }

                        return true;
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    Utils.Utils.Debug.Log(ex, true);
#endif
                    return false;
                }
            }



            //
            // internal static bool CheckConnection(string procid, string DBPath)
            // {
            //     try
            //     {
            //         using (SQLiteConnection conn = GetConnection())
            //         {
            //             conn.Open();
            //             using (SQLiteCommand command = new SQLiteCommand("PRAGMA schema_version;", conn))
            //             {
            //                 var ret = command.ExecuteScalar();
            //             }
            //
            //             return true;
            //         }
            //     }
            //     catch (Exception ex)
            //     {
            //         return false;
            //     }
            // }
        }


        #region Attributes

        /// <summary>
        /// Name of the table or the field in SQLite DB
        /// </summary>
        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        public class SQLiteNameAttribute : Attribute
        {
            public readonly string Name;

            public SQLiteNameAttribute(string name)
            {
                Name = name;
            }
        }

        /// <summary>
        /// GlobalHeadersAttribute will be ignored for action where you will define IgnoreHeadersAttribute
        /// </summary>
        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
        public class SQLiteIgnoreAttribute : Attribute { }
        
        #endregion

    }

    public enum Is
    {
        ///<summary>A > B</summary>
        BiggerThen,

        ///<summary>A < B</summary>
        LessThen,

        ///<summary>A == B</summary>
        Equals,

        ///<summary>A != B</summary>
        NotEquals,

        ///<summary>A == %B%</summary>
        Like,

        ///<summary>a < B < c</summary>
        Between,

        ///<summary>A.Contains('123')</summary>
        Contains,

        ///<summary>A.Contains('123') or A.Contains('456') or ...</summary>
        ContainsAnythingFrom,

        ///<summary>.....</summary>
        In,

        ///<summary>Get only N last results (N as parameter)</summary>
        LimitedBy,

        ///<summary>Get bool type as parameter</summary>
        Reversed,
    }
    
    public class WhereCondition
    {
        public WhereCondition Where(string propertyName, Is operation, params object[] parameters)
        {
            Conditions.Add(new Condition
            {
                PropertyName = propertyName,
                Operation = operation,
                Parameters = parameters,
            });

            return this;
        }

        internal enum ConditionTypes { AND, OR, }

        internal static Dictionary<Is, string> SimpleOperators = new Dictionary<Is, string>
        {
            {
                Is.BiggerThen, ">"
            },
            {
                Is.LessThen, "<"
            },
            {
                Is.Equals, "="
            },
            {
                Is.NotEquals, "!="
            },
        };

        internal class Condition
        {
            internal string PropertyName;
            internal Is Operation;
            internal object[] Parameters;
        }

        internal List<Condition> Conditions = new List<Condition>();
        internal long Limit = -1;
        internal bool Reverse = false;
        internal ConditionTypes ConditionType = ConditionTypes.AND;

        public List<T> Get<T>() => (List<T>) SQLite.Worker.ExecuteQuery(null, typeof(T), this);

        public IList Get(Type type) => (IList)SQLite.Worker.ExecuteQuery(null, type, this);

        public int GetCount<T>() => (int) SQLite.Worker.ExecuteQuery(null, typeof(T), this);

        public void Delete<T>(params object[] data) => SQLite.Worker.ExecuteQuery(data, typeof(T), this);

        public void Delete(Type type, params object[] data) => SQLite.Worker.ExecuteQuery(data, type, this);
    }
}