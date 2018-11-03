using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Data.SQLite;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using InternalNewtonsoft.Json;

namespace PN.Storage.New
{
    public class SQLite
    {
        #region ExternalUse methods

        public static IList Get(Type type)
        {
            return (IList)Worker.ExecuteQuery(null, type);
        }

        public static List<T> Get<T>()
        {
            return (List<T>)Worker.ExecuteQuery(null, typeof(T));
        }

        public static int GetCount<T>()
        {
            return (int)Worker.ExecuteQuery(null, typeof(T));
        }

        public static SQLiteMethodResponse Set(params object[] data)
        {
            return (SQLiteMethodResponse)Worker.ExecuteQuery(data);
        }

        public static SQLiteMethodResponse Update(params object[] data)
        {
            return (SQLiteMethodResponse)Worker.ExecuteQuery(data);
        }

        public static SQLiteMethodResponse Delete(params object[] data)
        {
            return (SQLiteMethodResponse)Worker.ExecuteQuery(data);
        }

        public static SQLiteMethodResponse Truncate<T>(params object[] data)
        {
            return (SQLiteMethodResponse)Worker.ExecuteQuery(data, typeof(T));
        }


        public static SQLiteMethodResponse ExecuteString(string str)
        {
            return (SQLiteMethodResponse)Worker.ExecuteQuery(new object[] { str });
        }
        public static List<T> ExecuteString<T>(string str)
        {
            return (List<T>)Worker.ExecuteQuery(new object[] { str }, typeof(T));
        }
        public static IList ExecuteString(string str, Type returnType)
        {
            return (IList)Worker.ExecuteQuery(new object[] { str }, returnType);
        }

        #endregion


        #region Table's list

        [SQLiteName("sqlite_master")]
        public class sqlite_master
        {
            public string type { get; set; }
            public string name { get; set; }
            public string tbl_name { get; set; }
            public string rootpage { get; set; }
            public string sql { get; set; }
        }

        public static List<sqlite_master> Tables => WhereAND("type", Is.Equals, "table").Get<sqlite_master>();

        #endregion


        #region Settings props and Utils methods

        public static string PathToDB { get; set; }

        public static Exception LastQueryException;


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

        #endregion



        public class Node
        {
            public Node() { }
            public Node(Type propertyType)
            {
                PropertyType = propertyType;
            }

            public PropertyInfo Property { get; set; }

            private Type _propertyType;
            public Type PropertyType
            {
                get => _propertyType ?? Property.PropertyType;
                set => _propertyType = value ?? _propertyType;
            }

            public string TableName => PropertyType?.GetCustomAttribute<SQLiteNameAttribute>()?.Name ?? ((PropertyType?.Name ?? "") + "s");

            public string TablePropertyName => Property?.GetCustomAttribute<SQLiteNameAttribute>()?.Name ?? Property?.Name;

            public string TableAnyName => Property == null ? TableName : TablePropertyName;

            public string PropertyHash { get; set; }

            public bool IsEnumerable { get; set; }

            public bool IsTable { get; set; }

            public Node Parent { get; set; }

            public List<Node> Children { get; set; }
        }

        public static string NodeToJson(Node node) => InternalNewtonsoft.Json.JsonConvert.SerializeObject(node);

        public static Node GenerateTree<T>() => GenerateTree(typeof(T));

        public static Node GenerateTree(Type type)
        {
            var node = Worker.FullfillChildrenNodes(new Node(type));

            var json = JsonConvert.SerializeObject(node, Formatting.Indented,
            new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            });

            //   Console.WriteLine(json);

            var strWhere = String.Empty;
            var strJoin = String.Empty;
            var strSelect = String.Empty;

            foreach (var child in node.Children)
            {
                strSelect += Worker.CreateSelectPartOfSqlRequestFromNode(child);
                strJoin += Worker.CreateJoinPartOfSqlRequestFromNode(child);
                strWhere += Worker.CreateWherePartOfSqlRequestFromNode(child);
            }

            strWhere = strWhere.TrimStart();
            strWhere = (strWhere.StartsWith("AND") ? strWhere.Substring(3) : strWhere).Trim();

            var str = "SELECT " + strSelect.Trim().Trim(',') + 
                Environment.NewLine + $" FROM {node.TableName} " + strJoin + 
                (string.IsNullOrWhiteSpace(strWhere) ? string.Empty : Environment.NewLine + " WHERE " + strWhere);

            Console.WriteLine(str);

            return node;
        }

        #region Workerk

        internal class Worker
        {
            #region Building Tree

