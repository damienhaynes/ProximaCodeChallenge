using CodeChallenge.Clients.Binance;
using CodeChallenge.Exchanges.Binance.DataModels;
using System.Collections.Generic;
using Xunit;

namespace CodeChallenageTest
{
    public class BinanceOrderbookUnitTest
    {
        [Fact]
        public void TestCalculateAverageExecutionPrice()
        {
            var lDepthItems = new List<DepthItem>();

            decimal lAveragePx = OrderbookUtils.CalculateAverageExecutionPrice(0, lDepthItems);
            Assert.Equal(0, lAveragePx);

            lAveragePx = OrderbookUtils.CalculateAverageExecutionPrice(-10, null);
            Assert.Equal(0, lAveragePx);

            // single level
            lDepthItems.Add(new DepthItem { Price = 2, Quantity = 10 });
            lAveragePx = OrderbookUtils.CalculateAverageExecutionPrice(10, lDepthItems);
            
            //(2x10/10)
            Assert.Equal(2, lAveragePx);

            // multiple levels
            lDepthItems.Clear();
            lDepthItems.Add(new DepthItem { Price = 1, Quantity = 1 });
            lDepthItems.Add(new DepthItem { Price = 2, Quantity = 2 });
            lDepthItems.Add(new DepthItem { Price = 3, Quantity = 3 });
            lDepthItems.Add(new DepthItem { Price = 4, Quantity = 4 });
            lAveragePx = OrderbookUtils.CalculateAverageExecutionPrice(10, lDepthItems);

            //(1x1 + 2x2 + 3x3 + 4x4 / 1+2+3+4) 
            Assert.Equal(30/10, lAveragePx);

            // More realistic - sweep 6 of 7 levels with qty remaining
            decimal lTradeQty = 3.92300000m;

            lDepthItems.Clear();
            lDepthItems.Add(new DepthItem { Price = 19501.25000000m, Quantity = 3.53187800m }); // 0.391122 remaining
            lDepthItems.Add(new DepthItem { Price = 19501.88000000m, Quantity = 0.19598300m }); // 0.195139 remaining
            lDepthItems.Add(new DepthItem { Price = 19502.35000000m, Quantity = 0.00600000m }); // 0.189139 remaining
            lDepthItems.Add(new DepthItem { Price = 19502.50000000m, Quantity = 0.02351300m }); // 0.165626 remaining
            lDepthItems.Add(new DepthItem { Price = 19502.80000000m, Quantity = 0.00600000m }); // 0.159626 remaining
            lDepthItems.Add(new DepthItem { Price = 19503.61000000m, Quantity = 0.16007900m }); // only left with 0.159626 to trade at this level
            lDepthItems.Add(new DepthItem { Price = 19504.05000000m, Quantity = 0.26001100m });

            lAveragePx = OrderbookUtils.CalculateAverageExecutionPrice(lTradeQty, lDepthItems);

            decimal lExpected = (68876.0358475m + 3822.03694804m + 117.0141m + 458.5622825m + 117.0168m + 3113.28324986m) / 3.923m;
            Assert.Equal(lExpected, lAveragePx);
        }

        [Fact]
        public void TestMergeDepthInvalidUpdates()
        {
            List<DepthItem> lUpdateItems = null;
            List<DepthItem> lCurrentItems = null;

            // check invalid input argument
            OrderbookUtils.MergeDepth(lUpdateItems, ref lCurrentItems, false);
            Assert.True(lCurrentItems == null);

            // items removed when nothing to remove (qty = 0)
            lUpdateItems = new List<DepthItem>
            {
                new DepthItem { Price = 1.00000000m, Quantity = 0.00000000m },
                new DepthItem { Price = 2.00000000m, Quantity = 0.00000000m }
            };

            OrderbookUtils.MergeDepth(lUpdateItems, ref lCurrentItems, false);
            Assert.True(lCurrentItems.Count == 0);
        }

        [Fact]
        public void TestMergeDepthWithRemovals()
        {
            // remove items only
            var lUpdateItems = new List<DepthItem>
            {
                new DepthItem { Price = 1.00000000m, Quantity = 0.00000000m },
                new DepthItem { Price = 2.00000000m, Quantity = 0.00000000m },
                new DepthItem { Price = 3.00000000m, Quantity = 0.00000000m }
            };
            var lCurrentItems = new List<DepthItem>
            {
                new DepthItem { Price = 1.00000000m, Quantity = 1.00000000m },
                new DepthItem { Price = 2.00000000m, Quantity = 2.00000000m },
                new DepthItem { Price = 3.00000000m, Quantity = 3.00000000m }
            };

            OrderbookUtils.MergeDepth(lUpdateItems, ref lCurrentItems, false);
            Assert.True(lCurrentItems.Count == 0);

            // remove items that dont't exist
            lCurrentItems = new List<DepthItem>
            {
                new DepthItem { Price = 4.00000000m, Quantity = 1.00000000m },
                new DepthItem { Price = 5.00000000m, Quantity = 2.00000000m },
                new DepthItem { Price = 6.00000000m, Quantity = 3.00000000m }
            };

            OrderbookUtils.MergeDepth(lUpdateItems, ref lCurrentItems, false);
            Assert.True(lCurrentItems.Count == 3);
        }

