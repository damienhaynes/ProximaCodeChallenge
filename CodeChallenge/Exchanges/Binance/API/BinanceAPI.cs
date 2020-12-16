using CodeChallenge.Exchanges.Binance.DataModels;
using CodeChallenge.Exchanges.Binance.DataModels.CustomConverters;
using CodeChallenge.Exchanges.Interfaces;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Text;
using WebSocket4Net;

namespace CodeChallenge.Exchanges.Binance.API
{
    public class BinanceAPI : IExchange
    {
        #region Event Handlers

        public delegate void DepthStreamOpened();
        public delegate void DepthStreamClosed();
        public delegate void DepthStreamError(string aErrorMessage);
        public delegate void DepthStreamMessageReceived(DepthUpdate aDepthUpdate);

        public event DepthStreamOpened OnDepthStreamOpened;
        public event DepthStreamClosed OnDepthStreamClosed;
        public event DepthStreamError OnDepthStreamError;
        public event DepthStreamMessageReceived OnDepthStreamMessageReceived;

        #endregion

        private const string cRestBaseUrl = "https://api.binance.com/api/v3";
        private const string cWebSocketBaseUrl = "wss://stream.binance.com:9443/ws";

        private readonly JsonSerializerSettings mSerialisationSettings;

        #region Constructor

        public BinanceAPI()
        {
            // Set any custom JSON serialisation settings
            mSerialisationSettings = new JsonSerializerSettings
            {
                Converters = new[]
                { 
                    // Convert a string[] array to DepthItem object { Price: decimal, Quantity: decimal }
                    new DepthItemConverter()
                }
            };
        }

        #endregion

        #region IExchange

        string IExchange.Name => "Binance";

        #endregion

        /// <summary>
        /// Request for orderbook market data end-point
        /// https://github.com/binance/binance-spot-api-docs/blob/master/rest-api.md#order-book
        /// </summary>
        /// <param name="aSymbol">Symbol for depth snapshot request</param>
        /// <param name="aRecordLimit">Maximum number of levels to return in depth. Default 100; Max 5000. Valid limits:[5, 10, 20, 50, 100, 500, 1000, 5000]</param>
        /// <returns><see cref="DepthSnapshot"/></returns>
        public DepthSnapshot GetDepthSnapshot(string aSymbol, ushort aRecordLimit = 100)
        {
            // check if we have valid record limit (this is Binance specific)
            ushort[] lValidDepthLimits = { 5, 10, 20, 50, 100, 500, 1000, 5000 };
            if (!lValidDepthLimits.Contains(aRecordLimit))
            {
                throw new ArgumentOutOfRangeException(nameof(aRecordLimit), "Invalid record limit, Allowed values are 5, 10, 20, 50, 100, 500, 1000, 5000");
            }

            string lUrl = $"{cRestBaseUrl}/depth?symbol={aSymbol.ToUpper()}&limit={aRecordLimit}";
            return DownloadJson<DepthSnapshot>(lUrl, mSerialisationSettings);
        }

        /// <summary>
        /// Order book price and quantity depth updates used to locally manage an order book
        /// </summary>
        /// <param name="aSymbol">Symbol to request for depth stream</param>
        /// <param name="aUpdateSpeed">Update Speed in milliseconds, 1000ms or 100ms (default 1000ms)</param>
        public void OpenDepthStream(string aSymbol, ushort aUpdateSpeed = 1000)
        {
            string lUri = $"{cWebSocketBaseUrl}/{aSymbol.ToLower()}@depth@{aUpdateSpeed}ms";

            var lWebsocket = new WebSocket(lUri);
            
            // Subscribe to stream events
            // Send updates to any listeners  
            lWebsocket.Opened += (aSender, aEventArgs) =>
            {
                OnDepthStreamOpened?.Invoke();
            };

            lWebsocket.Error += (aSender, aEventArgs) =>
            {
                OnDepthStreamError?.Invoke(aEventArgs.Exception.Message);
            };

            lWebsocket.Closed += (aSender, aEventArgs) =>
            {
                OnDepthStreamClosed?.Invoke();
            };

            lWebsocket.MessageReceived += (aSender, aEventArgs) =>
            {
                var lDepthUpdate = JsonConvert.DeserializeObject<DepthUpdate>(aEventArgs.Message, mSerialisationSettings);
                OnDepthStreamMessageReceived?.Invoke(lDepthUpdate);
            };

            // open socket
            lWebsocket.Open();
        }

        /// <summary>
        /// Downloads a resource as a string (JSON) and deserialises as an object of Type T
        /// </summary>
        /// <typeparam name="T">Object to deserialise JSON string</typeparam>
        /// <param name="aUrl">Url to request</param>
        /// <param name="aSettings">Custom JSON serialiser settings</param>
        private T DownloadJson<T>(string aUrl, JsonSerializerSettings aSettings = null)
        {
            string lResponse = null;

            using (var lWebClient = new WebClient())
            {
                try
                {
                    lResponse = lWebClient.DownloadString(aUrl);
                }
                catch(WebException aWebException)
                {
                    using var reader = new System.IO.StreamReader(aWebException.Response.GetResponseStream(), Encoding.UTF8);

                    var lErrorObject = Newtonsoft.Json.Linq.JObject.Parse(reader.ReadToEnd());
                    var lErrorCode = (int)lErrorObject["code"];
                    var lErrorMessage = (string)lErrorObject["msg"];

                    throw new Exception($"Failed to download resource from '{aUrl}'. Code = '{lErrorCode}', Reason = '{lErrorMessage}'");
                }
            }

            // return deserialised JSON
            return JsonConvert.DeserializeObject<T>(lResponse, aSettings);
        }
    }
}
