// using System;
// using System.Text.Json;
// using System.Threading.Tasks;
// using Annium.Finance.Providers.Abstractions.Domain.Dto;
// using Annium.Finance.Providers.Abstractions.Domain.Enums;
// using Annium.Finance.Providers.Abstractions.Domain.Extensions;
// using Annium.Finance.Providers.Abstractions.Domain.Models;
// using Annium.Finance.Providers.Shared;
// using Annium.Logging;
// using Annium.Testing;
// using Xunit;
// // using static Annium.Finance.Providers.Abstractions.Domain.Tools.RequestBuilder;
//
// namespace Annium.Finance.Providers.Tests.Shared.Connectors;
//
// public abstract class UserConnectorSpotTestBase : UserConnectorTestBase, IAsyncLifetime
// {
//     private AssetDto _assetBalance = default!;
//
//     protected UserConnectorSpotTestBase(
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
//         this.Trace("try sell asset if any");
//         var amount = GetAssetAmount();
//         if (amount > 0)
//         {
//             this.Trace("sell asset amount: {amount}", amount);
//             await InitValidOrder(
//                 InitMarketOrder(GenerateClientOrderId(), Instrument.Symbol, OrderSide.Sell, amount),
//                 OrderStatus.Filled
//             );
//             await EnsureBalanceIsIncreased();
//             await EnsureAssetIsDecreased();
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
//         _assetBalance = GetBalance(Instrument.Quote.Code);
//
//         this.Trace("done");
//     }
//
//     protected decimal GetAssetAmount()
//     {
//         this.Trace<string>("get {quote} last balance", Instrument.Quote.Code);
//         var free = GetBalance(Instrument.Quote.Code).Free;
//
//         return Instrument.ToLotSize(free);
//     }
//
//     protected Task EnsureAssetIsIncreased()
//     {
//         var originalBalance = _assetBalance;
//
//         this.Trace<string>(
//             "ensure current balance is bought compared to original {balance}",
//             JsonSerializer.Serialize(originalBalance)
//         );
//
//         return Expect.ToAsync(() =>
//         {
//             var currentBalance = GetBalance(Instrument.Quote.Code);
//             currentBalance.Free.IsGreater(originalBalance.Free);
//         });
//     }
//
//     protected Task EnsureAssetIsDecreased()
//     {
//         var originalBalance = _assetBalance;
//
//         this.Trace<string>(
//             "ensure current balance is sold compared to original {balance}",
//             JsonSerializer.Serialize(originalBalance)
//         );
//
//         return Expect.ToAsync(() =>
//         {
//             var currentBalance = GetBalance(Instrument.Quote.Code);
//             currentBalance.Free.IsLess(originalBalance.Free);
//         });
//     }
// }
