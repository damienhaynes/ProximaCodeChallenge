using CodeChallenge.Exchanges.Binance.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace CodeChallenge.Exchanges.Binance.DataModels
{
    /// <summary>
    /// Model representing JSON message received from Diff. Depth Stream
    /// https://github.com/binance-exchange/binance-official-api-docs/blob/master/web-socket-streams.md#diff-depth-stream
    /// </summary>
    public class DepthUpdate
    {
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("e")]
        public EventTypes EventType { get; set; }
 
        [JsonProperty("E")]
        public ulong EventTime { get; set; }

        [JsonProperty("s")]
        public string Symbol { get; set; }

        [JsonProperty("U")]
        public ulong FirstUpdateId { get; set; }

        [JsonProperty("u")]
        public ulong LastUpdateId { get; set; }

        /// <summary>
        /// List of bids updated
        /// </summary>
        [JsonProperty("b")]
        public List<DepthItem> BidsUpdated { get; set; }

        /// <summary>
        /// List of asks updated
        /// </summary>
        [JsonProperty("a")]
        public List<DepthItem> AsksUpdated { get; set; }
    }
}
