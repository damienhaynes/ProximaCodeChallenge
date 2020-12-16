using CodeChallenge.Exchanges.Binance.API;
using CodeChallenge.Exchanges.Binance.DataModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static CodeChallenge.Clients.Binance.OrderbookUtils;

namespace CodeChallenge.Clients.Binance
{
    /// <summary>
    /// Manage a local orderbook from the Binance exchange
    /// </summary>
    internal class BinanceOrderbook
    {
        #region Event Handlers

        public delegate void DepthUpdated(DepthSnapshot aDepthSnapshot);
        public delegate void DepthError(string aErrorDescription);
        public delegate void DepthConnected();

        public delegate void BestBidUpdated(DepthItem aDepthItem);
        public delegate void BestAskUpdated(DepthItem aDepthItem);

        public delegate void BidsUpdated(List<DepthItem> aDepthItems);
        public delegate void AsksUpdated(List<DepthItem> aDepthItems);

        // Can also be calculated manually from OrderbookUtils after bids/asks updated
        public delegate void BidAvgPxUpdated(decimal aAveragePrice);
        public delegate void AskAvgPxUpdated(decimal aAveragePrice);

        /// <summary>
        /// Invoked after initial depth snapshot and/or merged update(s). 
        /// Requires subscription to depth
        /// </summary>
        public event DepthUpdated OnDepthUpdated;

        /// <summary>
        /// Invoked on connection to the WebSocket stream. 
        /// Requires subscription to depth
        /// </summary>
        public event DepthConnected OnDepthConnected;

        /// <summary>
        /// Invoked on error from depth RestAPI or WebSocket stream. 
        /// Requires subscription to depth
        /// </summary>
        public event DepthError OnDepthError;

        /// <summary>
        /// Invoked after a change to the best bid. 
        /// Requires subscription to depth
        /// </summary>
        public event BestBidUpdated OnBestBidUpdated;

        /// <summary>
        /// Invoked after a change to the best ask. 
        /// Requires subscription to depth
        /// </summary>
        public event BestAskUpdated OnBestAskUpdated;

        /// <summary>
        /// Invoked after a change to the list of bids. 
        /// Requires subscription to depth
        /// </summary>
        public event BidsUpdated OnBidsUpdated;

        /// <summary>
        /// Invoked after a change to the list of asks. 
        /// Requires subscription to depth
        /// </summary>
        public event AsksUpdated OnAsksUpdated;

        /// <summary>
        /// Invoked after a change to the average execution price from buy side. 
        /// When list of asks change, average price will be re-calculated
        /// Requires subscription to depth
        /// </summary>
        public event BidAvgPxUpdated OnBidAvgPxUpdated;

        /// <summary>
        /// Invoked after a change to the average execution price from sell side. 
        /// When list of bids change, average price will be re-calculated
        /// Requires subscription to depth
        /// </summary>
        public event AskAvgPxUpdated OnAskAvgPxUpdated;

        #endregion

        #region Member Fields

        private BinanceAPI mBinanceAPIClient;
        private DepthSnapshot mCurrentDepthSnapshot;
        
        private readonly ConcurrentQueue<DepthUpdate> mDepthUpdates = new ConcurrentQueue<DepthUpdate>();
        private readonly System.Timers.Timer mUpdateTimer;
        private readonly Options mOptions;
        private readonly object mLockObject = new object();

        #endregion

        #region Constuctor

        public BinanceOrderbook(Options aOptions)
        {
            mBinanceAPIClient = new BinanceAPI();
            mOptions = aOptions;

            // Timer used to process any queued updates from streams
            // set timer to keep up with stream updates (binance allows 1000ms or 100ms)
            mUpdateTimer = new System.Timers.Timer(aOptions.MaxUpdateSpeed ? 100 : 1000);
            mUpdateTimer.Elapsed += new System.Timers.ElapsedEventHandler(TickTimer);        
        }

        #endregion

        #region Public Properties

        public string Symbol => mOptions.Symbol;

        #endregion

        #region Public Methods

        public void UnSubscribeDepth()
        {
            mCurrentDepthSnapshot = null;
            mUpdateTimer.Enabled = false;

            // TODO: add call to close socket from Binance API (will need to remove inline events)
        }

