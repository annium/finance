// using System.Threading.Tasks;
// using Xunit;
// using Xunit.Abstractions;
//
// namespace Annium.Finance.Providers.Crypto.Binance.UsdFutures.Tests.Internal.Connectors;
//
// [Collection(Collection.Name)]
// public class UserDataLimitTests : UserDataTestsMarketBase
// {
//     public UserDataLimitTests(ITestOutputHelper output)
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
//         var request = InitLimitOrder(GenerateClientOrderId(), Security.Key, OrderSide.Buy, ExtremeHighQty, LowPrice);
//         await InitInvalidOrder(request);
//
//         Logger.Trace("done");
//     }
//
//     [Fact]
//     [Trait(TestGroup, UserDataTest)]
//     public async Task InitOrder_Valid()
//     {
//         Logger.Trace("start");
//
//         // arrange
//         var request = InitLimitOrder(GenerateClientOrderId(), Security.Key, OrderSide.Buy, MinQty, LowPrice);
//
//         // act
//         var order = await InitValidOrder(request, OrderStatus.New);
//         await EnsureBalanceIsLocked();
//
//         // cleanup
//         await CancelValidOrder(order);
//         await EnsureBalanceIsReleased();
//
//         Logger.Trace("done");
//     }
//
//     [Fact]
//     [Trait(TestGroup, UserDataTest)]
//     public async Task ModifyOrder_Invalid()
//     {
//         Logger.Trace("start");
//
//         // arrange
//         var initRequest = InitLimitOrder(GenerateClientOrderId(), Security.Key, OrderSide.Buy, MinQty, LowPrice);
//         var initOrder = await InitValidOrder(initRequest, OrderStatus.New);
//         var modifyRequest = ModifyToLimitOrder(initOrder, initOrder.Side, ExtremeHighQty, initOrder.Price);
//
//         // act
//         await ModifyInvalidOrder(modifyRequest);
//
//         Logger.Trace("done");
//     }
//
//     [Fact]
//     [Trait(TestGroup, UserDataTest)]
//     public async Task ModifyOrder_Valid()
//     {
//         Logger.Trace("start");
//
//         // arrange
//         var initRequest = InitLimitOrder(GenerateClientOrderId(), Security.Key, OrderSide.Buy, MinQty, LowPrice);
//         var initialOrder = await InitValidOrder(initRequest, OrderStatus.New);
//         var modifyRequest = ModifyToLimitOrder(initialOrder, initialOrder.Side, initialOrder.Quantity + Security.LotSize, initialOrder.Price + Security.MinStep);
//
//         // act
//         var modifiedOrder = await ModifyValidOrder(modifyRequest, OrderStatus.New);
//         await EnsureBalanceIsLocked();
//
//         // cleanup
//         await CancelValidOrder(modifiedOrder);
//         await EnsureBalanceIsReleased();
//
//         Logger.Trace("done");
//     }
//
//     [Fact]
//     [Trait(TestGroup, UserDataTest)]
//     public async Task CancelOrder()
//     {
//         Logger.Trace("start");
//
//         // arrange
//         var request = InitLimitOrder(GenerateClientOrderId(), Security.Key, OrderSide.Buy, MinQty, LowPrice);
//
//         // act
//         var order = await InitValidOrder(request, OrderStatus.New);
//         await EnsureBalanceIsLocked();
//
//         // cleanup
//         await CancelValidOrder(order);
//         await EnsureBalanceIsReleased();
//
//         Logger.Trace("done");
//     }
//
//     [Fact]
//     [Trait(TestGroup, UserDataTest)]
//     public async Task CancelOrderTwice()
//     {
//         Logger.Trace("start");
//
//         // arrange
//         var request = InitLimitOrder(GenerateClientOrderId(), Security.Key, OrderSide.Buy, MinQty, LowPrice);
//
//         // act
//         var order = await InitValidOrder(request, OrderStatus.New);
//         await EnsureBalanceIsLocked();
//         await CancelValidOrder(order);
//         // Try to cancel the order which is already cancelled. The operation must be correct.
//         await CancelValidOrder(order);
//
//         Logger.Trace("done");
//     }
//
//     [Fact]
//     [Trait(TestGroup, UserDataTest)]
//     public async Task PlaceOrderWithSameClientId()
//     {
//         Logger.Trace("start");
//
//         // arrange
//         var request = InitLimitOrder(GenerateClientOrderId(), Security.Key, OrderSide.Buy, MinQty, LowPrice);
//
//         // act
//         var order = await InitValidOrder(request, OrderStatus.New);
//         await EnsureBalanceIsLocked();
//         // Try to place another order with the same ClientId
//         var anotherOrder = await InitValidOrder(request, OrderStatus.New);
//
//         // assert
//         EnsureIsSameOrder(order, anotherOrder);
//
//         // cleanup
//         await CancelValidOrder(order);
//
//         Logger.Trace("done");
//     }
// }