        [Fact]
        public void TestMergeDepthWithAddAsksAndSort()
        {
            var lUpdateItems = new List<DepthItem>
            {
                new DepthItem { Price = 1.00000000m, Quantity = 1.00000000m },
                new DepthItem { Price = 3.00000000m, Quantity = 3.00000000m },
                new DepthItem { Price = 5.00000000m, Quantity = 5.00000000m }
            };
            var lCurrentItems = new List<DepthItem>
            {
                new DepthItem { Price = 2.00000000m, Quantity = 2.00000000m },
                new DepthItem { Price = 4.00000000m, Quantity = 4.00000000m },
                new DepthItem { Price = 6.00000000m, Quantity = 6.00000000m }
            };

            // check sort order of asks after add
            OrderbookUtils.MergeDepth(lUpdateItems, ref lCurrentItems, false);
            Assert.True(lCurrentItems.Count == 6);
            Assert.True(lCurrentItems[0].Equals(lUpdateItems[0]));
            Assert.True(lCurrentItems[2].Equals(lUpdateItems[1]));
            Assert.True(lCurrentItems[4].Equals(lUpdateItems[2]));
        }

        [Fact]
        public void TestMergeDepthWithAddBidsAndSort()
        {
            var lUpdateItems = new List<DepthItem>
            {
                new DepthItem { Price = 5.00000000m, Quantity = 5.00000000m },
                new DepthItem { Price = 3.00000000m, Quantity = 3.00000000m },
                new DepthItem { Price = 1.00000000m, Quantity = 1.00000000m }
            };
            var lCurrentItems = new List<DepthItem>
            {
                new DepthItem { Price = 6.00000000m, Quantity = 6.00000000m },
                new DepthItem { Price = 4.00000000m, Quantity = 4.00000000m },
                new DepthItem { Price = 2.00000000m, Quantity = 2.00000000m }
            };

            // check sort order of bids after add
            OrderbookUtils.MergeDepth(lUpdateItems, ref lCurrentItems, true);
            Assert.True(lCurrentItems.Count == 6);
            Assert.True(lCurrentItems[1].Equals(lUpdateItems[0]));
            Assert.True(lCurrentItems[3].Equals(lUpdateItems[1]));
            Assert.True(lCurrentItems[5].Equals(lUpdateItems[2]));
        }

        [Fact]
        public void TestMergeDepthWithUpdatedAsks()
        {
            var lUpdateItems = new List<DepthItem>
            {
                new DepthItem { Price = 1.00000000m, Quantity = 2.00000000m },
                new DepthItem { Price = 2.00000000m, Quantity = 4.00000000m },
                new DepthItem { Price = 3.00000000m, Quantity = 6.00000000m }
            };
            var lCurrentItems = new List<DepthItem>
            {
                new DepthItem { Price = 1.00000000m, Quantity = 1.00000000m },
                new DepthItem { Price = 2.00000000m, Quantity = 2.00000000m },
                new DepthItem { Price = 3.00000000m, Quantity = 3.00000000m }
            };

            OrderbookUtils.MergeDepth(lUpdateItems, ref lCurrentItems, false);
            Assert.True(lCurrentItems.Count == 3);
            Assert.True(lCurrentItems[0].Equals(lUpdateItems[0]));
            Assert.True(lCurrentItems[1].Equals(lUpdateItems[1]));
            Assert.True(lCurrentItems[2].Equals(lUpdateItems[2]));
        }

        [Fact]
        public void TestMergeDepthWithAddUpdateRemovalOfAsks()
        {
            var lUpdateItems = new List<DepthItem>
            {
                new DepthItem { Price = 1.00000000m, Quantity = 1.00000000m },
                new DepthItem { Price = 2.00000000m, Quantity = 2.00000000m },
                new DepthItem { Price = 3.00000000m, Quantity = 3.00000000m },
                new DepthItem { Price = 4.00000000m, Quantity = 0.00000000m },
                new DepthItem { Price = 5.00000000m, Quantity = 5.00000000m },
                new DepthItem { Price = 7.00000000m, Quantity = 7.00000000m },

            };
            var lCurrentItems = new List<DepthItem>
            {
                new DepthItem { Price = 2.00000000m, Quantity = 1.00000000m },
                new DepthItem { Price = 4.00000000m, Quantity = 4.00000000m },
                new DepthItem { Price = 6.00000000m, Quantity = 6.00000000m }
            };

            OrderbookUtils.MergeDepth(lUpdateItems, ref lCurrentItems, false);
            Assert.True(lCurrentItems.Count == 6);
            Assert.True(lCurrentItems[0].Equals(lUpdateItems[0]));
            Assert.True(lCurrentItems[1].Equals(lUpdateItems[1]));
            Assert.True(lCurrentItems[2].Equals(lUpdateItems[2]));
            Assert.True(lCurrentItems[3].Equals(lUpdateItems[4]));
            Assert.True(lCurrentItems[5].Equals(lUpdateItems[5]));
        }
    }
}
