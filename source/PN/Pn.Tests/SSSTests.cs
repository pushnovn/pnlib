using Microsoft.VisualStudio.TestTools.UnitTesting;
using PN.Storage;

namespace Pn.Tests
{
    [TestClass]
    public class SssTests
    {
        private const string PasswordOne = "ExampleOfPassword";
        private const string PasswordTwo = "AnotherPassword";

        private const string First = "ExampleTestReCrypt";
        private const string Second = "IDDD";
        
        
        [TestMethod]
        public void SssCheckAuth()
        {
            SSS.Auth<Sss3>(PasswordOne);

            Sss3.ExampleSet = new TestModel() {Value = First};

            Sss3.ExampleSet2 = new TestModel() {Value = Second};


            var get1 = Sss3.ExampleSet;
            Assert.AreEqual(get1.Value, First);

            var get2 = Sss3.ExampleSet2;
            Assert.AreEqual(get2.Value, Second);
        }
        
        [TestMethod]
        public void SssCheckReauth()
        {
            SSS.UpdatePasswordAndReCrypt<Sss3>(PasswordTwo);
            
            var get3 = Sss3.ExampleSet;
            Assert.AreEqual(get3.Value, First);

            var get4 = Sss3.ExampleSet2;
            Assert.AreEqual(get4.Value, Second);
        }
    }


    public class TestModel
    {
        public string Value { get; set; }
    }
}