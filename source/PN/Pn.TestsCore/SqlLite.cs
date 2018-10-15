using Xunit;
using System;
using PN.Storage;

// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace Pn.TestsCore
{
    public class SqlLite
    {
        private const string TestDb = "test.db";

        [Fact]
        public bool SetTest()
        {
            SQLite.PathToDB = TestDb;
            var a = SQLite.Set(new Message
            {
                MessageText = Guid.NewGuid().ToString()
            });

            Assert.NotNull(a);

            return true;
        }

        [Fact]
        public bool GetTest()
        {
            SQLite.PathToDB = TestDb;
            var list = SQLite.Get<Value>();
            Assert.NotNull(list);

            var iList = SQLite.Get(typeof(Value));
            Assert.NotNull(iList);

            return true;
        }

        [Fact]
        public void GetCountTest()
        {
            SQLite.PathToDB = TestDb;
            var values = SQLite.GetCount<Value>();
            Assert.NotNull(values);
        }

        [Fact]
        public void UpdateTest()
        {
            SQLite.PathToDB = TestDb;
        }

        [Fact]
        public void DeleteTest()
        {
            SQLite.PathToDB = TestDb;
            var setTest = SetTest();
            Assert.True(setTest, "setTest");

            var getTest = GetTest();
            Assert.True(getTest, "getTest");

            var counts = new int[2];
            counts[0] = SQLite.Get<Value>().Count;

            counts[1] = SQLite.Get<Value>().Count;
        }

        [SQLite.SQLiteName("Values")]
        private class Value
        {
            // ReSharper disable once UnusedMember.Local
            public int Id { get; set; }

            public string StringValue { get; set; }
        }

        private class Message
        {
            public int Id { get; set; }

            public string MessageText { get; set; }
        }
    }
}