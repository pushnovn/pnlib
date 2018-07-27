using PN.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PN.Network.HTTP;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            API.Init("http://site.WWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWW", new List<HeaderAttribute>()
                {
                    new HeaderAttribute("!!!!!", "*****************"),
                    new HeaderAttribute("=============", "sssssssssssssssssssss"),
                });
            var ttt = API.A.B.C.ASD(new Entities.RequestEntity()
            {
                Headers = new List<HeaderAttribute>()
                {
                    new HeaderAttribute("1", "2"),
                    new HeaderAttribute("3", "4"),
                }
            });
            Debug.WriteLine(ttt.Exception);
        }
    }

    [Url("http://site.0000000000")]
    class API : HTTP
    {
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
                    public static Entities.ResponseEntity ASD(Entities.RequestEntity ttt) => Base(ttt);
                }
            }
        }
    }
}
