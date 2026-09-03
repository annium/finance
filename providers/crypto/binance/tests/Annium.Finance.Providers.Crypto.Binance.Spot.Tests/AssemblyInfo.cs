using Annium.Finance.Providers.Tests.Lib;

// where CI looks for this project's secrets: BINANCE_SPOT_TEST_KEY and friends. Spot and USD-M futures
// keep separate credentials on purpose - they are separate venues, and a further provider will differ
// again - so the environment has to keep them apart the way the per-project test.env already does
[assembly: TestEnvScope("BINANCE_SPOT")]