            internal static Node FullfillChildrenNodes(Node ParentNode, List<sqlite_master> tables = null)
            {
                tables = tables ?? Tables;
                ParentNode.PropertyHash = GetHashOfProperty(ParentNode.Property);

                var needAddChildren = true;
                var node = ParentNode;

                while (node.Parent != null && needAddChildren)
                {
                    needAddChildren = (node = node.Parent).PropertyHash != ParentNode.PropertyHash;
                }

                if (needAddChildren)
                {
                    ParentNode.Children = new List<Node>();

                    var parentNodeProps = GetSQLitePropertiesFromType(ParentNode.PropertyType);

                    foreach (var prop in parentNodeProps)
                    {
                        var enumInfo = DecomposeType(prop);

                        var childNode = new Node()
                        {
                            Property = prop,
                            PropertyType = enumInfo.PropertyType,
                            IsEnumerable = enumInfo.IsEnumerable,
                            IsTable = TypeTableExists(enumInfo.PropertyType, tables),
                            Parent = ParentNode,
                        };

                        childNode = childNode.IsTable ? FullfillChildrenNodes(childNode, tables) : childNode;

                        ParentNode.Children.Add(childNode);
                    }
                }

                return ParentNode;
            }


            static bool TypeTableExists(Type type, List<sqlite_master> tables)
            {
                return tables.Any(t => t.name == (type.GetCustomAttribute<SQLiteNameAttribute>()?.Name ?? type.Name + "s"));
            }

            static (Type PropertyType, bool IsEnumerable) DecomposeType(PropertyInfo prop)
            {
                var isEnumerable = typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string);

                return (isEnumerable ? prop.PropertyType.GetGenericArguments().FirstOrDefault() : prop.PropertyType, isEnumerable);
            }

            static string GetHashOfProperty(PropertyInfo prop)
            {
                return InternalNewtonsoft.Json.JsonConvert.SerializeObject(prop);
            }

            #endregion

            internal static string delimeter = "_";
            internal static int deepJoin = 0;
            internal static int deepWhere = 0;
            internal static int deepSelect = 0;

            internal static string CreateJoinPartOfSqlRequestFromNode(Node node)
            {
                if (node.Children == null)
                    return "";

                deepJoin++;

                var tempSelectString = string.Empty;

                var tempName = string.Empty;
                var tempNode = node;

                while (tempNode != null)
                {
                    tempName = tempNode.TableAnyName + delimeter + tempName;

                    tempNode = tempNode.Parent;
                }

                tempName = tempName.Remove(tempName.Length - delimeter.Length);

                tempSelectString += $"{Environment.NewLine}" +
                     $"{Spaces(deepJoin)}" +
                    // $"{deep}: " +
                    $"JOIN " +
                    //   $"{(node.Property == null ? GetTableNameByType(node.PropertyType) : GetPropertyNameInTable(node.Property))} AS {tempName}";
                    $"{node.TableName} AS {tempName}";

                foreach (var subChild in node.Children ?? new List<Node>())
                {
                    tempSelectString += CreateJoinPartOfSqlRequestFromNode(subChild);
                }

                deepJoin--;

                return tempSelectString;
            }
            
            internal static string CreateWherePartOfSqlRequestFromNode(Node node)
            {
                // and Users_Posts_Author_Comments.Id in (select Id1 from Сonnections where type = "" and Users_Posts_Author.Id = id2)

                // AND
                // CurrentPath.Id
                // IN
                // ( SELECT 
                // ID1 or ID2
                // where type = 
                // (node.PropertyType, node.Parent.PropertyType).Sort().Join(" AND ")
                // AND
                // ParentPath.Id = 
                // ID2 or ID1 )

                if (node.Children == null)
                    return "";

                deepWhere++;

                var tempSelectString = string.Empty;

                var tempName = string.Empty;
                var tempParrentName = string.Empty;
                var tempNode = node;
                var tempParent = node.Parent;
                

                while (tempNode != null)
                {
                    tempName = tempNode.TableAnyName + delimeter + tempName;

                    tempNode = tempNode.Parent;
                }

                var props = GetSQLitePropertiesFromType(node.PropertyType);
                var id = props.FirstOrDefault(prop => prop.Name.ToLower() == "id")?.Name;
                tempName = tempName.Remove(tempName.Length - delimeter.Length) + (id == null ? "" : $".{id}");


                while (tempParent != null)
                {
                    tempParrentName = tempParent.TableAnyName + delimeter + tempParrentName;

                    tempParent = tempParent.Parent;
                }
                
                tempParrentName = tempParrentName.Remove(tempParrentName.Length - delimeter.Length);


                var typesNames = new List<string> { node.PropertyType.Name, node.Parent.PropertyType.Name };
                typesNames.Sort();
                var whereTypeName = $"{typesNames[0]}_AND_{typesNames[1]}";

                var tbl = " FROM Сonnections ";
                var idForSelect = (node.PropertyType.Name == typesNames[0] ? "ID1" : "ID2") + tbl;
                var idForWhere = (node.PropertyType.Name != typesNames[0] ? "ID1" : "ID2");

                tempSelectString += $"{Environment.NewLine}" +
                    $"{Spaces(deepWhere)}" +
              //      $"{deepWhere}: " +
                    $"AND " +
                    $"{tempName} IN " +
                        $"(SELECT {idForSelect} " +
                            $"WHERE TYPE=\"{whereTypeName}\" AND {tempParrentName}.Id = {idForWhere})";

                foreach (var subChild in node.Children ?? new List<Node>())
                {
                    tempSelectString += CreateWherePartOfSqlRequestFromNode(subChild);
                }

                deepWhere--;

                return tempSelectString;
            }
            
