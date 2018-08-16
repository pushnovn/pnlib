using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PN.Network.HTTP.Entities;

namespace Pn.Tests
{


    public class Link
    {
        public string href { get; set; }
        public string rel { get; set; }
        public string method { get; set; }
    }

    public class Food
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public object Type { get; set; }
        public int Calories { get; set; }
        public DateTime Created { get; set; }
        public List<Link> links { get; set; }
    }
    public class FoodsList
    {
        public List<Food> value { get; set; }
        public List<Link> links { get; set; }
    }


    public class QueryParametersRequestModel : RequestEntity
    {
        public int Page { get; set; } = 1;

        public int PageCount { get; set; } = 1;

        public string Query { get; set; }

        public string OrderBy { get; set; } = "Name";
    }


    public class FoodRequest : RequestEntity
    {
        public string name { get; set; }
        public long id { get; set; }

        public string type { get; set; }
        public int calories { get; set; }
        public DateTime created { get; set; }
    }

    public class FoodResponse : ResponseEntity
    {
        public long id { get; set; }

        public string name { get; set; }

        public string type { get; set; }
        public int calories { get; set; }
        public DateTime created { get; set; }
    }
}
