using Xunit;
using System;
using System.Collections.Generic;
using PN.Storage;

// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace Pn.TestsCore
{
    public class SqlLite
    {
        private const string TestDb = "test.db";

        [Fact]
        public void SetTest()
        {
            SQLite.PathToDB = TestDb;
            var a = SQLite.Set(new Value
            {
                StringValue = Guid.NewGuid().ToString()
            });
            Assert.NotNull(a);
        }

        [Fact]
        public void GetTest()
        {
            SQLite.PathToDB = TestDb;
            var values = SQLite.Get<Value>();
            Assert.NotNull(values);
        }

        [Fact]
        public void GetCountTest()
        {
            SQLite.PathToDB = TestDb;
            var values = SQLite.GetCount<Value>();
            Assert.NotNull(values);
        }

        // [Fact]
        public void UpdateTest()
        {
            SQLite.PathToDB = TestDb;
        }

        // [Fact]
        public void MainTest()
        {
            SQLite.PathToDB = TestDb;
            var counts = new int[2];
            counts[0] = SQLite.GetCount<Value>();

            SQLite.Set(new Value
            {
                StringValue = Guid.NewGuid().ToString()
            });

            counts[1] = SQLite.GetCount<Value>();

            Assert.NotEqual(counts[0], counts[1]);
        }

        private class Value
        {
            // ReSharper disable once UnusedMember.Local
            public int Id { get; set; }

            public string StringValue { get; set; }
        }
    }
}