            internal static string CreateSelectPartOfSqlRequestFromNode(Node node)
            {
                deepSelect++;

                var tempSelectString = string.Empty;

                if (node.IsTable == false)
                {
                    var tempNode = node;
                    var tempList = new List<String>();

                    while (tempNode != null)
                    {
                        tempList.Insert(0,tempNode.TableAnyName);

                        tempNode = tempNode.Parent;
                    }
                    
                    var afterDot = tempList.LastOrDefault();
                    tempList.RemoveAt(tempList.Count - 1);

                    var left = string.Join(delimeter, tempList.ToArray());
                    
                    //  Post.id as "PostId", Post.Text as "PostText", Comments.Text as "CommentText", CommentAuther.Name as "CommentAutherName"


                    tempSelectString += $"{Environment.NewLine}" +
                         $"{Spaces(deepJoin)}" +
                        // $"{deep}: " +
                        //   $"{(node.Property == null ? GetTableNameByType(node.PropertyType) : GetPropertyNameInTable(node.Property))} AS {tempName}";
                        $"{left}.{afterDot} AS \"{left}{delimeter}{afterDot}\", ";
                }
                
                foreach (var subChild in node.Children ?? new List<Node>())
                {
                    tempSelectString += CreateSelectPartOfSqlRequestFromNode(subChild);
                }

                deepSelect--;

                return tempSelectString;
             //   return deepSelect == 0? tempSelectString.Trim().Trim(',') : tempSelectString;
            }





            internal static string Spaces(int count)
            {
                var str = string.Empty;

                for (int i = 0; i < count - 1; i++)
                    str += "    ";

                return str;
            }



            // Исполняем любой запрос к БД
            internal static object ExecuteQuery(object[] data = null, Type resultType = null, WhereCondition where = null)
            {
                var commandName = GetCurrentMethodName();

                data = Utils.Converters.ConvertArrayWithSingleListToArrayOfItems(data);

                if (commandName.Equals("Truncate") == false)
                {
                    // Проверяем, не подсунули ли нам пустоту вместо данных для работы
                    if ((commandName.Contains("Get") == false && commandName.Contains("Delete") == false) && ((data == null) || (data.Count() < 1)))
                        return NewSQLiteResponse();

                    if ((data ?? new object[0]).Any(d => d == null))
                        return NewSQLiteResponse(new NullReferenceException("Data array contains null object."));
                }

                if (resultType == null && (data ?? new object[0]).Count() == 0)
                    return NewSQLiteResponse(new NullReferenceException($"Command '{commandName}' cannot be called with empty or nullable data array."));

                resultType = resultType ?? data[0].GetType();
                var tableName = '"' + (resultType.GetCustomAttribute<SQLiteNameAttribute>()?.Name ?? resultType.Name + "s") + '"';
                var props = GetSQLitePropertiesFromType(resultType);

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
                            try
                            {
                                using (SQLiteDataReader sqliteDataReader = command.ExecuteReader())
                                {
                                    sqliteDataReader.Read();
                                    return sqliteDataReader.GetInt32(0);
                                }
                            }
                            catch (Exception ex)
                            {
                                LastQueryException = ex;
                                return -1;
                            }

                        #endregion

                        #region Set method implementation

                        case "Set":

                            var tableToInsertPartOfQuery = $"INSERT INTO {tableName}";

                            props.Remove(props.FirstOrDefault(prop => prop.Name.ToLower() == "id"));

