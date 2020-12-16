using CodeChallenge.Clients.Binance;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeChallenge
{
    class Program
    {
        private static Options mProgramOptions;
        private static int mPreviousOutputLength = 0;

        /// <summary>
        /// Entry point to the program, see <see cref="Options"/> for accepted arguments
        /// </summary>
        private static void Main(string[] aArguments)
        {
            Console.WriteLine("Press the <Enter> key to exit at any time.");

            // parse command line arguments, if in error, help will be printed
            if (!ParseCommandLineArgs(aArguments))
            {
                Console.ReadLine();
                return;
            }

            // new up a Binance Orderbook
            var lClientOrderbook = new BinanceOrderbook(mProgramOptions);

            // add event listeners required for getting the average execution price
            lClientOrderbook.OnDepthConnected += () =>
            {
                Console.WriteLine("Connected to market depth stream, waiting for updates");
            };

            lClientOrderbook.OnDepthError += (string aError) =>
            {
                Console.WriteLine(aError);
            };

            // if trader is buying, the BidAvgPxUpdated will be updated
            if (mProgramOptions.Side == Options.TradeSide.Buy)
            {
                lClientOrderbook.OnBidAvgPxUpdated += (decimal aAvgPrice) =>
                {
                    DisplayAveragePrice(aAvgPrice);
                };
            }
            else
            {
                lClientOrderbook.OnAskAvgPxUpdated += (decimal aAvgPrice) =>
                {
                    DisplayAveragePrice(aAvgPrice);
                };
            }

            Console.WriteLine($"Downloading orderbook for {mProgramOptions.Symbol} from Binance");
            if (lClientOrderbook.SubscribeDepth())
            {
                Console.WriteLine($"Initial market depth snapshot downloaded");
            }

            // pause
            Console.ReadLine();
        }

        /// <summary>
        /// Outputs the average execution price to the console (replaces existing entry)
        /// </summary>
        /// <param name="aAvgPrice">Price to be output</param>
        private static void DisplayAveragePrice(decimal aAvgPrice)
        {
            // NB: '\r' brings cursor back to the beginning of the line
            string lOutput = $"\rAverage Execution Price to {mProgramOptions.Side} {mProgramOptions.Quantity} {mProgramOptions.Symbol}: {aAvgPrice}";

            // Pad the output to the previous length so we don't show stale decimal digits
            Console.Write(lOutput.PadRight(mPreviousOutputLength, ' '));

            // remember previous output length for padding on next update
            mPreviousOutputLength = lOutput.Length;
        }

        /// <summary>
        /// Parses the arguments; Set any options or print help.
        /// </summary>
        /// <param name="aArguments">The command line arguments.</param>
        /// <param name="aOptions">Options to be set from command line.</param>
        /// <returns>False if <see cref="DisplayCommandLineHelp"/> needed.</returns>
        private static bool ParseCommandLineArgs(IEnumerable<string> aArguments)
        {
            ParserResult<Options> lResult = Parser.Default.ParseArguments<Options>(aArguments);

            // read in options successfully parsed
            lResult.WithParsed(aOptions =>
            {
                mProgramOptions = new Options
                {
                    Symbol = aOptions.Symbol,
                    Quantity = aOptions.Quantity,
                    MaxUpdateSpeed = aOptions.MaxUpdateSpeed,
                    Side = aOptions.Side,
                    DepthLimit = aOptions.DepthLimit
                };
            });
            // handle option errors / help usage
            lResult.WithNotParsed(aErrors =>
            {
                // print out command line usage (includes any errors detected)
                DisplayCommandLineHelp(lResult);
            });
            
            // program options won't be set if in error or help required
            return mProgramOptions != null;
        }
        private static void DisplayCommandLineHelp(ParserResult<Options> aParserResult)
        {
            CommandLine.Text.HelpText.AutoBuild(aParserResult);
        }
    }
}
