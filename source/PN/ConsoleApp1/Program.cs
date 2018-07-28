using PN.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

                var ppp = API.Testtt.Test(mod);
                Console.WriteLine("Sync: " + (ppp.Exception?.ToString() ?? ppp.id));

                ppp = await API.Testtt.TestAsync(mod);
                Console.WriteLine("Async: " + (ppp.Exception?.ToString() ?? ppp.id));

            });
        //    Debug.WriteLine(ttt.Exception);
        while (true)
            Console.ReadLine();
        }
    }

    [Url("http://projects.pushnovn.com/")]
  //          [WWWW_Custom]
    class API : HTTP
    {
    //    [Url("test/{yyy}")]
        [Url("test")]
        public class Testtt
        {
            [Header("aaaaaa", "bbbbbbbbbbbbbbb")]
            [IgnoreGlobalHeaders]
            [Url("")]
            public static Task<TestModel> TestAsync(RequestEntity ttt) => Base(ttt);

            [Header("aaaaaa", "bbbbbbbbbbbbbbb")]
            [IgnoreGlobalHeaders]
            [Url("")]
            public static TestModel Test(RequestEntity ttt) => Base(ttt);
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
