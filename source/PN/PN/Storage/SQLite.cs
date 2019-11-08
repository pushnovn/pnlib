using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Data.SQLite;
using System.Diagnostics;
using System.Collections;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static PN.Storage.WhereCondition;

namespace PN.Storage
{
    public class SQLite
    {
        #region Interface methods

        #region Get

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static List<T> Get<T>(Expression<Func<T, dynamic>> expression)
        {
            return (List<T>)Worker.ExecuteQuery(null, typeof(T), Where<T>(expression));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static IList Get(Type type)
        {
            return (IList)Worker.ExecuteQuery(null, type);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static List<T> Get<T>()
        {
            return (List<T>)Worker.ExecuteQuery(null, typeof(T));
        }

        #endregion

        #region GetCount

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int GetCount<T>()
        {
            return (int)Worker.ExecuteQuery(null, typeof(T));
        }

        #endregion

        #region Set

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static SQLiteMethodResponse Set(params object[] data)
        {
            return (SQLiteMethodResponse)Worker.ExecuteQuery(data);
        }

        #endregion

        #region Update

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static SQLiteMethodResponse Update(params object[] data)
        {
            return (SQLiteMethodResponse)Worker.ExecuteQuery(data);
        }

        #endregion

        #region SetOrUpdate

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static SQLiteMethodResponse SetOrUpdate(params object[] data)
        {
            return (SQLiteMethodResponse)Worker.ExecuteQuery(data);
        }

        #endregion

        #region Delete

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static SQLiteMethodResponse Delete(params object[] data)
        {
            return (SQLiteMethodResponse)Worker.ExecuteQuery(data);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static SQLiteMethodResponse Delete<T>(params object[] data)
        {
            return (SQLiteMethodResponse)Worker.ExecuteQuery(data, typeof(T));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static SQLiteMethodResponse Delete<T>(Expression<Func<T, dynamic>> expression)
        {
            return (SQLiteMethodResponse)Worker.ExecuteQuery(null, typeof(T), Where<T>(expression));
        }

        #endregion

        #region Truncate

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static SQLiteMethodResponse Truncate<T>(params object[] data)
        {
            return (SQLiteMethodResponse)Worker.ExecuteQuery(data, typeof(T));
        }

        #endregion

        #region ExecuteString

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static SQLiteMethodResponse ExecuteString(string str)
        {
            return (SQLiteMethodResponse)Worker.ExecuteQuery(new object[] { str });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static List<T> ExecuteString<T>(string str)
        {
            return (List<T>)Worker.ExecuteQuery(new object[] { str }, typeof(T));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static IList ExecuteString(string str, Type returnType)
        {
            return (IList)Worker.ExecuteQuery(new object[] { str }, returnType);
        }

        #endregion

        #endregion


        #region Global Properties

        public static string PathToDB { get; set; }

        public static BooleanStorageType BooleanType { get; set; } = BooleanStorageType.Integer;
        public enum BooleanStorageType { Integer, String }

        public static Exception LastQueryException;
        public static string LastQueryString;

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

        public static List<sqlite_master> Tables => Where("type", Is.Equals, "table").Get<sqlite_master>();

        #endregion

        #endregion


        #region Where
        public static WhereCondition Where<T>(Expression<Func<T, dynamic>> expression)
        {
            return AddConditionAndReturnSelf(new Condition(ConditionTypes.WHERE, expression));
        }

        public static WhereCondition Where<T>(Expression<Func<T, object>> member, Is operation, params object[] parameters)
        {
            return AddConditionAndReturnSelf(new Condition(ConditionTypes.WHERE, Utils.Converters.ExpressionToString(member), operation, parameters));
        }

        public static WhereCondition Where(string propertyName, Is operation, params object[] parameters)
        {
            return AddConditionAndReturnSelf(new Condition(ConditionTypes.WHERE, propertyName, operation, parameters));
        }

        private static WhereCondition AddConditionAndReturnSelf(Condition condition)
        {
            return new WhereCondition(condition);
        }

        public static string Test<T>(Expression<Func<T, dynamic>> expression)
        {
            return ExpressionToSQLTranslator.Translate(expression);
        }
        #endregion


        #region Worker

        internal class Worker
        {
            // Исполняем любой запрос к БД
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static object ExecuteQuery(object[] data = null, Type resultType = null, WhereCondition where = null)
            {
                var commandName = GetCurrentMethodName();

                data = Utils.Converters.ConvertArrayWithSingleListToArrayOfItems(data);

                if (commandName.Equals("Truncate") == false)
                {
                    data = data.Where(d => d != null).ToArray();

                    var emptyDataCommands = new List<string>() { nameof(Get), nameof(GetCount), nameof(Delete) };

                    if (emptyDataCommands.Any(s => s == commandName) == false && data.Count() == 0)
                        return NewSQLiteResponse();
                }

                resultType = resultType ?? data.FirstOrDefault()?.GetType();
                if (resultType == null)
                    return NewSQLiteResponse(new Exception($"Command '{commandName}' cannot be called with empty or null data array."));

                var tableName = '"' + (resultType.GetCustomAttribute<SQLiteNameAttribute>()?.Name ?? resultType.Name + "s") + '"';
                var props = GetSQLitePropertiesFromType(resultType);

                // Открываем соединение к БД (после запроса автоматом закроем его)
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var command = conn.CreateCommand();

                    switch (commandName)
                    {
                        #region Get/GetCount

                        case nameof(Get):
                            command.CommandText = $"SELECT * FROM {tableName} {CreateWherePartOfSqlRequest(where, props)}";
                            return GetResultsFromDB(command, resultType);

                        case nameof(GetCount):
                            command.CommandText = $"SELECT COUNT(*) FROM {tableName} {CreateWherePartOfSqlRequest(where, props, true)}";
                            try
                            {
                                using (var sqliteDataReader = command.ExecuteReader())
                                {
                                    sqliteDataReader.Read();
                                    return sqliteDataReader.GetInt32(0);
                                }
                            }
                            catch (Exception ex)
                            {
                                LastQueryException = ex;
                                LastQueryString = command.CommandText;
                                return -1;
                            }

                        #endregion

                        #region Set

                        case nameof(Set):
                        case nameof(SetOrUpdate):

                            var tableToInsertPartOfQuery = $"INSERT OR REPLACE INTO {tableName}";

                            if (commandName == nameof(Set))
                                props.Remove(props.FirstOrDefault(prop => prop.Name.ToLower() == "id"));

                            var strWithFields = $" ({string.Join(",", props.Select(prop => GetPropertyNameInTable(prop)).ToArray()).TrimEnd(',')}) values ";
                            var strWithValues = string.Empty;
                            foreach (var obj in Utils.Converters.ConvertArrayWithSingleListToArrayOfItems(data))
                            {
                                strWithValues += '(';
                                foreach (var prop in props)
                                {
                                    var value = prop.GetValue(obj, null);

                                    if (prop.Name.ToLower() == "id")
                                    {
                                        value = (commandName == nameof(Set) || value.Equals(0)) ? null : value;
                                    }

                                    if (value is string valstr)
                                    {
                                        value = valstr.Replace("\'", "\'\'");
                                    }
                                    
                                    strWithValues += $"{value.ToSQLiteString()},";
                                }

                                strWithValues = strWithValues.TrimEnd(',') + "),";
                            }

                            command.CommandText = tableToInsertPartOfQuery + strWithFields + strWithValues.TrimEnd(',') + ";";

                            return ExecuteNonQueryWithResponse(command);

                        #endregion

                        #region Update

                        case nameof(Update):

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
                                    
                                    groupString += $"WHEN {id} THEN {value.ToSQLiteString()} ";

                                    idsEnum += id + ",";
                                }

                                groupString += "END,                                                      ";
                            }

                            groupString = $"{groupString.Remove(groupString.LastIndexOf(","), 1)} WHERE id IN ({idsEnum.TrimEnd(',')});";

                            command.CommandText = tableToInsertPartOfQueryForUpdateMethod + groupString;

                            return ExecuteNonQueryWithResponse(command);

                        #endregion

                        #region Delete

                        case nameof(Delete):

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

                                where = Where(idCaseSensitiveProperty.Name, Is.In, ids);
                            }

                            command.CommandText = $"DELETE FROM {tableName} {CreateWherePartOfSqlRequest(where, props, true)}";

                            return ExecuteNonQueryWithResponse(command);

                        #endregion

                        #region Truncate

                        case nameof(Truncate):
                            //   TRUNCATE[TABLE] tbl_name
                            // command.CommandText = String.Format("SELECT * FROM {0};", resultType.Name + 's');
                            command.CommandText = string.Format($"DELETE FROM {tableName}");

                            return ExecuteNonQueryWithResponse(command);

                        #endregion

                        #region ExecuteQuery

                        case nameof(ExecuteString):

                            foreach (var chr in data)
                                command.CommandText = (command.CommandText ?? String.Empty) + chr.ToString();

                            if (resultType == typeof(string))
                                return ExecuteNonQueryWithResponse(command);
                            else
                                return GetResultsFromDB(command, resultType);

                            #endregion
                    }

                    return NewSQLiteResponse(new ArgumentException($"Command '{commandName}' not found."));
                }
            }

            static SQLiteMethodResponse ExecuteNonQueryWithResponse(SQLiteCommand command)
            {
                try
                {
                    return new SQLiteMethodResponse() { RowsCountAffected = command.ExecuteNonQuery(), SqlQuery = command.CommandText };
                }
                catch (Exception ex)
                {
                    return new SQLiteMethodResponse() { Exception = LastQueryException = ex, SqlQuery = command.CommandText };
                }
            }

            private static string CreateWherePartOfSqlRequest(WhereCondition where, List<PropertyInfo> props, bool needSkipLimitPart = false)
            {
                if (where == null)
                    return ";";

                var commandText = string.Empty;

                foreach (var condition in where.Conditions)
                {
                    if (condition.Expression != null)
                    {
                        commandText += $"{condition.ConditionType} {ExpressionToSQLTranslator.Translate(condition.Expression)}";
                        continue;
                    }

                    if (condition.Parameters.Length < 1 || condition.Parameters.Any(p => p == null))
                        continue;

                    var subCommandText = string.Empty;

                    if (condition.Operation != Is.Reversed && condition.Operation != Is.LimitedBy && condition.Operation != Is.Offset)
                        subCommandText += $" {condition.ConditionType} (";

                    condition.Parameters = Utils.Converters.ConvertArrayWithSingleListToArrayOfItems(condition.Parameters);

                    switch (condition.Operation)
                    {
                        case Is.BiggerThan:
                        case Is.BiggerThanOrEquals:
                        case Is.LessThan:
                        case Is.LessThanOrEquals:
                        case Is.Equals:
                        case Is.NotEquals:
                            subCommandText += $"{condition.PropertyName} " +
                                $"{SimpleOperators[condition.Operation]} " +
                                $"{condition.Parameters[0].ToSQLiteString()}";
                            break;

                        case Is.Like:
                            subCommandText += $"{condition.PropertyName} LIKE {condition.Parameters[0].ToSQLiteString(true)}";
                            break;

                        case Is.Between:
                            if (condition.Parameters.Length > 1)
                                subCommandText += $"{condition.PropertyName} " +
                                    $"BETWEEN {condition.Parameters[0].ToSQLiteString()} " +
                                    $"AND {condition.Parameters[1].ToSQLiteString()}";
                            break;

                        case Is.In:
                            subCommandText += $"{condition.PropertyName} IN (";
                            foreach (var arrItem in condition.Parameters)
                            {
                                subCommandText += $"{arrItem.ToSQLiteString()},";
                            }
                            subCommandText = subCommandText.TrimEnd(',') + ")";
                            break;

                        case Is.Contains:
                            if (string.IsNullOrEmpty(condition.PropertyName))
                                foreach (var prop in props)
                                    foreach (var subStr in condition.Parameters)
                                        subCommandText += $" {GetPropertyNameInTable(prop)} LIKE {subStr.ToSQLiteString(true)} OR";
                            else
                                foreach (var arrItem in (condition.Parameters[0] is IList)
                                        ? condition.Parameters[0] as IList
                                        : condition.Parameters)
                                    subCommandText += $" {condition.PropertyName} LIKE {arrItem.ToSQLiteString(true)} OR";

                            if (subCommandText.Trim().EndsWith("OR"))
                                subCommandText = subCommandText.Trim().Remove(subCommandText.Length - 3);
                            break;

                        case Is.ContainsAnythingFrom:
                            foreach (var prop in props)
                                foreach (var subStr in condition.Parameters)
                                    subCommandText += $" {GetPropertyNameInTable(prop)} LIKE {subStr.ToSQLiteString(true)} OR";

                            if (subCommandText.Trim().EndsWith("OR"))
                                subCommandText = subCommandText.Trim().Remove(subCommandText.Length - 3);
                            break;

                        case Is.LimitedBy:
                            where.Limit = (int)condition.Parameters[0];
                            break;

                        case Is.Reversed:
                            where.Reverse = (bool)condition.Parameters[0];
                            break;

                        case Is.Offset:
                            where.Offset = (int)condition.Parameters[0];
                            break;
                    }

                    if (condition.Operation != Is.Reversed && condition.Operation != Is.LimitedBy && condition.Operation != Is.Offset)
                        subCommandText += ")";

                    commandText += subCommandText;
                }

                if (needSkipLimitPart)
                    return commandText;

                commandText += where.Reverse ? " ORDER BY id DESC " : " ";

                commandText += $" LIMIT {where.Offset}, {where.Limit};";

                return commandText;
            }

            private static BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic |
                                                       BindingFlags.Static | BindingFlags.Instance |
                                                       BindingFlags.DeclaredOnly;

            static List<PropertyInfo> GetSQLitePropertiesFromType(Type resultType)
            {
                return resultType.GetProperties(bindingFlags).Where(prop => prop.GetCustomAttribute<SQLiteIgnoreAttribute>() == null).ToList();
            }

            static IList GetResultsFromDB(SQLiteCommand command, Type resultType)
            {
                try
                {
                    using (var sqliteDataReader = command.ExecuteReader())
                    {
                        var resultList = Utils.Internal.CreateList(resultType);

                        while (sqliteDataReader.Read())
                        {
                            var resObj = Activator.CreateInstance(resultType);
                            var props = GetSQLitePropertiesFromType(resultType);
                            
                            foreach (var prop in props)
                            {
                                var propertyNameInTable = GetPropertyNameInTable(prop);
                                var objectFromSQLiteDataReader = sqliteDataReader[propertyNameInTable];
                                object objectToSetToProperty;

                                try
                                {
                                    if (objectFromSQLiteDataReader == DBNull.Value)
                                    {
                                        objectToSetToProperty = Utils.Internal.CreateDefaultObject(prop.PropertyType);
                                    }
                                    else if (prop.PropertyType.IsEnum)
                                    {
                                        objectToSetToProperty = Convert.ChangeType(
                                            Enum.Parse(prop.PropertyType, objectFromSQLiteDataReader.ToString(), true), prop.PropertyType);
                                    }
                                    else if (prop.PropertyType == typeof(bool) && SQLite.BooleanType == BooleanStorageType.Integer)
                                    {
                                        objectToSetToProperty = (long)objectFromSQLiteDataReader == 1;
                                    }
                                    else if (prop.PropertyType == typeof(DateTime))
                                    {
                                        if (DateTime.TryParse(objectFromSQLiteDataReader.ToString(), out DateTime res))
                                        {
                                            objectToSetToProperty = res;
                                        }
                                        else
                                        {
                                            throw new ArgumentException($"Filed is DateTime, but problems with '{objectFromSQLiteDataReader}'");
                                        }
                                    }
                                    else if (prop.PropertyType == typeof(DateTimeOffset))
                                    {
                                        if (DateTimeOffset.TryParse(objectFromSQLiteDataReader.ToString(), out DateTimeOffset res))
                                        {
                                            objectToSetToProperty = res;
                                        }
                                        else
                                        {
                                            throw new ArgumentException($"Filed is DateTimeOffset, but problems with '{objectFromSQLiteDataReader}'");
                                        }
                                    }
                                    else if (prop.PropertyType.IsValueType || prop.PropertyType == typeof(string))
                                    {
                                        objectToSetToProperty = Convert.ChangeType(objectFromSQLiteDataReader, prop.PropertyType);
                                    }
                                    else
                                    {
                                        objectToSetToProperty = Utils.Converters.StringToObject((string)objectFromSQLiteDataReader, prop.PropertyType);
                                    }

                                    prop.SetValue(resObj, objectToSetToProperty, null);
                                }
                                catch (Exception ex)
                                {
#if DEBUG
                                    throw;
#endif

                                    prop.SetValue(resObj, Utils.Internal.CreateDefaultObject(prop.PropertyType), null);
                                }
                            }

                            resultList.Add(resObj);
                        }

                        return resultList;
                    }
                }
                catch (Exception ex)
                {
                    LastQueryException = ex;
                    LastQueryString = command.CommandText;
                    return null;
                }
            }

            // Get the type of calling method
            // It's may be Get | Set | Update | Delete
            private static string GetCurrentMethodName()
            {
                var st = new StackTrace();

                // frame 1 = ExecuteQuery
                // frame 2 = Get | Set | Update | Delete
                var sf = st.GetFrame(2);
                return sf.GetMethod().Name;
            }

            internal static string GetPropertyNameInTable(PropertyInfo prop) => prop.GetCustomAttribute<SQLiteNameAttribute>()?.Name ?? prop.Name;

            static SQLiteMethodResponse NewSQLiteResponse(Exception ex = null, string sqlQuery = "")
            {
                if (ex == null)
                    return new SQLiteMethodResponse() { RowsCountAffected = 0, SqlQuery = sqlQuery };

                LastQueryException = ex;
                LastQueryString = sqlQuery;

                return new SQLiteMethodResponse() { Exception = ex, SqlQuery = sqlQuery };
            }

            static SQLiteConnection GetConnection()
            {
                var path = Path.GetFullPath(PathToDB ?? throw new ArgumentException("Path to DB (SQLite) is not set!"));

                return new SQLiteConnection($"Data Source={path};Version=3;");
            }

            static bool CheckConnection()
            {
                try
                {
                    using (var conn = GetConnection())
                    {
                        conn.Open();
                        using (var command = new SQLiteCommand("PRAGMA schema_version;", conn))
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


        #region Expression To SQL

        internal class ExpressionToSQLTranslator : ExpressionVisitor
        {
            private StringBuilder sb;

            public int? Skip { get; private set; } = null;

            public int? Take { get; private set; } = null;

            public string OrderBy { get; private set; } = string.Empty;

            public string WhereClause { get; private set; } = string.Empty;

            public ExpressionToSQLTranslator()
            {
                sb = new StringBuilder();
            }

            public static string Translate(Expression expression)
            {
                var qrt = new ExpressionToSQLTranslator();
                qrt.Visit(expression);
                return qrt.sb.ToString();
            }

            private static Expression StripQuotes(Expression e)
            {
                while (e.NodeType == ExpressionType.Quote)
                {
                    e = ((UnaryExpression)e).Operand;
                }
                return e;
            }

            protected override Expression VisitMethodCall(MethodCallExpression m)
            {
                if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Where")
                {
                    Visit(m.Arguments[0]);
                    var lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                    Visit(lambda.Body);
                    return m;
                }
                else if (m.Method.Name == "Take")
                {
                    if (ParseTakeExpression(m))
                    {
                        var nextExpression = m.Arguments[0];
                        return Visit(nextExpression);
                    }
                }
                else if (m.Method.Name == "Skip")
                {
                    if (ParseSkipExpression(m))
                    {
                        var nextExpression = m.Arguments[0];
                        return Visit(nextExpression);
                    }
                }
                else if (m.Method.Name == "OrderBy")
                {
                    if (ParseOrderByExpression(m, "ASC"))
                    {
                        var nextExpression = m.Arguments[0];
                        return Visit(nextExpression);
                    }
                }
                else if (m.Method.Name == "OrderByDescending")
                {
                    if (ParseOrderByExpression(m, "DESC"))
                    {
                        var nextExpression = m.Arguments[0];
                        return Visit(nextExpression);
                    }
                }

                throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));
            }

            protected override Expression VisitUnary(UnaryExpression u)
            {
                switch (u.NodeType)
                {
                    case ExpressionType.Not:
                        sb.Append(" NOT ");
                        Visit(u.Operand);
                        break;
                    case ExpressionType.Convert:
                        Visit(u.Operand);
                        break;
                    default:
                        throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
                }
                return u;
            }

            protected override Expression VisitBinary(BinaryExpression b)
            {
                if (CheckExpressionOnNull(b.Left) || CheckExpressionOnNull(b.Right))
                {
                    sb.Append("(1=1)");
                    return b;
                }

                sb.Append("(");
                Visit(b.Left);

                sb.Append(ExpressionNodeTypeToString(b));

                Visit(b.Right);
                sb.Append(")");
                return b;
            }

            #region VisitBinary Help methods

            private string ExpressionNodeTypeToString(BinaryExpression b)
            {
                switch (b.NodeType)
                {
                    case ExpressionType.And:
                    case ExpressionType.AndAlso:
                        return (" AND ");

                    case ExpressionType.Or:
                    case ExpressionType.OrElse:
                        return (" OR ");

                    case ExpressionType.Equal:
                        return ($" {(IsNullConstant(b.Right) ? "IS" : "=")} ");

                    case ExpressionType.NotEqual:
                        return ($" {(IsNullConstant(b.Right) ? "IS NOT" : "<>")} ");

                    case ExpressionType.LessThan:
                        return (" < ");

                    case ExpressionType.LessThanOrEqual:
                        return (" <= ");

                    case ExpressionType.GreaterThan:
                        return (" > ");

                    case ExpressionType.GreaterThanOrEqual:
                        return (" >= ");

                    default:
                        throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", b.NodeType));
                }
            }

            private bool CheckExpressionOnNull(Expression expression)
            {
                if (expression is MemberExpression memberExpression)
                {
                    if (memberExpression.Expression.NodeType == ExpressionType.MemberAccess ||
                        memberExpression.Expression.NodeType == ExpressionType.Constant)
                    {
                        if (GetMemberExpressionValue(memberExpression) == null)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            #endregion

            protected override Expression VisitConstant(ConstantExpression c)
            {
                var q = c.Value as IQueryable;

                if (q == null && c.Value == null)
                {
                    sb.Append("NULL");
                }
                else if (q == null)
                {
                    switch (Type.GetTypeCode(c.Value.GetType()))
                    {
                        case TypeCode.Boolean:
                            sb.Append($"'{(bool)c.Value}'");
                            break;

                        case TypeCode.DateTime:
                        case TypeCode.String:
                            sb.Append($"'{c.Value}'");
                            break;

                        case TypeCode.Object:
                            throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", c.Value));

                        default:
                            sb.Append(c.Value.ToSQLiteString());
                            break;
                    }
                }

                return c;
            }

            protected override Expression VisitMember(MemberExpression m)
            {
                if (m.Expression == null)
                    throw new ArgumentException($"Part of the Expression ({m.Member?.Name ?? "unknown name"}) is null!");

                if (m.Expression.NodeType == ExpressionType.Parameter)
                {
                    Console.WriteLine($"                                       1: {m.Member.Name} => {GetMemberInfoNameInTable(m.Member)}");
                    sb.Append("(");
                    sb.Append(GetMemberInfoNameInTable(m.Member));
                    if (m.Member.MemberType == MemberTypes.Property && (m.Member as PropertyInfo)?.PropertyType == typeof(bool))
                        sb.Append($" = {true.ToSQLiteString()}");
                    sb.Append(")");
                    return m;
                }
                else if (m.Expression.NodeType == ExpressionType.Constant || m.Expression.NodeType == ExpressionType.MemberAccess)
                {
                    var obj = GetMemberExpressionValue(m);
                    sb.Append(obj);
                    return m;
                }

                throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
            }

            protected bool IsNullConstant(Expression exp)
            {
                return (exp.NodeType == ExpressionType.Constant && ((ConstantExpression)exp).Value == null);
            }

            private bool ParseOrderByExpression(MethodCallExpression expression, string order)
            {
                var unary = (UnaryExpression)expression.Arguments[1];
                var lambdaExpression = (LambdaExpression)unary.Operand;

                lambdaExpression = (LambdaExpression)Evaluator.PartialEval(lambdaExpression);

                var body = lambdaExpression.Body as MemberExpression;
                if (body != null)
                {
                    if (string.IsNullOrEmpty(OrderBy))
                    {
                        OrderBy = $"{GetMemberInfoNameInTable(body.Member)} {order}";
                    }
                    else
                    {
                        OrderBy = $"{OrderBy}, {GetMemberInfoNameInTable(body.Member)} {order}";
                    }

                    return true;
                }

                return false;
            }

            private bool ParseTakeExpression(MethodCallExpression expression)
            {
                var sizeExpression = (ConstantExpression)expression.Arguments[1];

                int size;
                if (int.TryParse(sizeExpression.Value.ToString(), out size))
                {
                    Take = size;
                    return true;
                }

                return false;
            }

            private bool ParseSkipExpression(MethodCallExpression expression)
            {
                var sizeExpression = (ConstantExpression)expression.Arguments[1];

                int size;
                if (int.TryParse(sizeExpression.Value.ToString(), out size))
                {
                    Skip = size;
                    return true;
                }

                return false;
            }

            private object GetMemberExpressionValue(MemberExpression member)
            {
                try
                {
                    var objectMember = Expression.Convert(member, typeof(object));

                    var getterLambda = Expression.Lambda<Func<object>>(objectMember);

                    var getter = getterLambda.Compile();

                    return getter();
                }
                catch { return null; }
            }

            string GetMemberInfoNameInTable(MemberInfo memberInfo) => memberInfo.GetCustomAttribute<SQLiteNameAttribute>()?.Name ?? memberInfo.Name;

            #region EF part

            /// <summary>
            /// Enables the partial evaluation of queries.
            /// </summary>
            /// <remarks>
            /// From http://msdn.microsoft.com/en-us/library/bb546158.aspx
            /// Copyright notice http://msdn.microsoft.com/en-gb/cc300389.aspx#O
            /// </remarks>
            internal static class Evaluator
            {
                /// <summary>
                /// Performs evaluation and replacement of independent sub-trees
                /// </summary>
                /// <param name="expression">The root of the expression tree.</param>
                /// <param name="fnCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
                /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
                public static Expression PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated)
                {
                    return new SubtreeEvaluator(new Nominator(fnCanBeEvaluated).Nominate(expression)).Eval(expression);
                }

                /// <summary>
                /// Performs evaluation and replacement of independent sub-trees
                /// </summary>
                /// <param name="expression">The root of the expression tree.</param>
                /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
                public static Expression PartialEval(Expression expression)
                {
                    return PartialEval(expression, Evaluator.CanBeEvaluatedLocally);
                }

                private static bool CanBeEvaluatedLocally(Expression expression)
                {
                    return expression.NodeType != ExpressionType.Parameter;
                }

                /// <summary>
                /// Evaluates and replaces sub-trees when first candidate is reached (top-down)
                /// </summary>
                class SubtreeEvaluator : ExpressionVisitor
                {
                    HashSet<Expression> candidates;

                    internal SubtreeEvaluator(HashSet<Expression> candidates)
                    {
                        this.candidates = candidates;
                    }

                    internal Expression Eval(Expression exp)
                    {
                        return this.Visit(exp);
                    }

                    public override Expression Visit(Expression exp)
                    {
                        if (exp == null)
                        {
                            return null;
                        }
                        if (this.candidates.Contains(exp))
                        {
                            return this.Evaluate(exp);
                        }
                        return base.Visit(exp);
                    }

                    private Expression Evaluate(Expression e)
                    {
                        if (e.NodeType == ExpressionType.Constant)
                        {
                            return e;
                        }
                        var lambda = Expression.Lambda(e);
                        var fn = lambda.Compile();
                        return Expression.Constant(fn.DynamicInvoke(null), e.Type);
                    }

                    protected override Expression VisitMemberInit(MemberInitExpression node)
                    {
                        if (node.NewExpression.NodeType == ExpressionType.New)
                            return node;

                        return base.VisitMemberInit(node);
                    }
                }

                /// <summary>
                /// Performs bottom-up analysis to determine which nodes can possibly
                /// be part of an evaluated sub-tree.
                /// </summary>
                class Nominator : ExpressionVisitor
                {
                    Func<Expression, bool> fnCanBeEvaluated;
                    HashSet<Expression> candidates;
                    bool cannotBeEvaluated;

                    internal Nominator(Func<Expression, bool> fnCanBeEvaluated)
                    {
                        this.fnCanBeEvaluated = fnCanBeEvaluated;
                    }

                    internal HashSet<Expression> Nominate(Expression expression)
                    {
                        this.candidates = new HashSet<Expression>();
                        this.Visit(expression);
                        return this.candidates;
                    }

                    public override Expression Visit(Expression expression)
                    {
                        if (expression != null)
                        {
                            var saveCannotBeEvaluated = this.cannotBeEvaluated;
                            this.cannotBeEvaluated = false;
                            base.Visit(expression);
                            if (!this.cannotBeEvaluated)
                            {
                                if (this.fnCanBeEvaluated(expression))
                                {
                                    this.candidates.Add(expression);
                                }
                                else
                                {
                                    this.cannotBeEvaluated = true;
                                }
                            }
                            this.cannotBeEvaluated |= saveCannotBeEvaluated;
                        }
                        return expression;
                    }
                }
            }

            #endregion
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
        BiggerThan,

        ///<summary>A >= B</summary>
        BiggerThanOrEquals,

        ///<summary>A < B</summary>
        LessThan,

        ///<summary>A <= B</summary>
        LessThanOrEquals,

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

        ///<summary>Skip several first elements after sorting</summary>
        Offset,
    }


    public class WhereCondition
    {
        internal WhereCondition(Condition condition)
        {
            Conditions.Add(condition);
        }

        #region AND Where
        public WhereCondition AndWhere<T>(Expression<Func<T, dynamic>> expression)
        {
            return AddConditionAndReturnSelf(new Condition(ConditionTypes.AND, expression));
        }

        public WhereCondition AndWhere<T>(Expression<Func<T, object>> member, Is operation, params object[] parameters)
        {
            return AddConditionAndReturnSelf(new Condition(ConditionTypes.AND, Utils.Converters.ExpressionToString(member), operation, parameters));
        }

        public WhereCondition AndWhere(string propertyName, Is operation, params object[] parameters)
        {
            return AddConditionAndReturnSelf(new Condition(ConditionTypes.AND, propertyName, operation, parameters));
        }
        #endregion

        #region OR Where
        public WhereCondition OrWhere<T>(Expression<Func<T, dynamic>> expression)
        {
            return AddConditionAndReturnSelf(new Condition(ConditionTypes.OR, expression));
        }

        public WhereCondition OrWhere<T>(Expression<Func<T, object>> member, Is operation, params object[] parameters)
        {
            return AddConditionAndReturnSelf(new Condition(ConditionTypes.OR, Utils.Converters.ExpressionToString(member), operation, parameters));
        }

        public WhereCondition OrWhere(string propertyName, Is operation, params object[] parameters)
        {
            return AddConditionAndReturnSelf(new Condition(ConditionTypes.OR, propertyName, operation, parameters));
        }
        #endregion

        private WhereCondition AddConditionAndReturnSelf(Condition condition)
        {
            Conditions.Add(condition);

            return this;
        }

        internal enum ConditionTypes { AND, OR, WHERE }

        internal static Dictionary<Is, string> SimpleOperators = new Dictionary<Is, string>
        {
            {
                Is.BiggerThan, ">"
            },
            {
                Is.BiggerThanOrEquals, ">="
            },
            {
                Is.LessThan, "<"
            },
            {
                Is.LessThanOrEquals, "<="
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
            public Condition() { }
            public Condition(ConditionTypes conditionType, Expression expression)
            {
                Expression = expression;
                ConditionType = conditionType;
            }
            public Condition(ConditionTypes conditionType, string propertyName, Is operation, params object[] parameters)
            {
                PropertyName = propertyName;
                Operation = operation;
                Parameters = parameters;
                ConditionType = conditionType;
            }

            internal string PropertyName;
            internal Is Operation;
            internal object[] Parameters;
            internal Expression Expression;
            internal ConditionTypes ConditionType = ConditionTypes.WHERE;
        }

        internal List<Condition> Conditions = new List<Condition>();
        internal long Limit = -1;
        internal long Offset = 0;
        internal bool Reverse = false;

        public List<T> Get<T>() => (List<T>)SQLite.Worker.ExecuteQuery(null, typeof(T), this);

        public IList Get(Type type) => (IList)SQLite.Worker.ExecuteQuery(null, type, this);

        public int GetCount<T>() => (int)SQLite.Worker.ExecuteQuery(null, typeof(T), this);

        public SQLiteMethodResponse Delete<T>(params object[] data) => (SQLiteMethodResponse)SQLite.Worker.ExecuteQuery(data, typeof(T), this);

        public SQLiteMethodResponse Delete(Type type, params object[] data) => (SQLiteMethodResponse)SQLite.Worker.ExecuteQuery(data, type, this);
    }


    public class SQLiteMethodResponse
    {
        public int RowsCountAffected { get; set; } = 0;
        public string SqlQuery { get; set; } = string.Empty;
        public Exception Exception { get; set; } = null;
    }


    public static class DoubleExtensions
    {
        public static string ToSQLiteString(this object value, bool isLike = false)
        {
            if (value == null)
                return "NULL";

            if (isLike)
                return $"'%{value}%'";

            if (value is double || value is float || value is decimal)
                return value.ToString().Replace(',', '.');

            if (value is string || value is Enum)
                return $"'{value}'";

            if (value is bool)
            {
                switch (SQLite.BooleanType)
                {
                    case SQLite.BooleanStorageType.Integer:
                        return (bool)value ? "1" : "0";

                    case SQLite.BooleanStorageType.String:
                        return $"'{value}'";
                }
            }

            if (value.GetType() == typeof(DateTime) ||
                value.GetType() == typeof(DateTimeOffset))
            {
                return $"'{value}'";
            }

            if (value.GetType().IsPrimitive == false)
            {
                return $"'{Utils.Converters.ObjectToString(value)}'";
            }

            return value.ToString();
        }
    }
}