                            var strWithFields = $" ({string.Join(",", props.Select(prop => GetPropertyNameInTable(prop)).ToArray()).TrimEnd(',')}) values ";
                            var strWithValues = string.Empty;
                            foreach (var obj in Utils.Converters.ConvertArrayWithSingleListToArrayOfItems(data))
                            {
                                strWithValues += '(';
                                foreach (var prop in props)
                                {
                                    var value = prop.GetValue(obj, null);
                                    value = (value != null && value is string) ? (value as string).Replace("\'", "\'\'") : value;

                                    if (prop.PropertyType.IsValueType == false && prop.PropertyType != typeof(string))
                                        value = Utils.Converters.ObjectToString(value);

                                    strWithValues += value == null ? "NULL," : $"'{value}',";
                                }

                                strWithValues = strWithValues.TrimEnd(',') + "),";
                            }

                            command.CommandText = tableToInsertPartOfQuery + strWithFields + strWithValues.TrimEnd(',') + ";";

                            return ExecuteNonQueryWithResponse(command);

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
                                foreach (var obj in Utils.Converters.ConvertArrayWithSingleListToArrayOfItems(data))
                                {
                                    var value = prop.GetValue(obj, null);
                                    value = value != null && value is string ? (value as string).Replace("\'", "\'\'") : value;

                                    var id = resultType.GetProperty("id", bindingFlags | BindingFlags.IgnoreCase).GetValue(obj, null);

                                    if (prop.PropertyType.IsValueType == false && prop.PropertyType != typeof(string))
                                        value = Utils.Converters.ObjectToString(value);

                                    groupString += $"WHEN {id} THEN " + (value == null ? "NULL " : $"'{value}' ");

                                    idsEnum += id + ",";
                                }

                                groupString += "END,";
                            }

                            groupString = $"{groupString.TrimEnd(',')} WHERE id IN ({idsEnum.TrimEnd(',')});";

                            command.CommandText = tableToInsertPartOfQueryForUpdateMethod + groupString;

                            return ExecuteNonQueryWithResponse(command);

                        #endregion

                        #region Delete method implementation

                        case "Delete":

                            if (where == null)
                            {
                                if (data.Count() == 0)
                                {
                                    return NewSQLiteResponse();
                                }

                                var idCaseSensitiveProperty = props.FirstOrDefault(pr => pr.Name.ToLower() == "id");
                                if (idCaseSensitiveProperty == null)
                                {
                                    return NewSQLiteResponse(new Exception(
                                        "Some objects to delete have no 'id' property. " +
                                        "Or, maybe, the first object passed to the SQLite.Delete(...) is 'id', " +
                                        "but we cannot recognize table name, if the first object's type " +
                                        "is not the required model's type (or any IEnumerable<ModelType>)."));
                                }

                                var ids = new List<object>();

                                foreach (var dat in data)
                                {
                                    ids.Add(dat.GetType().IsValueType ? dat : idCaseSensitiveProperty.GetValue(dat));
                                }

                                where = new WhereCondition().Where(idCaseSensitiveProperty.Name, Is.In, ids);
                            }

                            command.CommandText = $"DELETE FROM {tableName} {CreateWherePartOfSqlRequest(where, props)}";

                            return ExecuteNonQueryWithResponse(command);

                        #endregion

                        #region Truncate method implementation

                        case "Truncate":
                            //   TRUNCATE[TABLE] tbl_name
                            // command.CommandText = String.Format("SELECT * FROM {0};", resultType.Name + 's');
                            command.CommandText = string.Format($"DELETE FROM {tableName}");

                            return ExecuteNonQueryWithResponse(command);

                        #endregion

                        #region ExecuteQuery method implementation

                        case "ExecuteString":

                            foreach (var chr in data)
                                command.CommandText = (command.CommandText ?? String.Empty) + chr.ToString();

