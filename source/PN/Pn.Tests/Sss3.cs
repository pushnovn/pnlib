using System.Collections.Generic;
using PN.Storage;

namespace Pn.Tests
{
    public class Sss3 : SSS
    {
        public static TestModel ExampleSet
        {
            get => Base();
            set => Base(value);
        }

        [NeedAuth]
        public static TestModel ExampleSet2
        {
            get => Base();
            set => Base(value);
        }

        private static readonly Dictionary<string, string> Dict = new Dictionary<string, string>();
        protected override string Get(string key) => Dict.ContainsKey(key) ? Dict[key] : null;
        protected override void Set(string key, string value) => Dict[key] = value;
    }
}