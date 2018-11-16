//using Newtonsoft.Json;
using PN.Network;
using PN.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static PN.Network.HTTP;
using static PN.Network.HTTP.Entities;
using static PN.Storage.ExportЁr;

namespace ConsoleApp1
{
    class Program
    {
        #region

        [SQLite.SQLiteName("Value_")]
        private class Value
        {
            public int Id { get; set; }

            public string Valuesss { get; set; }

            public List<string> ListV { get; set; }
        }
        
  //      [PN.Storage.New.SQLite.SQLiteName("Posts")]
        class PostSingle
        {
            public int id { get; set; }
            public string Text { get; set; }
            public User Author { get; set; }
        }

   //     [PN.Storage.New.SQLite.SQLiteName("Posts")]
        class PostMulti
        {
            public int id { get; set; }
            public string Text { get; set; }
            public List<User> Authors { get; set; }
        }

        #endregion
        

        public class Post
        {
            public int id { get; set; }
            public string Text { get; set; }
            public User Author { get; set; }
            public List<Comment> Comments { get; set; }
        }
        

        public class Comment
        {
            public int id { get; set; }
            public string Text { get; set; }
            public Post SourcePost { get; set; }
            public User Author { get; set; }
        }

        public class User
        {
            public int id { get; set; }
            public string Name { get; set; }
            public List<Post> Posts { get; set; }
            public List<Comment> Comments { get; set; }
        }

        [ExportName("Список рейсов")]
        public class Air
        {
            [ExportName("Номер рейса")]
            public string Number { get; set; }

            [ExportName("Количество пассажиров")]
            public int PassengersCount { get; set; }

            public string PlaneType { get; set; }

            [ExportName("Аэропорт отправления")]
            public string AirportFrom { get; set; }

            public string AirportTo { get; set; }
        }


