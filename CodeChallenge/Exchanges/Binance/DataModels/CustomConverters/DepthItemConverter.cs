using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace CodeChallenge.Exchanges.Binance.DataModels.CustomConverters
{
    public class DepthItemConverter : JsonConverter
    {
        public override bool CanConvert(Type aObjectType)
        {
            return aObjectType == typeof(DepthItem);
        }

        public override object ReadJson(JsonReader aReader, Type aObjectType, object aExistingValue, JsonSerializer aSerializer)
        {
            if (aReader.TokenType == JsonToken.Null)
                return null;

            var lArray = JArray.Load(aReader);
            var lDepthItem = (aExistingValue as DepthItem ?? new DepthItem());

            lDepthItem.Price = (decimal)lArray.ElementAtOrDefault(0);
            lDepthItem.Quantity = (decimal)lArray.ElementAtOrDefault(1);

            return lDepthItem;
        }

        public override void WriteJson(JsonWriter aWriter, object aValue, JsonSerializer aSerializer)
        {
            var lDepthItem = (DepthItem)aValue;
            aSerializer.Serialize(aWriter, new[] { lDepthItem.Price, lDepthItem.Quantity });
        }
    }
}
