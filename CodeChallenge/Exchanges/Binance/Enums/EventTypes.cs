using System.Runtime.Serialization;

namespace CodeChallenge.Exchanges.Binance.Enums
{
    /// <summary>
    /// Event Types returned in detailed information streams
    /// https://github.com/binance-exchange/binance-official-api-docs/blob/master/web-socket-streams.md#detailed-stream-information
    /// NB: When deserialising JSON, Newtonsoft will convert to a corresponding enum value if there is a case-insensitive match (by default)
    /// </summary>
    public enum EventTypes
    {
        [EnumMember(Value = "aggTrade")]
        AggregateTrade,
        
        Trade,
        Kline,

        [EnumMember(Value = "24hrMiniTicker")]
        SymbolMiniTicker,

        [EnumMember(Value = "24hrTicker")]
        SymbolTicker,

        DepthUpdate
    }
}
