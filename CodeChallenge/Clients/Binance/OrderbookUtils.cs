using CodeChallenge.Exchanges.Binance.DataModels;
using System.Collections.Generic;
using System.Linq;

namespace CodeChallenge.Clients.Binance
{
    public static class OrderbookUtils
    {
        /// <summary>
        /// Updates the current list of bids or asks basis the corresponding list of updates
        /// </summary>
        /// <param name="aUpdateItems">List of Bid/Ask items that have been updated</param>
        /// <param name="aCurrentItems">Current list of Bid/Ask items to update basis the <see cref="aUpdateItems"/> argument</param>
        /// <param name="aIsBids">Depth item type (bids or asks) used for sorting</param>
        public static void MergeDepth(List<DepthItem> aUpdateItems, ref List<DepthItem> aCurrentItems, bool aIsBids)
        {
            // check for invalid arguments
            if (aUpdateItems == null) return;

            if (aCurrentItems == null)
                aCurrentItems = new List<DepthItem>();

            // Check if there are any bid/ask depth entries to be removed (Quantity ==0)
            foreach (var lUpdateItem in aUpdateItems.Where(item => item.Quantity == 0))
            {
                // check if the depth level exists
                int lIndex = aCurrentItems.FindIndex(item => item.Price == lUpdateItem.Price);

                // remove the price level if it exists (it's possible to receive a depth item to remove without a reference)
                if (lIndex >= 0)
                {
                    aCurrentItems.RemoveAt(lIndex);
                }
            }

            // Merge bid/ask updates
            // If the price level exists, then update, otherwise add a new level
            foreach (var lUpdateItem in aUpdateItems.Where(item => item.Quantity > 0))
            {
                // check if the depth level exists
                int lIndex = aCurrentItems.FindIndex(item => item.Price == lUpdateItem.Price);
                
                if (lIndex >= 0)
                {
                    // bid/ask already exists, lets replace the level with the new *absolute* quantity
                    aCurrentItems[lIndex] = lUpdateItem;
                }
                else
                {
                    // add the new bid/ask level
                    aCurrentItems.Add(lUpdateItem);
                }
            }

            // sort items
            if (!aIsBids)
            {
                // sort the asks in asscending price order
                aCurrentItems = aCurrentItems.OrderBy(b => b.Price).ToList();
            }
            else
            {
                // sort the bids in descending price order
                aCurrentItems = aCurrentItems.OrderByDescending(b => b.Price).ToList();
            }
        }

        /// <summary>
        /// Calculates the average execution price basis the quantity for the current bid or ask depth. 
        /// Assumes DepthItems are sorted (bids: descending, asks: assecending)
        /// </summary>
        /// <param name="aQuantity">Quantity used to determine average price</param>
        /// <param name="aDepthItems">The bid or ask levels used to calculate the weighed average price over</param>
        /// <returns>Average execution price</returns>
        public static decimal CalculateAverageExecutionPrice(decimal aQuantity, List<DepthItem> aDepthItems)
        {
            // check for invalid arguments
            if (aDepthItems == null || aQuantity <= 0)
                return 0;

            decimal lTotalTradedQty = 0;
            decimal lRemainingQty = aQuantity;

            // List of Weighted price per level i.e. (QtyTraded x Price)
            var lWeightedPrices = new List<decimal>();

            // we want to see what the price is, if quantity is executed over 1 or more levels
            // stop processing levels when no more quantity to trade is remaining
            foreach (var lItem in aDepthItems)
            {
                // if the current level qty is >= remaining qty,
                // then we can exit trading only on what's left
                if (lItem.Quantity >= lRemainingQty)
                {
                    // add the weighted price
                    lWeightedPrices.Add(lRemainingQty * lItem.Price);

                    // update remaining/total traded qty
                    // there should be nothing remaining and completely executed
                    lRemainingQty = 0;
                    lTotalTradedQty = aQuantity;

                    // we're done, no more remaining qty
                    break;
                }

                // add the weighted price
                lWeightedPrices.Add(lItem.Quantity * lItem.Price);

                // execute all the qty at the current price level
                lRemainingQty -= lItem.Quantity;

                // update total traded qty
                lTotalTradedQty += lItem.Quantity;
            }

            // calculate the final average execution price
            return lWeightedPrices.Sum() / lTotalTradedQty;
        }
    }
}
