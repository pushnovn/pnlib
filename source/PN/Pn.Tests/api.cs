using System.Threading.Tasks;
using PN.Network;

namespace Pn.Tests
{
    [Url("https://pushnovn.com:4001/api/")]
    public class api : HTTP
    {
        public class v1
        {
            [ContentType(ContentTypes.JSON)]
            [Url("Foods?OrderBy={OrderBy}")]
            public static FoodsList GetFoods(QueryParametersRequestModel req) => Base(req);


            [ContentType(ContentTypes.JSON)]
            [Url("Foods?OrderBy={OrderBy}")]
            public static Task<FoodsList> GetFoodsAsync(QueryParametersRequestModel req) => Base(req);


            [RequestType(RequestTypes.POST)]
            [ContentType(ContentTypes.JSON)]
            [Url("Foods")]
            public static FoodResponse PostFoods(FoodRequest req) => Base(req);


            [RequestType(RequestTypes.POST)]
            [ContentType(ContentTypes.JSON)]
            [Url("Foods")]
            public static Task<FoodResponse> PostFoodsAsync(FoodRequest req) => Base(req);


            [ContentType(ContentTypes.JSON)]
            [Url("Foods/{id}")]
            public static Task<FoodResponse> GetFoodsById(FoodRequest req) => Base(req);


            [ContentType(ContentTypes.JSON)]
            [RequestType(RequestTypes.DELETE)]
            [Url("Foods/{id}")]
            public static Task<Entities.ResponseEntity> DeleteFood(FoodRequest req) => Base(req);
        }
    }
}