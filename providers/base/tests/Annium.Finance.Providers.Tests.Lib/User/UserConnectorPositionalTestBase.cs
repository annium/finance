// using System;
// using System.Linq;
// using System.Text.Json;
// using System.Threading.Tasks;
// using Annium.Finance.Providers.Abstractions.Domain.Dto;
// using Annium.Finance.Providers.Abstractions.Domain.Enums;
// using Annium.Finance.Providers.Abstractions.Domain.Extensions;
// using Annium.Finance.Providers.Abstractions.Domain.Models;
// using Annium.Finance.Providers.Shared;
// using Annium.Finance.Providers.Tests.Shared.Extensions;
// using Annium.Logging;
// using Annium.Testing;
// using Xunit;
// // using static Annium.Finance.Providers.Abstractions.Domain.Tools.RequestBuilder;
//
// namespace Annium.Finance.Providers.Tests.Lib.Connectors;
//
// public abstract class UserConnectorPositionalTestBase : UserConnectorTestBase, IAsyncLifetime
// {
//     private PositionDto _position = default!;
//
//     protected UserConnectorPositionalTestBase(
//         Action<ProviderRegistrationContext> registerProvider,
//         UserSettings config,
//         string symbol,
//         ITestOutputHelper output
//     )
//         : base(registerProvider, config, symbol, output) { }
//
//     public async Task InitializeAsync()
//     {
//         this.Trace("start");
//
//         this.Trace("initialize base");
//         await InitializeBaseAsync();
//
//         this.Trace("cancel open orders");
//         await CancelOpenOrders();
//
//         this.Trace("await for balances");
//         await AwaitForInitialBalances();
//
//         this.Trace("await for positions and leverages (before closing)");
//         await AwaitForInitialPositionsAndLeverages();
//
//         this.Trace("close active positions");
//         await CloseActivePositions();
//
//         EnsureNoErrors();
//
//         this.Trace("done");
//     }
//
//     public async Task DisposeAsync()
//     {
//         this.Trace("start");
//
//         this.Trace("cancel open orders");
//         await CancelOpenOrders();
//
//         this.Trace("try close position if any");
//         var amount = GetPositionAmount();
//         if (amount > 0)
//         {
//             this.Trace("close position amount: {0}", amount);
//             await InitValidOrder(
//                 InitMarketOrder(GenerateClientOrderId(), Instrument.Symbol, OrderSide.Sell, amount),
//                 OrderStatus.Filled
//             );
//             await EnsureBalanceIsIncreased();
//             await EnsurePositionIsDecreased();
//         }
//
//         EnsureNoErrors();
//
//         this.Trace("dispose base");
//         await DisposeBaseAsync();
//
//         EnsureNoErrors();
//
//         this.Trace("done");
//     }
//
//     protected override void Snapshot()
//     {
//         this.Trace("start");
//
//         base.Snapshot();
//         _position = GetPosition();
//
//         this.Trace("done");
//     }
//
//     protected Task AwaitForInitialPositionsAndLeverages()
//     {
//         this.Trace("await for positions");
//         return Expect.ToAsync(() => _.Count.IsGreater(0));
//     }
//
//     protected decimal GetPositionAmount()
//     {
//         this.Trace<string>("get size of {symbol} position", Instrument.Symbol);
//         var amount = GetPosition().Amount;
//
//         return Instrument.ToLotSize(amount);
//     }
//
//     protected Task EnsurePositionIsIncreased()
//     {
//         var originalPosition = _position;
//
//         this.Trace<string>(
//             "ensure position amount is increased compared to original {0}",
//             JsonSerializer.Serialize(originalPosition)
//         );
//
//         return Expect.ToAsync(() =>
//         {
//             var currentPosition = GetPosition();
//             currentPosition.Amount.IsGreater(originalPosition.Amount);
//         });
//     }
//
//     protected Task EnsurePositionIsDecreased()
//     {
//         var originalPosition = _position;
//
//         this.Trace<string>(
//             "ensure position amount is decreased compared to original {0}",
//             JsonSerializer.Serialize(originalPosition)
//         );
//
//         return Expect.ToAsync(() =>
//         {
//             var currentPosition = GetPosition();
//             currentPosition.Amount.IsLess(originalPosition.Amount);
//         });
//     }
//
//     private async Task CloseActivePositions()
//     {
//         this.Trace("start");
//
//         var activePositions = Connector.Positions.Where(x => x.Amount > 0).ToArray();
//         if (activePositions.Length == 0)
//         {
//             this.Trace("no active positions, break");
//             return;
//         }
//
//         foreach (var position in activePositions)
//         {
//             this.Trace("close {0} position with amount {1}", Instrument, position.Amount);
//             await Connector
//                 .InitOrder(
//                     InitMarketOrder(
//                         GenerateClientOrderId(),
//                         Instrument.Symbol,
//                         position.Amount < 0 ? OrderSide.Buy : OrderSide.Sell,
//                         Math.Abs(position.Amount)
//                     )
//                 )
//                 .Unwrap();
//         }
//
//         EnsureNoErrors();
//
//         this.Trace("close active positions - done");
//     }
//
//     private PositionDto GetPosition()
//     {
//         this.Trace<string>("get {0} position", Instrument.Symbol);
//         return Connector.Positions.Single(
//             x => x.OrientationRange is OrientationRange.Both && x.Symbol == Instrument.Symbol
//         );
//     }
// }
