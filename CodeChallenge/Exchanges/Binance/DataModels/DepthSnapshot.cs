using Newtonsoft.Json;
using System.Collections.Generic;

namespace CodeChallenge.Exchanges.Binance.DataModels
{
    /// <summary>
    /// Model representing JSON message received from a Depth Snapshot
    /// https://github.com/binance-exchange/binance-official-api-docs/blob/master/web-socket-streams.md#how-to-manage-a-local-order-book-correctly
    /// </summary>
    public class DepthSnapshot
    {
        [JsonProperty("lastUpdateId")]
        public ulong LastUpdateId { get; set; }

        /// <summary>
        /// List of bids for requested limit
        /// </summary>
        [JsonProperty("bids")]
        public List<DepthItem> Bids { get; set; }

        /// <summary>
        /// List of asks for requested limit
        /// </summary>
        [JsonProperty("asks")]
        public List<DepthItem> Asks { get; set; }
    }
}