                            if (resultType == typeof(string))
                                return ExecuteNonQueryWithResponse(command);
                            else
                                return GetResultsFromDB(command, resultType, GetSQLitePropertiesFromType(resultType));
                            #endregion
                    }

                    return NewSQLiteResponse(new ArgumentException($"Command '{commandName}' not found."));
                }
            }

            static SQLiteMethodResponse ExecuteNonQueryWithResponse(SQLiteCommand command)
            {
                try
                {
                    return new SQLiteMethodResponse() { RowsCountAffected = command.ExecuteNonQuery() };
                }
                catch (Exception ex)
                {
                    LastQueryException = ex;
                    return new SQLiteMethodResponse() { Exception = ex };
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

                    condition.Parameters = Utils.Converters.ConvertArrayWithSingleListToArrayOfItems(condition.Parameters);
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
                                foreach (var arrItem in condition.Parameters)
                                    commandText += $"{quote}{arrItem}{quote},";
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

            static List<PropertyInfo> GetSQLitePropertiesFromType(Type resultType)
            {
                return resultType.GetProperties(bindingFlags).Where(prop => prop.GetCustomAttribute<SQLiteIgnoreAttribute>() == null).ToList();
            }

            static IList GetResultsFromDB(SQLiteCommand command, Type resultType, List<PropertyInfo> props = null)
            {
                try
                {
                    using (SQLiteDataReader sqliteDataReader = command.ExecuteReader())
                    {
                        // Создаём лист типов, которые надо вернуть
                        var result = Utils.Internal.CreateList(resultType);
                        // Пробегаемся по всем строкам результата
                        while (sqliteDataReader.Read())
                        {
                            // Для каждой строки создаём свой объект нужного нам типа
                            var resObj = Activator.CreateInstance(resultType);

                            foreach (var prop in props ?? GetSQLitePropertiesFromType(resultType))
                            {
                                //Заносим значения ячеек в наш новый объект
                                try
                                {
                                    var propertyNameInTable = GetPropertyNameInTable(prop);
                                    var objectFromSQLiteDataReader = sqliteDataReader[propertyNameInTable];

                                    var objectToSetToProperty = (prop.PropertyType.IsValueType || prop.PropertyType == typeof(string)) ?
                                        Convert.ChangeType(objectFromSQLiteDataReader, prop.PropertyType) :
                                        Utils.Converters.StringToObject((string)objectFromSQLiteDataReader, prop.PropertyType);

                                    prop.SetValue(resObj, objectFromSQLiteDataReader != DBNull.Value ? objectToSetToProperty : null, null);
                                }
                                catch (Exception ex)
                                {
                                    prop.SetValue(resObj, Utils.Internal.CreateDefaultObject(prop.PropertyType), null);
                                }
                            }

                            // И, наконец, добавляем полученный объект в лист с результатами
                            result.Add(resObj);
                        }

                        return result;
                    }
                }
                catch (Exception ex)
                {
                    LastQueryException = ex;
                    return null;
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

            static string GetPropertyNameInTable(PropertyInfo prop) => prop.GetCustomAttribute<SQLiteNameAttribute>()?.Name ?? prop.Name;

            static SQLiteMethodResponse NewSQLiteResponse(Exception ex = null)
            {
                if (ex == null)
                    return new SQLiteMethodResponse() { RowsCountAffected = 0 };

                LastQueryException = ex;

                return new SQLiteMethodResponse() { Exception = ex };
            }

            static SQLiteConnection GetConnection()
            {
                //if (Utils.Internal.CurrentPlatformIsWindows)
                //{
                //    if (Directory.Exists("x64") == false)
                //    {
                //        Directory.CreateDirectory("x64");
                //    }

                //    if (Directory.Exists("x86") == false)
                //    {
                //        Directory.CreateDirectory("x86");
                //    }

                //    //if (File.Exists("System.Data.SQLite.dll") == false)
                //    //{
                //    //    Utils.Utils.Internal.WriteResourceToFile("PN.SQLiteDlls.System.Data.SQLite.dll", "System.Data.SQLite.dll");
                //    //}

                //    if (File.Exists("x64/SQLite.Interop.dll") == false)
                //    {
                //        Utils.Internal.WriteResourceToFile("PN.SQLiteDlls.x64.SQLite.Interop.dll", "x64/SQLite.Interop.dll");
                //    }

                //    if (File.Exists("x86/SQLite.Interop.dll") == false)
                //    {
                //        Utils.Internal.WriteResourceToFile("PN.SQLiteDlls.x86.SQLite.Interop.dll", "x86/SQLite.Interop.dll");
                //    }
                //}

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
                    Utils.Debug.Log(ex, true);
#endif
                    return false;
                }
            }
        }

        #endregion


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
        /// All properties with such attributes will be ignored in SQL queries
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

        public List<T> Get<T>() => (List<T>)SQLite.Worker.ExecuteQuery(null, typeof(T), this);

        public IList Get(Type type) => (IList)SQLite.Worker.ExecuteQuery(null, type, this);

        public int GetCount<T>() => (int)SQLite.Worker.ExecuteQuery(null, typeof(T), this);

        public SQLiteMethodResponse Delete<T>(params object[] data) => (SQLiteMethodResponse)SQLite.Worker.ExecuteQuery(data, typeof(T), this);

        public SQLiteMethodResponse Delete(Type type, params object[] data) => (SQLiteMethodResponse)SQLite.Worker.ExecuteQuery(data, type, this);
    }

    public class SQLiteMethodResponse
    {
        public int RowsCountAffected { get; set; } = 0;
        public Exception Exception { get; set; } = null;
    }
}