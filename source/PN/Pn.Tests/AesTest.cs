using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PN.Crypt;

namespace Pn.Tests
{
    [TestClass]
    public class AesTest
    {
        private const string BigBadString = "!@#$%^&*()_+QWERTYUIOP{DFGHJKLCVBNM<1234567890-=qwertyuiopdfghjkcvbnm ?><MNBVCXASDFGHJKL:{POIUYTREWQ!@#$%^&*()";
        private const string Password = "ASDFGHJKL:POIUYTREWE$%^&UJM VCFR%^&UJM BVFGT6y7ujmn vfrt567uJMN BVFT^y7";
        private const string Hash = "0a67be938424ac6658f8cb8b831f987997f9c86cf39a950d57b44666520e2033";
        
        [TestMethod]
        public void TestCrypt()
        {
            var cryptedString = AES.Encrypt(BigBadString, Password);

            var decryptedString = AES.Decrypt(cryptedString, Password);

            Assert.AreNotEqual(BigBadString, cryptedString);

            Assert.AreEqual(BigBadString, decryptedString);
        }
        
        
        
        [TestMethod]
        public void TestHash()
        {
            var generatedHash = AES.SHA256Hash(BigBadString);

            Assert.AreEqual(Hash, generatedHash);
        }
    }
}