        static void Main(string[] args)
        {
            var testColl = new ObservableCollection<string>()
                //.ToList()
                ;
            testColl.Add("111");
            SSS3.TestCollection = testColl;


            Console.WriteLine(SSS3.TestCollection.Count);

            SSS3.TestCollection.Add("123");

            var lolol___ = SSS3.TestCollection;
            lolol___.Add("777");
            lolol___.Add("888");



            var tttttttt = SSS3.TestCollection;
            Console.WriteLine(SSS3.TestCollection.Count);

            tttttttt.Add("ssss");
            Console.WriteLine(SSS3.TestCollection.Count);
            //SSS3.TestCollection.Add("456");
            //SSS3.TestCollection.Add("789");

            //var lolol = SSS3.TestCollection;

            //SSS3.TestCollection.Clear();



            Console.ReadKey();




            var air1 = new Air()
            {
                Number = "BDF3546",
                PassengersCount =73,
                PlaneType = "Boeng",
                AirportFrom = "Minsk-1",
                AirportTo = "Kiev-2",
            };

            var air2 = new Air()
            {
                Number = "NNU432",
                PassengersCount = 121,
                PlaneType = "Airbus A-380",
                AirportFrom = "London-3",
                AirportTo = "Borispol",
            };

            var withHeader = false;


            var byteArray1 = PN.Storage.ExportЁr.ToXLSX(withHeader, air1, air2, new List<Air>() { air1, air2 });
            var byteArray2 = PN.Storage.ExportЁr.ToPDF(withHeader, air1, air2);
            var byteArray3 = PN.Storage.ExportЁr.ToCSV(withHeader, air1, air2);
            var byteArray4 = PN.Storage.ExportЁr.ToXLS(withHeader, air1, air2, new List<Air>() { air1, air2 });
            var byteArray = byteArray2;

            var path1 = "C:\\Temp\\test.xlsx";
            var path2 = "C:\\Temp\\test.pdf";
            var path3 = "C:\\Temp\\test.txt";
            var path4 = "C:\\Temp\\test.xls";
            var path = path2;

            if (File.Exists(path))
                File.Delete(path);

            using (var fileStream = File.OpenWrite(path))
            {
                fileStream.Write(byteArray, 0, byteArray.Length);
            }

       //     Process.Start(path);

            //   var result = PN.Storage.ExportImport.FromXLSX<Air>(byteArray);
            //    var result = PN.Storage.ExportЁr.FromXLS<Air>(false, byteArray);
            var result = PN.Storage.ExportЁr.FromPDF<Air>(withHeader, byteArray);

            //     Console.ReadKey();

            return;




            //PN.Storage.New.SQLite.PathToDB = @"C:\Temp\SQLite\sqlite-test.db";


            //var nodeSingle = PN.Storage.New.SQLite.GenerateTree<User>();




            //var nodeSingle = PN.Storage.New.SQLite.GenerateTree<PostSingle>();
            //var nodeMulti= PN.Storage.New.SQLite.GenerateTree<PostMulti>();



            Console.ReadLine();































            return;


            var search = new PN.Search.Distance();

            var tttp = search.Search("1");


            
            SQLite.PathToDB = "sqlite.db";

            SQLite.PathToDB = "test.db";

            //PN.Network.IBM.MQ.ExtractDll();

            var tables = SQLite.Tables;




            var test_exec1 = SQLite.ExecuteString<Value>("SELECT * FROM Value;");
            var test_exec2 = SQLite.ExecuteString("SELECT * FROM Value;", typeof(Value));

            var vall = new Value() { Valuesss = "TEST_Vall_SOLO", ListV = new List<string>() { "v1", "v2", "v3" } };

            var lsst = new List<Value>()
            {
                new Value() { Valuesss = "TEST_Valll1", ListV = new List<string>() { "v1", "v2", "v3" } },
                new Value() { Valuesss = "TEST_Valll2", ListV = new List<string>() { "v1", "v2", "v3" } },
                new Value() { Valuesss = "TEST_Valll3", ListV = new List<string>() { "v1", "v2", "v3" } },
                new Value() { Valuesss = "TEST_Valll4", ListV = new List<string>() { "v1", "v2", "v3" } },
            };

            //      var test = SQLite.WhereAND(nameof(Value.Id), Is.In, new List<int>() { 0, 1, 2, 3 }, 4).Delete<Value>(lsst, 2, vall,3,4, lsst, 5, vall, 6);

            var test = SQLite
           //     .WhereAND(nameof(Value.Id), Is.In, new List<int>() { 0, 1, 2, 3 }, 4)
                .Delete(lsst, 2, vall,3,4, lsst, 5, vall, 6);

            var test2 = SQLite.Delete(
                new Value() { Valuesss = "Valll1", ListV = new List<string>() { "v1", "v2", "v3" } },
                new Value() { Valuesss = "Valll2", ListV = new List<string>() { "v1", "v2", "v3" } },
                new Value() { Valuesss = "Valll3", ListV = new List<string>() { "v1", "v2", "v3" } },
                new Value() { Valuesss = "Valll4", ListV = new List<string>() { "v1", "v2", "v3" } });

            var ssqqCOUNT = SQLite.GetCount<Value>();
            var ssqq = SQLite.Get<Value>();
            if (ssqq == null)
            {
                Console.WriteLine(SQLite.LastQueryException?.ToString());
            }

            try
            {
                ssqq.FirstOrDefault().ListV.Remove("v6");
                ssqq.FirstOrDefault().ListV.Add("v7");
            }
            catch { }

            SQLite.Update(ssqq.ToArray());

            var ssqq2 = SQLite.Get<Value>();

            SQLite//.WhereAND("Title", Is.Equals, "table")
                  .Delete();



            var nws = SQLite.WhereAND(null, Is.LimitedBy, 10)
                            .Where("Title", Is.Equals, "table")
                            .Where("Title", Is.LimitedBy, 20)
                            .Where("Title", Is.Contains, "table")
                            .Where("Title", Is.LessThen, "table")
                            .Where("Title", Is.NotEquals, "table")
                            .Where("Title", Is.Between, "1", "2")
                            .Where("Title", Is.BiggerThen, "table")
                            .Where("Title", Is.Reversed, true)
                            .Where("Title", Is.ContainsAnythingFrom, "1", "2", "3")
                            .Where("Title", Is.Reversed, true)
                            .Where("Title", Is.Reversed, true)
                            .Get<New>();
            

            var nws2 = SQLite.WhereOR(null,  Is.LimitedBy, 10)
                             .Where("Title", Is.Equals, "table")
                             .Where("Title", Is.LimitedBy, 20)
                             .Where("Title", Is.Contains, "table")
                             .Where("Title", Is.LessThen, "table")
                             .Where("Title", Is.NotEquals, "table")
                             .Where("Title", Is.Between, "1", "2")
                             .Where("Title", Is.BiggerThen, "table")
                             .Where("Title", Is.Reversed, true)
                             .Where("Title", Is.ContainsAnythingFrom, "1", "2", "3")
                             .Where("Title", Is.Reversed, true)
                             .Where("Title", Is.Reversed, true)
                             .Get<New>();



            //var superWhere = new SQLite.WhereCondition()
            //                                .Where("type", SQLite.Is.Equals, "table")
            //                                .Where("type", SQLite.Is.Equals, "table")
            //                                .Where("type", SQLite.Is.Equals, "table")
            //                                .Where("type", SQLite.Is.Equals, "table")
            //                                .Where("type", SQLite.Is.Equals, "table")
            //                                .Where("type", SQLite.Is.Equals, "table")
            //                                .Where("type", SQLite.Is.Equals, "table");

        //    var ppp = SQLite.GetWhere<sqlite_master>(superWhere);




            //var ttd = SQLite.Where("type", SQLite.Is.Equals, "table")
            //                .Where("type", SQLite.Is.Contains, "table")
            //                .Where("type", SQLite.Is.LessThen, "table")
            //                .Where("type", SQLite.Is.NotEquals, "table")
            //                .Where("type", SQLite.Is.Between, "table")
            //                .Where("type", SQLite.Is.BiggerThen, "table")
            //                .Where("type", SQLite.Is.In, "table")
            //                .Get<New>();



            SQLite.ExecuteString("SELECT * FROM sqlite_master WHERE type='table';");

    //        SQLite.Set(new Value());

            var values = SQLite.Get<Value>();
            // Assert.NotNull(values);

            var ttt = SQLite.Get<New>();

            ttt[0].Text = null;

            SQLite.Update(ttt[0]);





            var d = SSS2.dict;

            SSS2.Example = "some example str";

            var ses = SSS2.Example;

            SSS2.Index<string, SSS2>()[27] = "some_str2";

            d = SSS2.dict;

            var some_str = SSS2.Index<string, SSS2>()[27];

            d = SSS2.dict;

            var testProp = AS.API___.TestProp;

   


            var typ = AS.API___.Files(new FilesRequestModel()).Result;
            var lastResp = HTTP.LastResponse;
            //var abc = API___.Files(null).Result;

            //    HTTP.DownloadProgressChanged += HTTP_DownloadProgressChanged;
            var fsdfsdf = Request<byte[]>("http://ftp.byfly.by/test/10gb.txt", new RequestEntity() { OnDownloadProgressChangedAction = HTTP_DownloadProgressChanged });


            //    StaticTest2.TestProp = "";
            //    Console.WriteLine(StaticTest2.TestProp);
            //API.Init("http://projects.pushnovn.com", new List<HeaderAttribute>()
            //    {
            //        new HeaderAttribute("ppppppppp", "*****************"),
            //        new HeaderAttribute("hgggggggggg", "sssssssssssssssssssss"),
            //    });

            //var mod = new TestRequsetModel()
            //{
            //    Headers = new List<HeaderAttribute>()
            //    {
            //        new HeaderAttribute("1", "2"),
            //        new HeaderAttribute("3", "4"),
            //    }
            //};

            //    var ppp = API.Testtt.TestAsync(mod).Result;
            //    var ppp = API.Testtt.Test(mod);


            //    Console.WriteLine(ppp.Exception?.ToString() ?? ppp.id);

            //        var ttt = API.Testtt.TestAsync(mod).Result;
            //        var ttt2 = API.Testtt.Test(mod);
            //     var isValT = (new byte[] { 1, 3 }).GetType().IsValueType;

            //     var arr = PN.Utils.Utils.Converters.ToByteArray(12.34);
            //     var dbl = PN.Utils.Utils.Converters.FromByteArray<Double>(arr);

            //     Console.WriteLine($"{12.34} => {dbl}");

            //     StaticTest.Get();



            //     Task.Run(async () =>
            //     {
                  //   var typ = await API___.Files(new FilesRequestModel());

            //     //    var files = JsonConvert.DeserializeObject(respone.ResponseText, typeof(List<ServerFileInfo>));
            //         var teststr = API_VR.TestString(new VersionRequestModel()
            //         {
            //             Token = "p4XsNk3x1WLbmL1WOROBGYEpmN2cjJsXwSSl666Jpe6ZbrUYpWk9z8rnNQFLZVmGeG4TXSef8CvW2GIk3v9y2aLsuAMfgjabYwVbYMLSvLqaOA/bs76wcUxZxYVj4Z4hSlljc2yll5zxTNbIiRqMf2RAKbE5zsVOT56lVrztYNRYDkXzHTNx8XecTUZ3a+RpQJdbifcgVkJJsM3Ze0gePg==",
            //             Version = "123",
            //         });

            //         var testint = API_VR.TestInt(new VersionRequestModel()
            //         {
            //             Token = "p4XsNk3x1WLbmL1WOROBGYEpmN2cjJsXwSSl666Jpe6ZbrUYpWk9z8rnNQFLZVmGeG4TXSef8CvW2GIk3v9y2aLsuAMfgjabYwVbYMLSvLqaOA/bs76wcUxZxYVj4Z4hSlljc2yll5zxTNbIiRqMf2RAKbE5zsVOT56lVrztYNRYDkXzHTNx8XecTUZ3a+RpQJdbifcgVkJJsM3Ze0gePg==",
            //             Version = "123",
            //         });

            //         var test00 = await API_VR.Version(new VersionRequestModel()
            //         {
            //             Token = "p4XsNk3x1WLbmL1WOROBGYEpmN2cjJsXwSSl666Jpe6ZbrUYpWk9z8rnNQFLZVmGeG4TXSef8CvW2GIk3v9y2aLsuAMfgjabYwVbYMLSvLqaOA/bs76wcUxZxYVj4Z4hSlljc2yll5zxTNbIiRqMf2RAKbE5zsVOT56lVrztYNRYDkXzHTNx8XecTUZ3a+RpQJdbifcgVkJJsM3Ze0gePg==",
            //             Version = "123",
            //         });

            //         var test0 = await API_test.GetRequestInfo(new VersionRequestModel()
            //         {
            //             Token = "p4XsNk3x1WLbmL1WOROBGYEpmN2cjJsXwSSl666Jpe6ZbrUYpWk9z8rnNQFLZVmGeG4TXSef8CvW2GIk3v9y2aLsuAMfgjabYwVbYMLSvLqaOA/bs76wcUxZxYVj4Z4hSlljc2yll5zxTNbIiRqMf2RAKbE5zsVOT56lVrztYNRYDkXzHTNx8XecTUZ3a+RpQJdbifcgVkJJsM3Ze0gePg==",
            //             Version = "123",
            //         });

            //         var test01 = await API_test_2.GetImage(new VersionRequestModel()
            //         {
            //             Token = "p4XsNk3x1WLbmL1WOROBGYEpmN2cjJsXwSSl666Jpe6ZbrUYpWk9z8rnNQFLZVmGeG4TXSef8CvW2GIk3v9y2aLsuAMfgjabYwVbYMLSvLqaOA/bs76wcUxZxYVj4Z4hSlljc2yll5zxTNbIiRqMf2RAKbE5zsVOT56lVrztYNRYDkXzHTNx8XecTUZ3a+RpQJdbifcgVkJJsM3Ze0gePg==",
            //             Version = "123",
            //         });

            //         var ttt = Convert.ToBase64String(test01.ResponseBody);

            //  //       Console.WriteLine(ttt);
            //         //StreamReader reader = new StreamReader(test0.ResponseStream);
            //         //string text = reader.ReadToEnd();

            ////         var yy =  JsonConvert.DeserializeObject(null, typeof(TestModel));

            //         var tt = new List<string>() { "", "" };
            //         var t = tt.IndexOf(null);

            //         var test1 = API.TestClass.Test(mod);

            //         var test2 = await API.TestClass.TestAsync(mod);



            //         var ppp = API.Testtt.Test(mod);
            //         Console.WriteLine("Sync: " + (ppp.Exception?.ToString() ?? ppp.id));

            //         ppp = await API.Testtt.TestAsync(mod);
            //         Console.WriteLine("Async: " + (ppp.Exception?.ToString() ?? ppp.id));

            //     });

        }

