using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace PN.Storage.EF
{
    public static class SimpleRepository
    {
        #region public methods

        public static TEntity Single<TEntity>(Func<TEntity, bool> predicate) where TEntity : class
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return EfSingle(predicate);
        }

        public static List<TEntity> Get<TEntity>(Func<TEntity, bool> predicate = null) where TEntity : class
        {
            return EfGet(predicate).ToList();
        }

        public static TEntity Upsert<TEntity>(this TEntity value) where TEntity : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return (TEntity)InvokeGenericMethodByName(nameof(EfUpsert), value.GetType(), new object[] { value });
        }

        public static void Delete<TEntity>(Func<TEntity, bool> predicate) where TEntity : class
        {
            Single(predicate)?.Delete();
        }

        public static void Delete<TEntity>(this TEntity value) where TEntity : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            InvokeGenericMethodByName(nameof(EfDelete), value.GetType(), new object[] { value });
        }

        public static bool Any<TEntity>(Func<TEntity, bool> predicate) where TEntity : class
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return EfAny(predicate);
        }

        public static int Count(Type type)
        {
            return (int)InvokeGenericMethodByName(nameof(EfCount), type, new object[] { null });
        }

        public static int Count<TEntity>(Func<TEntity, bool> predicate = null) where TEntity : class
        {
            return EfCount(predicate);
        }

        public static void SetDbContext(Type contextType) => EfDbContextType = contextType;

        #endregion

        #region private impl

        private static Type EfDbContextType;

        private static (DbSet<TEntity> Entities, DbContext EfContext) CreateEfDB<TEntity>() where TEntity : class
        {
            var context = (DbContext)Activator.CreateInstance(EfDbContextType ?? throw new NullReferenceException($"You need call '{nameof(SetDbContext)}' before using SimpleRepository' methods!"));
            var dbSet = context.Set<TEntity>();
            return (dbSet, context);
        }

        private static TEntity EfSingle<TEntity>(Func<TEntity, bool> predicate) where TEntity : class
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            var entities = CreateEfDB<TEntity>().Entities;
            var returnValue = entities.FirstOrDefault(predicate);
            return returnValue;
        }

        private static IQueryable<TEntity> EfGet<TEntity>(Func<TEntity, bool> predicate = null) where TEntity : class
        {
            var entities = CreateEfDB<TEntity>().Entities;
            var returnValue = predicate == null ? entities : entities.Where(predicate).AsQueryable();
            return returnValue;
        }

        private static bool EfAny<TEntity>(Func<TEntity, bool> predicate) where TEntity : class
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return CreateEfDB<TEntity>().Entities.AsNoTracking().Any(predicate);
        }

        private static TEntity EfUpsert<TEntity>(this TEntity value) where TEntity : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var (entities, efContext) = CreateEfDB<TEntity>();
            var isValueNew = false == EfAny<TEntity>(e => ComparePrimaryKeys(e, value, GetPrimaryKeyName<TEntity>(efContext)));

            var entityTypes = efContext.Model.GetEntityTypes()
                                             .Select(t => t.ClrType)
                                             .ToList();

            foreach (var prop in typeof(TEntity).GetProperties())
            {
                if (entityTypes.Contains(prop.PropertyType))
                {
                    efContext.Attach(prop.GetValue(value) ?? Activator.CreateInstance<TEntity>());
                }
            }

            var upsertEntity = isValueNew ? entities.Add(value)?.Entity : entities.Update(value)?.Entity;

            efContext.SaveChanges();
            efContext.Dispose();
            return upsertEntity;
        }

        private static bool ComparePrimaryKeys<TEntity>(TEntity t1, TEntity t2, string primaryKeyName)
        {
            var prop = typeof(TEntity).GetProperty(primaryKeyName);

            var t1PK = prop.GetValue(t1).ToString();
            var t2PK = prop.GetValue(t2).ToString();

            return t1PK == t2PK;
        }

        private static string GetPrimaryKeyName<T>(DbContext dbContext)
        {
            return dbContext.Model
                            .FindEntityType(typeof(T))
                            .FindPrimaryKey()
                            .Properties
                            .Select(x => x.Name)
                            .Single();
        }


        private static void EfDelete<TEntity>(this TEntity value) where TEntity : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var (entities, efContext) = CreateEfDB<TEntity>();
            entities.Remove(value);
            efContext.SaveChanges();
            efContext.Dispose();
        }

        private static int EfCount<TEntity>(Func<TEntity, bool> predicate = null) where TEntity : class
        {
            var (entities, _) = CreateEfDB<TEntity>();
            return predicate == null ? entities.Count() : entities.Count(predicate);
        }

        #endregion

        #region reflect helpers

        private static object InvokeGenericMethodByName(string methodName, Type genericType, object[] parameters = null)
        {
            return GetGenericMethod(genericType, methodName).Invoke(null, parameters ?? new object[] { });
        }

        private const BindingFlags AccessibleBindingFlags = BindingFlags.Public |
                                                            BindingFlags.Static |
                                                            BindingFlags.Instance |
                                                            BindingFlags.NonPublic |
                                                            BindingFlags.IgnoreCase;

        private static MethodInfo GetGenericMethod(Type type, string methodName)
        {
            var method = typeof(SimpleRepository).GetMethod(methodName, AccessibleBindingFlags);
            var generic = method.MakeGenericMethod(type);
            return generic;
        }

        #endregion
    }
}