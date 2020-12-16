# ProximaCodeChallenge
Calculates the average execution price for Binance's BTCUSDT market

# Command Line Usage
The following command line options and arguments are supported:
 * \-m (\-\-maxupdatespeed): Sets maximum update speed to 100ms, defaults to 1000ms;
 * \-t (\-\-side): Side of order used to calculate average execution price basis input quantity. By default assumes trader *buys* X bitcoin;
 * \-s (\-\-symbol): Symbol used to create a local copy of the market. Defaults to *BTCUSDT*;
 * \-l (\-\-depthlimit): Number of records to request on Binance depth endpoint. Valid Limits = 5, 10, 20, 50, 100, 500, 1000, 5000;
 * Quantity is not bound to any option and should be the first argument specified.

Example Usage:
```
CodeChallenge 4.10000000 --depthlimit 1000
```
The above example will print out the average execution price if a trader buys 4.10000000 BTCUSDT.