        /// <summary>
        /// Subscribes to market depth for a symbol. 
        /// Subscribe to <see cref="OnDepthUpdated" /> event for updates
        /// </summary>
        /// <param name="aSymbol">Symbol for depth snapshot request</param>
        /// <returns>True if subscription is successful</returns>
        public bool SubscribeDepth()
        {
            // Currently the BinanceAPI is not using async methods (TODO)
            // Create a new task here as an alternative
            var lTask = Task.Factory.StartNew(() =>
            {
                // Step 1: create a new API object
                mBinanceAPIClient = new BinanceAPI();

                // Step 2: Create listeners for depth stream
                mBinanceAPIClient.OnDepthStreamError += (aErrorDescription) =>
                {
                    OnDepthError?.Invoke(aErrorDescription);
                };

                mBinanceAPIClient.OnDepthStreamOpened += () =>
                {
                    OnDepthConnected?.Invoke();
                };

                mBinanceAPIClient.OnDepthStreamMessageReceived += (aDepthUpdate) =>
                {
                    // Add depth update to queue, will be processed on next timer update
                    mDepthUpdates.Enqueue(aDepthUpdate);
                };

                // Step 3: Open the depth stream connection
                mBinanceAPIClient.OpenDepthStream(mOptions.Symbol);

                // Step 4: Get Depth snapshot
                DepthSnapshot lSnapshot = mBinanceAPIClient.GetDepthSnapshot(mOptions.Symbol, mOptions.DepthLimit);

                // update the current snapshot
                mCurrentDepthSnapshot = lSnapshot;

                // if there is already a listener, send initial snapshot
                OnDepthUpdated?.Invoke(lSnapshot);
            });

            try
            {
                lTask.Wait();
            }
            catch (AggregateException ae)
            {
                // send any error messages to listeners
                foreach (var e in ae.InnerExceptions)
                {
                    OnDepthError?.Invoke(e.Message);
                }

                return false;
            }

            // Step 5: Merge updates from depth stream into snapshot periodically
            mUpdateTimer.Enabled = true;

            return true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Update the local orderbook after updates
        /// </summary>
        private void UpdateOrderbook()
        {
            // TODO: review, may not need the lock, will need to consider passing current snapshot/updates into the tasks to be safe
            lock (mLockObject)
            {
                // If there is nothing in the queue or we have no snapshot, then nothing to do
                if (mDepthUpdates.IsEmpty || mCurrentDepthSnapshot == null)
                    return;

                // Grab the last item in the queue
                if (!mDepthUpdates.TryDequeue(out DepthUpdate lResult))
                {
                    // this shouldn't happen!
                    OnDepthError("Failed to remove depth item from queue!");
                    return;
                }

                // Discard update if snapshot already includes it i.e last updated ID is greater
                // this would typically occur on initial updates only
                if (lResult.LastUpdateId <= mCurrentDepthSnapshot.LastUpdateId)
                    return;

                // We should now have an update to process which is newer than what is in the initial snapshot.
                // We can process in parrallel to improve performance
                List<DepthItem> lCurrentAsks = mCurrentDepthSnapshot.Asks;
                List<DepthItem> lCurrentBids = mCurrentDepthSnapshot.Bids;

                #region Update orderbook from list of asks

                var lAskTask = Task.Factory.StartNew(() =>
                {
                    // remove / add / update depth levels
                    MergeDepth(lResult.AsksUpdated, ref lCurrentAsks, false);

                    // we're finished processing updates, update our current orderbook picture
                    mCurrentDepthSnapshot.Asks = lCurrentAsks;

                    // update average execution price basis an order on the buy side with X quantity
                    decimal lAvgExecutionPx = CalculateAverageExecutionPrice(mOptions.Quantity, lCurrentAsks);

                    // send updates to any listeners (expose public properties for these instead? we can notify updates by implementing INotifyPropertyChanged)
                    OnAsksUpdated?.Invoke(lCurrentAsks);
                    OnBestAskUpdated?.Invoke(lCurrentAsks.FirstOrDefault());
                    OnBidAvgPxUpdated?.Invoke(lAvgExecutionPx);

                    Debug.WriteLine($"Buy Average Execution Price = {lAvgExecutionPx}, Thread ID = {Thread.CurrentThread.ManagedThreadId}");
                });

                #endregion

                #region Update orderbook from list of bids

                var lBidTask = Task.Factory.StartNew(() =>
                {
                    // remove / add / update depth levels
                    MergeDepth(lResult.BidsUpdated, ref lCurrentBids, true);

                    // we're finished processing updates, update our current orderbook picture
                    mCurrentDepthSnapshot.Bids = lCurrentBids;

                    // update average execution price basis an order on the sell side with X quantity
                    decimal lAvgExecutionPx = CalculateAverageExecutionPrice(mOptions.Quantity, lCurrentBids);

                    // send updates to any listeners
                    OnBidsUpdated?.Invoke(lCurrentBids);
                    OnBestBidUpdated?.Invoke(lCurrentBids.FirstOrDefault());
                    OnAskAvgPxUpdated?.Invoke(lAvgExecutionPx);

                    Debug.WriteLine($"Sell Average Execution Price = {lAvgExecutionPx}, Thread ID = {Thread.CurrentThread.ManagedThreadId}");
                });

                #endregion

                // wait until all tasks done
                Task[] lTasksToWait = { lBidTask, lAskTask };
                Task.WaitAll(lTasksToWait);

                // update the last updated Id in the snapshot
                mCurrentDepthSnapshot.LastUpdateId = lResult.LastUpdateId;

                // Send full snapshot to any listeners
                OnDepthUpdated?.Invoke(mCurrentDepthSnapshot);
            }

            return;
        }

        /// <summary>
        /// Process any queues and update orderbook when timer ticks
        /// </summary>
        private void TickTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            UpdateOrderbook();
        }

        #endregion
    }
}
