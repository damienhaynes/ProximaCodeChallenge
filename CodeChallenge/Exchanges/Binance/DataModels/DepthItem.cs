using Newtonsoft.Json;

namespace CodeChallenge.Exchanges.Binance.DataModels
{
    /// <summary>
    /// Represents a single depth item from a string[] array
    /// where the first item in the array is the Price and the second the Qty
    /// </summary>
    [JsonArray]
    public class DepthItem
    {
        /// <summary>
        /// First element in the string array represents the price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Seconds element in the string array represents the quantity
        /// </summary>
        public decimal Quantity { get; set; }

        public override string ToString()
        {
            return $"{Quantity}@{Price}";
        }
    }
}
