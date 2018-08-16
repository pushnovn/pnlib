using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PN.Network;

namespace Pn.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            for (int i = 0; i < 20; i++)
            {
                var d = await api.v1.DeleteFood(new FoodCreateResponse { id = i });
            }
            var foods = api.v1.GetFoods(new QueryParametersRequestModel { OrderBy = nameof(Food.Id) });
            //DELETE action and GET(sync) action
            Assert.AreEqual(foods.value.Count, 0);



            var newFood = new FoodCreateRequest { calories = 12, created = DateTime.UtcNow, name = "MakaroniAsync", type = "someoneType" };
            var createasync = await api.v1.PostFoodsAsync(newFood);
            //POST action
            Assert.AreEqual(createasync.created, newFood.created);


            var foodByID = await api.v1.GetFoodsByID(new FoodCreateResponse() { id = createasync.id });
            // GET ACTION
            Assert.AreEqual(newFood.created, foodByID.Created);


            var FoodsFromServer = api.v1.GetFoodsAsync(new QueryParametersRequestModel { OrderBy = nameof(Food.Id) }).Result;
            // GET LIST action
            Assert.AreEqual(FoodsFromServer.value.Count, 1);


        }
    }


    [Url("https://pushnovn.com:5001/api/")]
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
            public static FoodCreateResponse PostFoods(FoodCreateRequest req) => Base(req);



            [RequestType(RequestTypes.POST)]
            [ContentType(ContentTypes.JSON)]
            [Url("Foods")]
            public static Task<FoodCreateResponse> PostFoodsAsync(FoodCreateRequest req) => Base(req);


            [ContentType(ContentTypes.JSON)]
            [Url("Foods/{id}")]
            public static Task<Food> GetFoodsByID(FoodCreateResponse req) => Base(req);


            [ContentType(ContentTypes.JSON)]
            [RequestType(RequestTypes.DELETE)]
            [Url("Foods/{id}")]
            public static Task<Entities.ResponseEntity> DeleteFood(FoodCreateResponse req) => Base(req);
        }


    }



}
