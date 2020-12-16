using CommandLine;

namespace CodeChallenge
{
    /// <summary>
    /// Options to be bound from command line arguments
    /// Quantity is the only required input argument, and is the first and only positional option
    /// Example minimum command line usage: 'CodeChallenge 100.0'
    /// </summary>
    internal class Options
	{
		public enum TradeSide
		{
			Buy,
			Sell
		}

		[Option('m', "maxupdatespeed", Default = false, HelpText = " " )]
		public bool MaxUpdateSpeed { get; set; }

		[Option('t', "side", Default = TradeSide.Buy, HelpText = "Side of order used to calculate average execution price basis input quantity. By default assumes trader 'buys' X bitcoin." )]
		public TradeSide Side { get; set; }

		[Option('s', "symbol", Default = "BTCUSDT", HelpText = "Symbol used to create a local copy of the market.")]
		public string Symbol { get; set; }

		[Option('l', "depthlimit", Default = (ushort)100, HelpText = "Number of records to request on depth endpoint. Valid Limits = { 5, 10, 20, 50, 100, 500, 1000, 5000 }")]
		public ushort DepthLimit { get; set; }

		[Value(0, MetaName = "quantity", HelpText = "Decimal quantity of BTC to use for the weighted pricing e.g. 100.0.", Required = true)]
		public decimal Quantity { get; set; }
	}

}
