using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pn.Tests
{
    [TestClass]
    public class UnitTest1
    {
        private static readonly FoodRequest NewFood = new FoodRequest
        {
            id = 0,
            calories = 12,
            created = DateTime.UtcNow,
            name = "MakaroniAsync",
            type = "someoneType"
        };

        private static FoodResponse _createasync;


        [TestMethod]
        public async Task TestDelete()
        {
            for (var i = 0; i < 20; i++)
            {
                await api.v1.DeleteFood(new FoodRequest() {id = i});
            }

            var foods = api.v1.GetFoods(new QueryParametersRequestModel {OrderBy = nameof(Food.Id)});
            //DELETE action and GET(sync) action
            Assert.AreEqual(foods.value.Count, 0);
        }

        [TestMethod]
        public async Task TestPostAndGet()
        {
            _createasync = await api.v1.PostFoodsAsync(NewFood);
            //POST action
            Assert.AreEqual(_createasync.HttpCode, HttpStatusCode.Created);
            Assert.AreEqual(_createasync.created, NewFood.created);
            
            
            var foodById = await api.v1.GetFoodsById(new FoodRequest() {id = _createasync.id});
            // GET ACTION
            Assert.AreEqual(foodById.HttpCode, HttpStatusCode.OK);
            Assert.AreEqual(NewFood.created, foodById.created);
        }

        [TestMethod]
        public async Task TestListGet()
        {
            var foodsFromServer =
                await api.v1.GetFoodsAsync(new QueryParametersRequestModel {OrderBy = nameof(Food.Id)});

            // GET LIST action
            Assert.AreEqual(foodsFromServer.value.Count, 1);
        }
    }
}