using System;
using System.Diagnostics;
using System.Text.RegularExpressions ;

namespace WordsMatching
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Test
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			//
			// TODO: Add code to start application here
			//
			Test t=new Test() ;			
		}

		public Test()
		{
            var lst = "WordsMatching.csproj: Your project is targeting .NET Framework 2.0 or 3.0. If your project uses assemblies requiring a newer .NET Framework, your project will fail to build. You can change the .NET Framework version by clicking Properties on the project menu and then selecting a new version in the '.NET Framework' dropdown box. (In Visual Basic, this is located on the Compile tab by clicking the 'Advanced Compiler Options...' button.)";
            var str = lst.ToLower();

            foreach(var s in new string[] { "Words", "peoject", "MWR" })
            Console.WriteLine($"{s} => {new MatchsMaker(s, str).Score}");
            Console.ReadKey();





			string s1="The main plication. pplication pplication pplication pplication pplication pplication pplication ";
			string s2="Add code to start application here pplication pplication pplication pplication pplication pplication pplication pplication pplication pplication pplication ";
			//Entertaiment Entail tail, fail, enter men end			
			s1="ZONGSHyryyyyyyyyyyyyyyyyyyyyyyyrtEN F 200ytrrrrrrrrrrrrrrrrrrr8 LZSJCJLytrrrrrrrrrrrrryrtyrt0645 50 0 00ytrrrrrrrrrrrrryyhjjjjjjjjjjjjjjjjj124 00144 000285 002 ZONGSHyryyyyyyyyyyyyyyyyyyyyyyyrtEN F 200ytrrrrrrrrrrrrrrrrrrr8 LZSJCJLytrrrrrrrrrrrrryrtyrt0645 50 0 00ytrrrrrrrrrrrrryyhjjjjjjjjjjjjjjjjj124 00144 000285 002ZONGSHyryyyyyyyyyyyyyyyyyyyyyyyrtEN F 200ytrrrrrrrrrrrrrrrrrrr8 LZSJCJLytrrrrrrrrrrrrryrtyrt0645 50 0 00ytrrrrrrrrrrrrryyhjjjjjjjjjjjjjjjjj124 00144 000285 002ZONGSHyryyyyyyyyyyyyyyyyyyyyyyyrtEN F 200ytrrrrrrrrrrrrrrrrrrr8 LZSJCJLytrrrrrrrrrrrrryrtyrt0645 50 0 00ytrrrrrrrrrrrrryyhjjjjjjjjjjjjjjjjj124 00144 000285 002";
			s2="ALFA ALFAROMERO145TDfdsfsdgfdgdfgggggggggggggggggggggggggggggggfsdfsddddddsdfdsfsdfsdfdsfsdfdsCAT ZAR93000002 40 1 01929  001730 005 0 00ytrrrrrrrrrrrrryyhjjjjjjjjjjjjjjjjj124 00144 000285 002005 0 00ytrrrrrrrrrrrrryyhjjjjjjjjjjjjjjjjj124 00144 000285 002005 0 00ytrrrrrrrrrrrrryyhjjjjjjjjjjjjjjjjj124 00144 000285 002005 0 00ytrrrrrrrrrrrrryyhjjjjjjjjjjjjjjjjj124 00144 000285 002005 0 00ytrrrrrrrrrrrrryyhjjjjjjjjjjjjjjjjj124 00144 000285 002005 0 00ytrrrrrrrrrrrrryyhjjjjjjjjjjjjjjjjj124 00144 000285 002005 0 00ytrrrrrrrrrrrrryyhjjjjjjjjjjjjjjjjj124 00144 000285 002";
//			s1="2 JALAN PEMBERITA U1/49 \nTEMASYA INDUSTRIAL PARK \nGLENMARIE 40150 SHAH ALAM SEL";
//			s2="D-3-2 ARCADIA APARTMENT \nUSJ 11/1 SUBANG JAYA \n47600 PETALING JAYA";
			s1="ZONGSHEN F 2008 LZSJCJL0645 50 0 00124 00144 000285 002";
			s2="ALFA ALFAROMERO145TDCAT ZAR93000002 40 1 01929 001730 005";
//			//s1="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc dddddddddddddddddddddddddddddddd";
//			//s2="a b c d" ;

			//s1="Entertaiment Entail tail, enter fail, use enter men end Detail	";
			//s2="Retail detail entry point fai mention lure mention find" ;

//			s1="_incoming indicate the incomming left nodes of right nodes set";
//			s2="_outgoing indicate the outgoing right nodes of left nodes set";
//			s1="carty";
//			s2="auto";

			MatchsMaker match=new MatchsMaker(s1, s2) ;
			Trace.WriteLine(match.Score) ;			
		}

	}
}
