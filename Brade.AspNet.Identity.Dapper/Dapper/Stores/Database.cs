/*
 License: http://www.apache.org/licenses/LICENSE-2.0 
 Home page: http://code.google.com/p/dapper-dot-net/
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Dapper;

namespace Brade.AspNet.Identity.Dapper.Dapper.Stores
{
    /// <summary>
    /// A container for a database, assumes all the tables have an Id column named Id
    /// </summary>
    /// <typeparam name="TDatabase"></typeparam>
    public abstract partial class Database<TDatabase> : IDisposable where TDatabase : Database<TDatabase>, new()
    {
        public partial class Table<T, TId, TPrimaryKeys>
        {
            internal Database<TDatabase> Database;
            internal string _tableName;
            internal string LikelyTableName;
            private readonly List<string> _primaryKeys;

            public Table(Database<TDatabase> database, string likelyTableName, List<string> primaryKeys)
            {
                Database = database;
                LikelyTableName = likelyTableName;
                _primaryKeys = (primaryKeys == null || !primaryKeys.Any()) ?  new List<string>{"Id"} : primaryKeys;
            }

            public string TableName
            {
                get
                {
                    _tableName = _tableName ?? Database.DetermineTableName<T>(LikelyTableName);
                    return _tableName;
                }
            }

            /// <summary>
            /// Insert a row into the db
            /// </summary>
            /// <param name="data">Either DynamicParameters or an anonymous type or concrete type</param>
            /// <returns></returns>
            public virtual int? Insert(dynamic data)
            {
                var o = (object)data;
                List<string> paramNames = GetParamnamesWithoutPrimaryKey(o);
                

                string cols = string.Join(",", paramNames);
                string colsParams = string.Join(",", paramNames.Select(p => "@" + p));
                var sql = "set nocount on insert " + TableName + " (" + cols + ") values (" + colsParams + ") select cast(scope_identity() as int)";

                return Database.Query<int?>(sql, o).Single();
            }

            private List<string>  GetParamnamesWithoutPrimaryKey(object o)
            {
                 var ret = GetParamNames(o);
                 foreach (var key in _primaryKeys)
                 {
                     ret.Remove(key);
                 }
                return ret;
            }

            /// <summary>
            /// Update a record in the DB
            /// </summary>
            /// <param name="id"></param>
            /// <param name="data"></param>
            /// <returns></returns>
            public int Update(TId id, dynamic data)
            {
                List<string> paramNames = GetParamnamesWithoutPrimaryKey((object)data);

                var builder = new StringBuilder();
                builder.Append("update ").Append(TableName).Append(" set ");
                builder.AppendLine(string.Join(",", paramNames.Select(p => p + "= @" + p)));

                // todo primarykeys
                builder.Append("where Id = @Id");
                // todo primarykeys
                DynamicParameters parameters = new DynamicParameters(data);
                parameters.Add("Id", id);

                return Database.Execute(builder.ToString(), parameters);
            }

            /// <summary>
            /// Delete a record for the DB
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            public bool Delete(TId id)
            {                
                return Database.Execute("delete from " + TableName + " where Id = @id", new { id }) > 0;
            }

            /// <summary>
            /// Grab a record with a particular Id from the DB 
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            public T Get(TId id)
            {
                return Database.Query<T>("select * from " + TableName + " where Id = @id", new { id }).FirstOrDefault();
            }

            public virtual T First()
            {
                return Database.Query<T>("select top 1 * from " + TableName).FirstOrDefault();
            }

            public IEnumerable<T> All()
            {
                return Database.Query<T>("select * from " + TableName);
            }

            static readonly ConcurrentDictionary<Type, List<string>> ParamNameCache = new ConcurrentDictionary<Type, List<string>>();

            internal static List<string> GetParamNames(object o)
            {
                if (o is DynamicParameters)
                {
                    return (o as DynamicParameters).ParameterNames.ToList();
                }

                List<string> paramNames;
                if (ParamNameCache.TryGetValue(o.GetType(), out paramNames))
                {
                    return paramNames;
                }

                paramNames = new List<string>();
                foreach (var prop in o.GetType().GetProperties(BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public))
                {
                    var attribs = prop.GetCustomAttributes(typeof(IgnorePropertyAttribute), true);
                    var attr = attribs.FirstOrDefault() as IgnorePropertyAttribute;
                    if (attr == null || (attr != null && !attr.Value))
                    {
                        paramNames.Add(prop.Name);
                    }
                }
                ParamNameCache[o.GetType()] = paramNames;
                return paramNames;
            }
        }

        public class Table<T> : Table<T, int, string>
        {
            public Table(Database<TDatabase> database, string likelyTableName, List<string> primaryKeys)
                : base(database, likelyTableName,primaryKeys)
            {
            }
        }

        DbConnection _connection;
        int _commandTimeout;
        DbTransaction _transaction;


        public static TDatabase Init(DbConnection connection, int commandTimeout)
        {
            TDatabase db = new TDatabase();
            db.InitDatabase(connection, commandTimeout);
            return db;
        }

        internal static Action<TDatabase> TableConstructor;

        internal void InitDatabase(DbConnection connection, int commandTimeout)
        {
            _connection = connection;
            _commandTimeout = commandTimeout;
            if (TableConstructor == null)
            {
                TableConstructor = CreateTableConstructorForTable();
            }

            TableConstructor(this as TDatabase);
        }

        internal virtual Action<TDatabase> CreateTableConstructorForTable()
        {
            return CreateTableConstructor(typeof(Table<>));
        }

        public void BeginTransaction(IsolationLevel isolation = IsolationLevel.ReadCommitted)
        {
            _transaction = _connection.BeginTransaction(isolation);
        }

        public void CommitTransaction()
        {
            _transaction.Commit();
            _transaction = null;
        }

        public void RollbackTransaction()
        {
            _transaction.Rollback();
            _transaction = null;
        }

        protected Action<TDatabase> CreateTableConstructor(Type tableType)
        {
            var dm = new DynamicMethod("ConstructInstances", null, new [] { typeof(TDatabase) }, true);
            var il = dm.GetILGenerator();

            var setters = GetType().GetProperties()
                .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == tableType)
                .Select(p => Tuple.Create(
                        p.GetSetMethod(true),
                        p.PropertyType.GetConstructor(new [] { typeof(TDatabase), typeof(string) }),
                        p.Name,
                        p.DeclaringType
                 ));

            foreach (var setter in setters)
            {
                il.Emit(OpCodes.Ldarg_0);
                // [db]

                il.Emit(OpCodes.Ldstr, setter.Item3);
                // [db, likelyname]

                il.Emit(OpCodes.Newobj, setter.Item2);
                // [table]

                var table = il.DeclareLocal(setter.Item2.DeclaringType);
                il.Emit(OpCodes.Stloc, table);
                // []

                il.Emit(OpCodes.Ldarg_0);
                // [db]

                il.Emit(OpCodes.Castclass, setter.Item4);
                // [db cast to container]

                il.Emit(OpCodes.Ldloc, table);
                // [db cast to container, table]

                il.Emit(OpCodes.Callvirt, setter.Item1);
                // []
            }

            il.Emit(OpCodes.Ret);
            return (Action<TDatabase>)dm.CreateDelegate(typeof(Action<TDatabase>));
        }


        static readonly ConcurrentDictionary<Type, string> TableNameMap = new ConcurrentDictionary<Type, string>();
        private string DetermineTableName<T>(string likelyTableName)
        {
            string name;

            if (!TableNameMap.TryGetValue(typeof(T), out name))
            {
                name = likelyTableName;
                if (!TableExists(name))
                {
                    name = "[" + typeof(T).Name + "]";
                }

                TableNameMap[typeof(T)] = name;
            }
            return name;
        }

        private bool TableExists(string name)
        {
            string schemaName = null;

            name = name.Replace("[", "");
            name = name.Replace("]", "");

            if (name.Contains("."))
            {
                var parts = name.Split('.');
                if (parts.Count() == 2)
                {
                    schemaName = parts[0];
                    name = parts[1];
                }
            }

            var builder = new StringBuilder("select 1 from INFORMATION_SCHEMA.TABLES where ");
            if (!String.IsNullOrEmpty(schemaName)) builder.Append("TABLE_SCHEMA = @schemaName AND ");
            builder.Append("TABLE_NAME = @name");

            return _connection.Query(builder.ToString(), new { schemaName, name }, transaction: _transaction).Count() == 1;
        }

        public int Execute(string sql, dynamic param = null)
        {
            return SqlMapper.Execute(_connection, sql, param as object, _transaction, commandTimeout: this._commandTimeout);
        }

        public IEnumerable<T> Query<T>(string sql, dynamic param = null, bool buffered = true)
        {
            return SqlMapper.Query<T>(_connection, sql, param as object, _transaction, buffered, _commandTimeout);
        }

        public IEnumerable<TReturn> Query<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
        {
            return SqlMapper.Query(_connection, sql, map, param as object, transaction, buffered, splitOn);
        }

        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
        {
            return SqlMapper.Query(_connection, sql, map, param as object, transaction, buffered, splitOn);
        }

        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
        {
            return SqlMapper.Query(_connection, sql, map, param as object, transaction, buffered, splitOn);
        }

        public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null)
        {
            return SqlMapper.Query(_connection, sql, map, param as object, transaction, buffered, splitOn);
        }

        public IEnumerable<dynamic> Query(string sql, dynamic param = null, bool buffered = true)
        {
            return SqlMapper.Query(_connection, sql, param as object, _transaction, buffered);
        }

        public global::Dapper.SqlMapper.GridReader QueryMultiple(string sql, dynamic param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return SqlMapper.QueryMultiple(_connection, sql, param, transaction, commandTimeout, commandType);
        }


        public void Dispose()
        {

            if (_connection == null || _connection.State == ConnectionState.Closed)
            {
                return;
            }

            if (_transaction != null)
            {
                _transaction.Rollback();
            }

            _connection.Close();
            _connection = null;
        }
    }
}