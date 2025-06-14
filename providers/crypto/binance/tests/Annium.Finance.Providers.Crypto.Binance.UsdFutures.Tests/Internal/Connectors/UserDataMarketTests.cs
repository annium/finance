// using System.Threading.Tasks;
// using Xunit;
// //
// namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Connectors;
//
// [Collection(Collection.Name)]
// public class UserDataMarketTests : UserDataTestsMarketBase
// {
//     public UserDataMarketTests(ITestOutputHelper output)
//         : base(output)
//     {
//     }
//
//     [Fact]
//     [Trait(TestGroup, UserDataTest)]
//     public async Task InitOrder_Invalid()
//     {
//         Logger.Trace("start");
//
//         var request = InitMarketOrder(GenerateClientOrderId(), Security.Key, OrderSide.Buy, ExtremeHighQty);
//         await InitInvalidOrder(request);
//
//         Logger.Trace("done");
//     }
//
//     [Fact]
//     [Trait(TestGroup, UserDataTest)]
//     public async Task InitOrder_TakeProfit_StopLoss()
//     {
//         Logger.Trace("start");
//
//         // arrange
//         var request = InitMarketOrder(GenerateClientOrderId(), Security.Key, OrderSide.Buy, MinQty);
//
//         // open position
//         var order = await InitValidOrder(request, OrderStatus.Filled);
//         await EnsureBalanceIsDecreased();
//         await EnsurePositionIsIncreased();
//
//         // try cleanup
//         await CancelInvalidOrder(order);
//
//         // TP & SL invalid orders
//         await TestOrder(
//             InitStopLossMarketOrder(GenerateClientOrderId(), Security.Key, OrderSide.Sell, ExtremeHighQty, LowPrice),
//             InitStopLossMarketOrder(GenerateClientOrderId(), Security.Key, OrderSide.Sell, GetPositionAmount(), LowPrice));
//
//         await TestOrder(
//             InitTakeProfitMarketOrder(GenerateClientOrderId(), Security.Key, OrderSide.Sell, GetPositionAmount(), ExtremeHighPrice),
//             InitTakeProfitMarketOrder(GenerateClientOrderId(), Security.Key, OrderSide.Sell, GetPositionAmount(), HighPrice));
//
//         await TestOrder(
//             InitStopLossLimitOrder(GenerateClientOrderId(), Security.Key, OrderSide.Sell, ExtremeHighQty, LowPrice + Security.MinStep, LowPrice),
//             InitStopLossLimitOrder(GenerateClientOrderId(), Security.Key, OrderSide.Sell, GetPositionAmount(), LowPrice + Security.MinStep, LowPrice));
//
//         await TestOrder(
//             InitTakeProfitLimitOrder(GenerateClientOrderId(), Security.Key, OrderSide.Sell, GetPositionAmount(), HighPrice + Security.MinStep, ExtremeHighPrice),
//             InitTakeProfitLimitOrder(GenerateClientOrderId(), Security.Key, OrderSide.Sell, GetPositionAmount(), HighPrice + Security.MinStep, HighPrice));
//
//         // cleanup
//         request = InitMarketOrder(GenerateClientOrderId(), Security.Key, OrderSide.Sell, GetPositionAmount());
//         await InitValidOrder(request, OrderStatus.Filled);
//         await EnsureBalanceIsIncreased();
//         await EnsurePositionIsDecreased();
//
//         Logger.Trace("done");
//     }
//
//     private async Task TestOrder(IInitOrderRequest invalidRequest, IInitOrderRequest validRequest)
//     {
//         Logger.Trace("start {0} order tet", invalidRequest.Type);
//
//         Logger.Trace("init invalid {0} order", invalidRequest.Type);
//         await InitInvalidOrder(invalidRequest);
//
//         Logger.Trace("init valid {0} order", validRequest.Type);
//         var order = await InitValidOrder(validRequest, OrderStatus.New);
//
//         Logger.Trace("cancel valid {0} order", validRequest.Type);
//         await CancelValidOrder(order);
//     }
// }
