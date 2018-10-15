//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace PN.Storage
//{
//    class SQLite
//    {
//    }
//}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable ConvertToConstant.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable InvalidXmlDocComment
// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable UnusedVariable
// ReSharper disable MemberCanBePrivate.Local

namespace PN.Storage
{
    public class SQLite
    {
        public static IList Get(Type type, params object[] data)
        {
            return (IList)Worker.ExecuteQuery(data, type);
        }

        public static List<T> Get<T>(params object[] data)
        {
            return (List<T>)Worker.ExecuteQuery(data, typeof(T));
        }

        public static List<T> GetSpecial<T>(long lastId,
                                            int count,
                                            List<string> subStrings = null,
                                            bool getElemsBefore = false)
        {
            return (List<T>)Worker.ExecuteQuery(null, typeof(T), (int)lastId, count, subStrings, getElemsBefore);
        }

        public static int GetCount<T>(List<string> subStrings = null, int lastId = -1)
        {
            return (int)Worker.ExecuteQuery(null, typeof(T), lastId, -1, subStrings);
        }

        public static List<T> GetWhere<T>(WhereCondition where)
        {
            return (List<T>)Worker.ExecuteQuery(null, typeof(T), -1, -1, null, false, where);
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

        public class WhereCondition
        {
            public enum Funcs
            {
                ///<summary>A > B</summary>
                BiggerThen,

                ///<summary>A < B</summary>
                LessThen,

                ///<summary>A == B</summary>
                Equals,

                ///<summary>A != B</summary>
                NotEquals,

                ///<summary>A==%B%</summary>
                Like,

                ///<summary>a < B < c</summary>
                Between,

                ///<summary>A.Contains('123')</summary>
                Contains,

                ///<summary>.....</summary>
                In,
            }

            public enum ConditionTypes
            {
                And,
                Or,
            }

            public static Dictionary<Funcs, string> SimpleOperators = new Dictionary<Funcs, string>
            {
                {
                    Funcs.BiggerThen, ">"
                },
                {
                    Funcs.LessThen, "<"
                },
                {
                    Funcs.Equals, "="
                },
                {
                    Funcs.NotEquals, "!="
                },
            };

            public class Condition
            {
                internal string PropertyName;
                internal Funcs Operation;
                internal object[] Parameters;
            }

            public List<Condition> Conditions = new List<Condition>();
            public long Limit = -1;
            public bool Reverse = false;
            public ConditionTypes ConditionType = ConditionTypes.And;

            public void Add(string propertyName, Funcs operation, params object[] parameters)
            {
                Conditions.Add(new Condition
                {
                    PropertyName = propertyName,
                    Operation = operation,
                    Parameters = parameters,
                });
            }
        }

        class Worker
        {
            // Исполняем любой запрос к БД
            internal static object ExecuteQuery(object[] data = null,
                                                Type resultType = null,
                                                long lastId = -1,
                                                int count = -1,
                                                List<string> subStrings = null,
                                                bool getElemsBefore = false,
                                                WhereCondition where = null)
            {
                var commandName = GetCurrentMethodName();

                // Проверяем по сути пароль
                if (!CheckConnection())
                    return null;

                if (commandName.Equals("Truncate") == false)
                {
                    // Проверяем, не подсунули ли нам пустоту вместо данных для работы
                    if ((!commandName.Contains("Get")) && ((data == null) || (data.Count() < 1)))
                        return null;

                    if ((data ?? new object[0]).Any(d => d == null))
                        return null;
                }

                resultType = resultType ?? data[0].GetType();
                var tableName = resultType.GetCustomAttribute<SQLiteNameAttribute>()?.Name ?? resultType.Name + "s";
                var props = resultType.GetProperties(bindingFlags).Where(prop => prop.GetCustomAttribute<SQLiteIgnoreAttribute>() == null).ToList();
                
                // Открываем соединение к БД (после запроса автоматом закроем его)
                using (SQLiteConnection conn = GetConnection())
                {
                    // Получаем список полей типа, который должны вернуть
                    // var fieldNames = resultType.GetProperties(bindingFlags).Select(field => field.Name).ToList();

                    conn.Open();
                    var command = conn.CreateCommand();

                    switch (commandName)
                    {
                        #region GetCount method implementation

                        case "GetCount":
                            command.CommandText = $"SELECT COUNT(*) FROM {tableName}";

                            if (subStrings != null)
                            {
                                command.CommandText += " WHERE (";

                                foreach (var prop in props)
                                    foreach (var subStr in subStrings)
                                        command.CommandText += $" {GetPropertyNameInTable(prop)} LIKE '%{subStr}%' OR";

                                command.CommandText = command.CommandText.Trim().Substring(0, command.CommandText.Length - 2) + ") ";
                            }

                            if (lastId > -1)
                                command.CommandText += $" {(subStrings != null ? " AND " : " WHERE ")}  (id > {lastId})";

                            command.CommandText += ";";

                            using (SQLiteDataReader sqliteDataReader = command.ExecuteReader())
                            {
                                sqliteDataReader.Read();

                                return sqliteDataReader.GetInt32(0);
                            }

                        #endregion

                        #region Get & GetSpecial method implementation

                        case "Get":
                        case "GetSpecial":
                            command.CommandText = $"SELECT * FROM {tableName}";

                            if (subStrings != null)
                            {
                                command.CommandText += " WHERE (";

                                foreach (var prop in props)
                                    foreach (var subStr in subStrings)
                                        command.CommandText += $" {GetPropertyNameInTable(prop)} LIKE '%{subStr}%' OR";

                                command.CommandText = command.CommandText.Trim().Substring(0, command.CommandText.Length - 2) + ") ";
                            }

                            if (lastId > -1)
                                command.CommandText += $" {(subStrings != null ? " AND " : " WHERE ")} (id {(getElemsBefore ? "<" : ">")}  {lastId})";

                            if (getElemsBefore)
                                command.CommandText += " ORDER BY id DESC ";

                            command.CommandText += count > -1 ? $" LIMIT {count};" : ";";
                            
                            return GetResultsFromDB(command, resultType, props);

                        #endregion

                        #region GetWhere method implementation

                        case "GetWhere":
                            command.CommandText = $"SELECT * FROM {tableName}";

                            for (int i = 0; i < where.Conditions.Count; i++)
                            {
                                var condition = where.Conditions[i];

                                if (condition.Parameters.Length < 1)
                                    continue;

                                command.CommandText += i == 0 ? " WHERE (" : string.Empty;
                                var quote = condition.Parameters[0] is string ? "'" : string.Empty;
                                switch (condition.Operation)
                                {
                                    case WhereCondition.Funcs.BiggerThen:
                                    case WhereCondition.Funcs.LessThen:
                                    case WhereCondition.Funcs.Equals:
                                    case WhereCondition.Funcs.NotEquals:
                                        if (condition.Parameters.Length > 0)
                                            command.CommandText += $"{condition.PropertyName} " +
                                                                   WhereCondition.SimpleOperators[condition.Operation] +
                                                                   $" {quote}{condition.Parameters[0]}{quote}";
                                        break;

                                    case WhereCondition.Funcs.Like:
                                        if (condition.Parameters.Length > 0)
                                            command.CommandText += $"{condition.PropertyName} LIKE " +
                                                                   $"{quote}%{condition.Parameters[0]}%{quote}";
                                        break;

                                    case WhereCondition.Funcs.Between:
                                        if (condition.Parameters.Length > 1)
                                            command.CommandText += $"{condition.PropertyName} BETWEEN " +
                                                                   $"{condition.Parameters[0]} AND {condition.Parameters[1]}";
                                        break;

                                    case WhereCondition.Funcs.In:
                                        if (condition.Parameters.Length > 0)
                                        {
                                            command.CommandText += $"{condition.PropertyName} IN (";
                                            var qut = condition.Parameters[0] is List<string> ? "'" : string.Empty;
                                            foreach (var arrItem in condition.Parameters[0] as IList)
                                                command.CommandText += $"{qut}{arrItem}{qut},";
                                            command.CommandText = command.CommandText.TrimEnd(',') + ")";
                                        }

                                        break;

                                    case WhereCondition.Funcs.Contains:
                                        if (string.IsNullOrEmpty(condition.PropertyName))
                                            foreach (var prop in props)
                                                foreach (var subStr in condition.Parameters)
                                                    command.CommandText += " " + GetPropertyNameInTable(prop) + " LIKE '%" + subStr + "%' OR";
                                        else
                                            foreach (var arrItem in (condition.Parameters[0] is IList)
                                                    ? condition.Parameters[0] as IList
                                                    : condition.Parameters)
                                                command.CommandText +=
                                                        " " + condition.PropertyName + " LIKE '%" + arrItem + "%' OR";


                                        if (command.CommandText.Trim().EndsWith("OR"))
                                            command.CommandText = command
                                                                  .CommandText.Trim()
                                                                  .Substring(0, command.CommandText.Length - 2);
                                        break;
                                }

                                command.CommandText += $") {where.ConditionType} (";
                            }

                            if (where.Conditions.Count != 0)
                                command.CommandText = command
                                                      .CommandText.Trim()
                                                      .Substring(0,
                                                                 command.CommandText.Length -
                                                                 $" {where.ConditionType} (".Length);

                            if (where.Reverse)
                                command.CommandText += " ORDER BY id DESC ";

                            command.CommandText += where.Limit > -1 ? $" LIMIT {where.Limit};" : ";";

                            return GetResultsFromDB(command, resultType, props);

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
                                    strWithValues += value == null ? "NULL," : "'{value}',";
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
                            var idsEnumForDeleteMethod = string.Empty;
                            foreach (var obj in data)
                            {
                                idsEnumForDeleteMethod += resultType.GetProperty("id", bindingFlags | BindingFlags.IgnoreCase).GetValue(obj, null) + ",";
                            }

                            idsEnumForDeleteMethod = idsEnumForDeleteMethod.TrimEnd(',');

                            command.CommandText = string.Format("DELETE FROM {0} WHERE id IN ({1});",
                                                                resultType.Name + 's', idsEnumForDeleteMethod);
                            command.ExecuteNonQuery();
                            break;

                        #endregion

                        #region Truncate method implementation

                        case "Truncate":
                            //   TRUNCATE[TABLE] tbl_name
                            // command.CommandText = String.Format("SELECT * FROM {0};", resultType.Name + 's');
                            command.CommandText = string.Format($"DELETE FROM {resultType.Name + 's'}");

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
                                prop.SetValue(resObj, Convert.ChangeType(sqliteDataReader[GetPropertyNameInTable(prop)], prop.PropertyType), null);
                            }
                            catch
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
}
