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

        public static TEntity Single<TEntity>(Func<TEntity, bool> predicate) where TEntity : BaseEntity
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return EfSingle(predicate);
        }

        public static List<TEntity> Get<TEntity>(Func<TEntity, bool> predicate = null) where TEntity : BaseEntity
        {
            return EfGet(predicate).ToList();
        }

        public static TEntity Upsert<TEntity>(this TEntity value) where TEntity : BaseEntity
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return (TEntity)InvokeGenericMethodByName(nameof(EfUpsert), value.GetType(), new object[] { value });
        }

        public static void Delete<TEntity>(Func<TEntity, bool> predicate) where TEntity : BaseEntity
        {
            Single(predicate)?.Delete();
        }

        public static void Delete<TEntity>(this TEntity value) where TEntity : BaseEntity
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            InvokeGenericMethodByName(nameof(EfDelete), value.GetType(), new object[] { value });
        }

        public static bool Any<TEntity>(Func<TEntity, bool> predicate) where TEntity : BaseEntity
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

        public static int Count<TEntity>(Func<TEntity, bool> predicate = null) where TEntity : BaseEntity
        {
            return EfCount(predicate);
        }

        public static void SetDbContext(Type contextType) => EfDbContextType = contextType;

        #endregion

        #region private impl

        private static Type EfDbContextType;

        private static (DbSet<TEntity> Entities, DbContext EfContext) CreateEfDB<TEntity>() where TEntity : BaseEntity
        {
            var context = (DbContext)Activator.CreateInstance(EfDbContextType ?? throw new NullReferenceException("You need call 'SetDbContext' before using SimpleRepository' methods!"));
            var dbSet = context.Set<TEntity>();
            return (dbSet, context);
        }

        private static TEntity EfSingle<TEntity>(Func<TEntity, bool> predicate) where TEntity : BaseEntity
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            var entities = CreateEfDB<TEntity>().Entities;
            var returnValue = entities.FirstOrDefault(predicate);
            return returnValue;
        }

        private static IQueryable<TEntity> EfGet<TEntity>(Func<TEntity, bool> predicate = null) where TEntity : BaseEntity
        {
            var entities = CreateEfDB<TEntity>().Entities;
            var returnValue = predicate == null ? entities : entities.Where(predicate).AsQueryable();
            return returnValue;
        }

        private static bool EfAny<TEntity>(Func<TEntity, bool> predicate) where TEntity : BaseEntity
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return CreateEfDB<TEntity>().Entities.AsNoTracking().Any(predicate);
        }

        private static TEntity EfUpsert<TEntity>(this TEntity value) where TEntity : BaseEntity
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var (entities, efContext) = CreateEfDB<TEntity>();
            var isValueNew = false == EfAny<TEntity>(e => e.Id == value.Id);

            var entityProperties = value.GetType().GetProperties().ToList();
            entityProperties.ForEach(p =>
            {
                if (p.PropertyType.BaseType != typeof(BaseEntity)) return;
                var val = p.GetValue(value);
                efContext.Attach(val);
            });

            var upsertEntity = isValueNew ? entities.Add(value)?.Entity : entities.Update(value)?.Entity;

            efContext.SaveChanges();
            efContext.Dispose();
            return upsertEntity;
        }

        private static void EfDelete<TEntity>(this TEntity value) where TEntity : BaseEntity
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

        private static int EfCount<TEntity>(Func<TEntity, bool> predicate = null) where TEntity : BaseEntity
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

        public class BaseEntity
        {
            public Guid Id { get; set; }
        }

        #endregion
    }
}