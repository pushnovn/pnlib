using System;

namespace SampleWebApiAspNetCore.Entities
{
    /// <summary>
    /// This Is Food Item from me
    /// </summary>
    public class FoodItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int Calories { get; set; }
        public DateTime Created { get; set; }
    }
}
