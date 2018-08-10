//using Newtonsoft.Json;
using PN.Network;
using PN.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static PN.Network.HTTP;
using static PN.Network.HTTP.Entities;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
        //    StaticTest2.TestProp = "";
        //    Console.WriteLine(StaticTest2.TestProp);
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

            StaticTest.Get();

            while (true)
                Console.ReadLine();

            Task.Run(async () =>
            {
                var typ = await API___.Files(new FilesRequestModel());

            //    var files = JsonConvert.DeserializeObject(respone.ResponseText, typeof(List<ServerFileInfo>));
                var teststr = API_VR.TestString(new VersionRequestModel()
                {
                    Token = "p4XsNk3x1WLbmL1WOROBGYEpmN2cjJsXwSSl666Jpe6ZbrUYpWk9z8rnNQFLZVmGeG4TXSef8CvW2GIk3v9y2aLsuAMfgjabYwVbYMLSvLqaOA/bs76wcUxZxYVj4Z4hSlljc2yll5zxTNbIiRqMf2RAKbE5zsVOT56lVrztYNRYDkXzHTNx8XecTUZ3a+RpQJdbifcgVkJJsM3Ze0gePg==",
                    Version = "123",
                });

                var testint = API_VR.TestInt(new VersionRequestModel()
                {
                    Token = "p4XsNk3x1WLbmL1WOROBGYEpmN2cjJsXwSSl666Jpe6ZbrUYpWk9z8rnNQFLZVmGeG4TXSef8CvW2GIk3v9y2aLsuAMfgjabYwVbYMLSvLqaOA/bs76wcUxZxYVj4Z4hSlljc2yll5zxTNbIiRqMf2RAKbE5zsVOT56lVrztYNRYDkXzHTNx8XecTUZ3a+RpQJdbifcgVkJJsM3Ze0gePg==",
                    Version = "123",
                });

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

         //       Console.WriteLine(ttt);
                //StreamReader reader = new StreamReader(test0.ResponseStream);
                //string text = reader.ReadToEnd();

       //         var yy =  JsonConvert.DeserializeObject(null, typeof(TestModel));

                var tt = new List<string>() { "", "" };
                var t = tt.IndexOf(null);

                var test1 = API.TestClass.Test(mod);

                var test2 = await API.TestClass.TestAsync(mod);



                var ppp = API.Testtt.Test(mod);
                Console.WriteLine("Sync: " + (ppp.Exception?.ToString() ?? ppp.id));

                ppp = await API.Testtt.TestAsync(mod);
                Console.WriteLine("Async: " + (ppp.Exception?.ToString() ?? ppp.id));

            });

        }
    }

    

    public class StaticTest
    {
        public static string Get(string ooo = null)
        {
            var arr = new byte[1];

            var str = PN.Utils.Utils.Converters.BytesToString(arr);

            var bewarr = PN.Utils.Utils.Converters.StringToBytes("");


            PN.Utils.Utils.Debug.CalculateMethodTimeExecution(() => 
            {
                StackTrace st = new StackTrace();
                StackFrame[] fr = st.GetFrames();

                if (fr == null) return;

                Console.WriteLine($"fr.count = {fr.Count()}");

                for (int i = 0; i < fr.Count(); i++)
                {
                    var method = fr[i].GetMethod();

                    var ss = method.ReflectedType.FullName + " :: " + method.Name;

                    //var baseResponseModelType = (method as MethodInfo)?.ReturnType;
                    //var isGenericType = baseResponseModelType.IsGenericType;
                    //var responseModelType = isGenericType ? baseResponseModelType.GetGenericArguments()[0] : baseResponseModelType;

                    //var temp_uri = string.Empty;
                    //var typ = method.ReflectedType;
                    //while (typ != null)
                    //{
                    //    typ = typ.ReflectedType;
                    //}
                }
            }, "for");

            var ddd = SSS2.dict;

            SSS3.Auth<SSS3>("some auth pass");

            var ch1 = SSS3.CheckPassword<SSS3>("some auth pass");
            var ch2 = SSS3.CheckPassword<SSS3>("some auth pass 2");

            SSS3.ExampleTestReCrypt = new TestModel() { id = "ExampleTestReCrypt" };

            SSS3.ExampleTest3Model = new TestModel() { id = "IDDD" };

            SSS3.UpdatePasswordAndReCrypt<SSS3>("some new cryptostring");
            
            var ch3 = SSS3.CheckPassword<SSS3>("some new cryptostring");
            var ch4 = SSS3.CheckPassword<SSS3>("some auth pass");

            var testExampleTestReCrypt = SSS3.ExampleTestReCrypt;





            var uio = PN.Utils.Utils.Converters.NumberToShortFormattedString(8);






            var ttestmodel = SSS3.ExampleTest3Model;
            SSS3.ExampleTest3Model = new TestModel() { id = "IDDD" };
            ttestmodel = SSS3.ExampleTest3Model;








            var examplInt = SSS2.ExampleInt;
            var exampl = SSS2.Example;

            SSS2.ExampleInt = 7;
            examplInt = SSS2.ExampleInt;

            SSS2.Example = "some string";
            exampl = SSS2.Example;

            MethodBase ttt = null;
            PN.Utils.Utils.Debug.CalculateMethodTimeExecution(() => {
                 ttt = new StackTrace().GetFrame(3).GetMethod();
            }, "single");
            ttt = new StackTrace().GetFrame(1).GetMethod();
            var ssp = ttt.GetParameters();
            return ttt.ReflectedType.FullName + " :: " + ttt.Name;

        }
    }

    
        [Url("http://videoreg.pushnovn.com:1583/api/Files?Version=%7BVersion%7D&Token=jICmeDBf2e2vyfCkzlI87P1eG/PIQFjFeanVrXxgj7nr+o7T4LiNYWArvbOU3SrCIrcc2aAjNN4zIA6LgJmMmtfyGm/1SShlVZ7cStft6LblcjXg3Q0CIMkdSUtQ5sQy44WWoU4X7KLC3oiRNbyg4sJeGGta3qmxEK+v7VXTJmQ06R5yRCpF27LANQ8YVT4AXAK")]
        public class API___ : HTTP
        {
            [RequestType(RequestTypes.GET)]
            [Url("kE5lGUznLKEiGvGFig==")]
            public static Task<List<ServerFileInfo>> Files(FilesRequestModel ttt) => Base(ttt);
        }

        public class FilesRequestModel : PN.Network.HTTP.Entities.RequestEntity
        {
            //[JsonIgnore]
            //public string Version { get; set; }
            //[JsonIgnore]
            //public string Token { get; set; }

        }
        public class FilesResponseModel //: PN.Network.HTTP.Entities.ResponseEntity
        {
            public List<ServerFileInfo> Files { get; set; }
        }

        public class ServerFileInfo
        {
            public string OriginalName { get; set; }
            public string MD5 { get; set; }
            public string URI { get; set; }
            public string FuturePath { get; set; }
            public string PathOnServer { get; set; }
            public string toke { get; set; }
            public long FileSize { get; set; }
            public bool IsDownloaded { get; set; }
        }
    

    

    public class SSS2 : SSS//, ISSS
    {
        public static string Example { get => Base(); set => Base(value); }

        public static int ExampleInt { get => Base(); set => Base(value); }

        public static Dictionary<string, string> dict = new Dictionary<string, string>();
        protected override string Get(string key) => dict.ContainsKey(key) ? dict[key] : null;
        protected override void Set(string key, string value) => dict[key] = value;
    }
    
    public class SSS3 : SSS2
    {
        [NeedAuth]
        [IsResistantToSoftRemoval]
        private static string ExampleTestReCryptKEY { get; set; } = "some crypt key...";

        public static TestModel ExampleTestReCrypt { get => Base(); set => Base(value); }

        [NeedAuth]
        public static TestModel ExampleTest3Model { get => Base(); set => Base(value); }
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
        public static string TestString(VersionRequestModel ttt) => Base(ttt);

        [RequestType(RequestTypes.GET)]
        [Url("Version?Version={Version}&Token={Token}")]
        public static int TestInt(VersionRequestModel ttt) => Base(ttt);

        [RequestType(RequestTypes.GET)]
        [Url("Version?Version={Version}&Token={Token}")]
        public static Task<VersionResponseModel> Version(VersionRequestModel ttt) => Base(ttt);
    }

    public class VersionRequestModel : Entities.RequestEntity
    {
   //     [JsonIgnore]
        public string Version { get; set; }
 //       [JsonIgnore]
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