        public class New
        {
            public int Id { get; set; }
          
            //  [SQLite.SQLiteName("Title")]
            [SQLite.SQLiteIgnore]
            public string Titl { get; set; }

            public string Text { get; set; }
        }

        static int count = 0;
        static long val = 0;

        static List<byte> list = new List<byte>();

        private static void HTTP_DownloadProgressChanged(DownloadProgressChangedEventArgs e) => HTTP_DownloadProgressChanged(null, e);
        private static void HTTP_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            val += e.RecievedBytesCount;
            Console.Write($"\r{GetFriendlyLength((ulong)e.TotalRecievedBytesCount)} / {GetFriendlyLength((ulong)e.ResponseBodyLength)} bytes read {GetFriendlyLength((ulong)(val / ++count))}");
        }

        private static string GetFriendlyLength(ulong bandwidth)
        {
            var ordinals = new[] { string.Empty, "K", "M", "G", "T", "P", "E" };


            decimal rate = (decimal)bandwidth;

            var ordinal = 0;

            while (rate > 1024)
            {
                rate /= 1024;
                ordinal++;
            }

            return (string.Format("{0} {1}B", Math.Round(rate, 2, MidpointRounding.AwayFromZero), ordinals[ordinal]));
        }

    }






    public class StaticTest
    {
        public static string Get(string ooo = null)
        {
            var res = HTTP.Request<byte[]>("https://www.google.by/images/branding/googlelogo/2x/googlelogo_color_120x44dp.png");

                //,new HeaderAttribute[2] { new HeaderAttribute("1k", "1v"), new HeaderAttribute("2k", "2v") },
                //new List<HeaderAttribute> { new HeaderAttribute("1kLLL", "1vLLL"), new HeaderAttribute("2kLLL", "2vLLL") }


            var pict = Convert.ToBase64String(res);

            Console.WriteLine(pict);
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();


            var ppp = AS.API___.DownloadGoogleLogo();

            pict = Convert.ToBase64String(ppp);
            Console.WriteLine(pict);
            Console.ReadLine();


            var pp = AS.API___.FilesTest(new FilesRequestModel()).Result;




            var arr = new byte[1];

            var str = PN.Utils.Converters.BytesToString(arr);

            var bewarr = PN.Utils.Converters.StringToBytes("");


            PN.Utils.Debug.CalculateMethodTimeExecution(() =>
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

#if DEBUG
            var ch1 = SSS3.CheckPassword<SSS3>("some auth pass");
            var ch2 = SSS3.CheckPassword<SSS3>("some auth pass 2");
#endif
            SSS3.ExampleTestReCrypt = new TestModel() { id = "ExampleTestReCrypt" };

            SSS3.ExampleTest3Model = new TestModel() { id = "IDDD" };

            SSS3.UpdatePasswordAndReCrypt<SSS3>("some new cryptostring");
#if DEBUG
            var ch3 = SSS3.CheckPassword<SSS3>("some new cryptostring");
            var ch4 = SSS3.CheckPassword<SSS3>("some auth pass");
#endif
            var testExampleTestReCrypt = SSS3.ExampleTestReCrypt;





            var uio = PN.Utils.Converters.NumberToShortFormattedString(8);






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
            PN.Utils.Debug.CalculateMethodTimeExecution(() =>
            {
                ttt = new StackTrace().GetFrame(3).GetMethod();
            }, "single");
            ttt = new StackTrace().GetFrame(1).GetMethod();
            var ssp = ttt.GetParameters();
            return ttt.ReflectedType.FullName + " :: " + ttt.Name;

        }
    }











    public class AS
    {
        //[Url("http://videoreg.pushnovn.com:1583/api/Files?Version=%7BVersion%7D&Token=jICmeDBf2e2vyfCkzlI87P1eG/PIQFjFeanVrXxgj7nr+o7T4LiNYWArvbOU3SrCIrcc2aAjNN4zIA6LgJmMmtfyGm/1SShlVZ7cStft6LblcjXg3Q0CIMkdSUtQ5sQy44WWoU4X7KLC3oiRNbyg4sJeGGta3qmxEK+v7VXTJmQ06R5yRCpF27LANQ8YVT4AXAK")]
        // [Url("http://videoreg.pushnovn.com:1583/")]
        [Url("https://www.google.by/images/branding/googlelogo/2x/")]
        public class API___ : HTTP
        {
            [UserAgent("sdfsghfj")]
            [Url("googlelogo_color_120x44dp.png")]
            public static byte[] TestProp => Base();

            [RequestType(RequestTypes.GET)]
            [Url("kE5lGUznLKEiGvGFig==")]
            public static Task<List<ServerFileInfo>> Files(FilesRequestModel ttt) => Base(ttt);

            [Url("kE5lGUznLKEiGvGFig==")]
            public static Task<FilesResponseModel> FilesTest(FilesRequestModel ttt) => Base(ttt);

            [Url("googlelogo_color_120x44dp.png")]
            public static byte[] DownloadGoogleLogo() => Base();
        }
    }

   

    public class FilesRequestModel : PN.Network.HTTP.Entities.RequestEntity
    {
        //[JsonIgnore]
        //public string Version { get; set; }
        //[JsonIgnore]
        //public string Token { get; set; }

    }
    public class FilesResponseModel : PN.Network.HTTP.Entities.ResponseEntity
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

        public static ObservableCollection<string> TestCollection {

        //   [MethodImpl(MethodImplOptions.NoInlining)]
            get => Base();

       //     [MethodImpl(MethodImplOptions.NoInlining)]
            set => Base(value); }
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