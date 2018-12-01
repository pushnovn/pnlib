using System.Linq;
using static PN.Storage.SQLite;

namespace PN.Storage
{
    public class SSSOverSQLite : SSS
    {
        protected override string Get(string key)
        {
            var entities = SQLite.Where(nameof(SSSEntity.Key), Is.Equals, key).Get<SSSEntity>();

            return entities.FirstOrDefault()?.Value;
        }

        protected override void Set(string key, string value)
        {
            var entities = SQLite.Where(nameof(SSSEntity.Key), Is.Equals, key).Get<SSSEntity>();

            if (entities.FirstOrDefault() == null)
            {
                SQLite.Set(new SSSEntity() { Key = key, Value = value });
            }
            else
            {
                SQLite.Update(new SSSEntity() { id = entities.FirstOrDefault().id, Key = key, Value = value });
            }
        }

        [SQLiteName("SSS")]
        private class SSSEntity
        {
            public int id { get; set; }
            public string Key { get; set; }
            public string Value { get; set; }
        }
    }

    public static class MySSSOverSQLiteExtensions
    {
   //     public static T IndexOf

        static System.Collections.Generic.Dictionary<object, System.Guid> objects_guids2 = new System.Collections.Generic.Dictionary<object, System.Guid>();

        public static System.Guid GetGUIDNew(this object o)
        {
            if (objects_guids2.ContainsKey(o))
                return objects_guids2[o];

            return objects_guids2[o] = System.Guid.NewGuid();
        }
    }
}