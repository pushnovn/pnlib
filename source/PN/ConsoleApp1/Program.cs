using Newtonsoft.Json;
using PN.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static PN.Network.HTTP;
using static PN.Network.HTTP.Entities;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            //API.Init("http://projects.pushnovn.com", new List<HeaderAttribute>()
            //    {
            //        new HeaderAttribute("ppppppppp", "*****************"),
            //        new HeaderAttribute("hgggggggggg", "sssssssssssssssssssss"),
            //    });

            var mod = new TestRequsetModel()
            {
                Headers = new List<HeaderAttribute>()
                {
                    new HeaderAttribute("1", "2"),
                    new HeaderAttribute("3", "4"),
                }
            };

        //    var ppp = API.Testtt.TestAsync(mod).Result;
        //    var ppp = API.Testtt.Test(mod);

          
        //    Console.WriteLine(ppp.Exception?.ToString() ?? ppp.id);

            //        var ttt = API.Testtt.TestAsync(mod).Result;
            //        var ttt2 = API.Testtt.Test(mod);

            Task.Run(async () =>
            {
                var test00 = await API_VR.Version(new VersionRequestModel()
                {
                    Token = "p4XsNk3x1WLbmL1WOROBGYEpmN2cjJsXwSSl666Jpe6ZbrUYpWk9z8rnNQFLZVmGeG4TXSef8CvW2GIk3v9y2aLsuAMfgjabYwVbYMLSvLqaOA/bs76wcUxZxYVj4Z4hSlljc2yll5zxTNbIiRqMf2RAKbE5zsVOT56lVrztYNRYDkXzHTNx8XecTUZ3a+RpQJdbifcgVkJJsM3Ze0gePg==",
                    Version = "123",
                });

                var test0 = await API_test.GetRequestInfo(new VersionRequestModel()
                {
                    Token = "p4XsNk3x1WLbmL1WOROBGYEpmN2cjJsXwSSl666Jpe6ZbrUYpWk9z8rnNQFLZVmGeG4TXSef8CvW2GIk3v9y2aLsuAMfgjabYwVbYMLSvLqaOA/bs76wcUxZxYVj4Z4hSlljc2yll5zxTNbIiRqMf2RAKbE5zsVOT56lVrztYNRYDkXzHTNx8XecTUZ3a+RpQJdbifcgVkJJsM3Ze0gePg==",
                    Version = "123",
                });

                var test01 = await API_test_2.GetImage(new VersionRequestModel()
                {
                    Token = "p4XsNk3x1WLbmL1WOROBGYEpmN2cjJsXwSSl666Jpe6ZbrUYpWk9z8rnNQFLZVmGeG4TXSef8CvW2GIk3v9y2aLsuAMfgjabYwVbYMLSvLqaOA/bs76wcUxZxYVj4Z4hSlljc2yll5zxTNbIiRqMf2RAKbE5zsVOT56lVrztYNRYDkXzHTNx8XecTUZ3a+RpQJdbifcgVkJJsM3Ze0gePg==",
                    Version = "123",
                });

                var ttt = Convert.ToBase64String(test01.ResponseBody);

                Console.WriteLine(ttt);
                //StreamReader reader = new StreamReader(test0.ResponseStream);
                //string text = reader.ReadToEnd();

                var yy =  JsonConvert.DeserializeObject(null, typeof(TestModel));

                var tt = new List<string>() { "", "" };
                var t = tt.IndexOf(null);

                var test1 = API.TestClass.Test(mod);

                var test2 = await API.TestClass.TestAsync(mod);



                var ppp = API.Testtt.Test(mod);
                Console.WriteLine("Sync: " + (ppp.Exception?.ToString() ?? ppp.id));

                ppp = await API.Testtt.TestAsync(mod);
                Console.WriteLine("Async: " + (ppp.Exception?.ToString() ?? ppp.id));

            });

            while (true)
                Console.ReadLine();
        }
    }

    [Url("http://projects.pushnovn.com/pn/")]
    public class API_test : HTTP
    {
        [Url("get_request_info")]
        public static Task<VersionResponseModel> GetRequestInfo(VersionRequestModel ttt) => Base(ttt);
    }

    [Url("https://gc.onliner.by/images/logo/")]
    public class API_test_2 : HTTP
    {
        [Url("onliner_logo.v3@2x.png?token=1532946804")]
        public static Task<VersionResponseModel> GetImage(VersionRequestModel ttt) => Base(ttt);
    }

    [Url("http://videoreg.pushnovn.com:1337/api")]
    public class API_VR : HTTP
    { 
        [RequestType(RequestTypes.GET)]
        [Url("Version?Version={Version}&Token={Token}")]
        public static Task<VersionResponseModel> Version(VersionRequestModel ttt) => Base(ttt);
    }

    public class VersionRequestModel : Entities.RequestEntity
    {
        [JsonIgnore]
        public string Version { get; set; }
        [JsonIgnore]
        public string Token { get; set; }
    }
    public class VersionResponseModel : Entities.ResponseEntity
    {
        public string Version { get; set; }
        public int Position { get; set; }
        public bool NeedToUpdateDB { get; set; }
        public string Script { get; set; }
    }






    [Url("http://projects.pushnovn.com/")]
    class API : HTTP
    {
        [Url("test")]
        public class Testtt
        {

            [RequestType(RequestTypes.GET)]
            [Header("aaaaaa", "bbbbbbbbbbbbbbb")]
            [IgnoreGlobalHeaders]
            [Url("")]
            public static Task<TestModel> TestAsync(RequestEntity ttt) => Base(ttt);
            
            [Header("aaaaaa", "bbbbbbbbbbbbbbb")]
            [IgnoreGlobalHeaders]
            [Url("")]
            public static TestModel Test(RequestEntity ttt) => Base(ttt);
        }

        public class TestClass
        {
            [Url("")]
            public static TestModel Test(RequestEntity ttt) => Base(ttt);

            [Url("")]
            public static Task<TestModel> TestAsync(RequestEntity ttt) => Base(ttt);
        }

        public class A
        {
            [Url("BBBB")]
            public class B
            {
                public class C
                {
                    [Url("D/method-name")]
                    [Header("aaaaaa", "bbbbbbbbbbbbbbb")]
                    [IgnoreGlobalHeaders]
                    public static Task<TestModel> TestAsync(RequestEntity ttt) => Base(ttt);

                    [Url("D/method-name")]
                    [Header("aaaaaa", "bbbbbbbbbbbbbbb")]
                    [IgnoreGlobalHeaders]
                    public static TestModel Test(RequestEntity ttt) => Base(ttt);
                }
            }
        }
    }

    public class TestRequsetModel : Entities.RequestEntity
    {
        public string yyy { get; set; } = "888";
    }

    public class TestModel : Entities.ResponseEntity
    {
        public string id { get; set; }
    }
}
