using Xunit;
using System;
using System.Collections.Generic;
using PN.Storage;

// ReSharper disable UnusedMember.Local

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
                Text = Guid.NewGuid().ToString(),
                Attaches = new List<Attach>
                {
                    new Attach
                    {
                        FileName = Guid.NewGuid().ToString(), UploadDt = DateTime.UtcNow.AddDays(1)
                    },
                    new Attach
                    {
                        FileName = Guid.NewGuid().ToString(), UploadDt = DateTime.UtcNow.AddDays(2)
                    }
                }
            });

            Assert.NotNull(a);
            Assert.Null(a.Exception);

            return true;
        }

        [Fact]
        public bool GetTest()
        {
            SQLite.PathToDB = TestDb;
            var list = SQLite.Get<Message>();
            Assert.NotNull(list);

            var iList = SQLite.Get(typeof(Message));
            Assert.NotNull(iList);

            return true;
        }

        [Fact]
        public void GetCountTest()
        {
            SQLite.PathToDB = TestDb;
            var values = SQLite.GetCount<Message>();
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
            var setTest = SetTest(); // если SetTest прошел успешно, то в базе есть как минимум 1 запись
            Assert.True(setTest, "setTest");

            var getTest = GetTest();
            Assert.True(getTest, "getTest");

            var counts = new int[2];
            counts[0] = SQLite.Get<Message>().Count;

            SQLite.Delete(SQLite.Get<Message>()[0]);

            counts[1] = SQLite.Get<Message>().Count;

            Assert.NotEqual(counts[0], counts[1]);
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

            [SQLite.SQLiteName("MessageText")]
            public string Text { get; set; }

            public List<Attach> Attaches { get; set; }
        }

        private class Attach
        {
            public string FileName { get; set; }

            public DateTime UploadDt { get; set; }
        }
    }
}