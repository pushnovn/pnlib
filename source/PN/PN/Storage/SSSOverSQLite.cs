using System.Linq;
using static PN.Storage.SQLite;

namespace PN.Storage
{
    public class SSSOverSQLite : SSS
    {
        protected override string Get(string key)
        {
            var entities = SQLite.WhereAND(nameof(SSSEntity.Key), Is.Equals, key).Get<SSSEntity>();

            return entities.FirstOrDefault()?.Value;
        }

        protected override void Set(string key, string value)
        {
            var entities = SQLite.WhereAND(nameof(SSSEntity.Key), Is.Equals, key).Get<SSSEntity>();

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
}
