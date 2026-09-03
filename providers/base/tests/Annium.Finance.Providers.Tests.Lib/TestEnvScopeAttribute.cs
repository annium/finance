using System;

namespace Annium.Finance.Providers.Tests.Lib;

/// <summary>
/// Names the prefix a test assembly's secrets carry as process environment variables.
/// </summary>
/// <remarks>
/// Every test assembly is its own process with its own working directory, so a per-project `test.env`
/// separates one provider's credentials from another's by where the file sits. A process environment has
/// no such thing: one job, one set of variables, and every test assembly in it reading the same
/// <c>TEST_KEY</c>. Providers are meant to hold different credentials - two Binance venues may already,
/// and a third provider certainly will - so the environment needs the separation spelled out.
///
/// Declared once per test project:
/// <code>
/// [assembly: TestEnvScope("BINANCE_SPOT")]
/// </code>
/// after which <c>TEST_KEY</c> is read from <c>BINANCE_SPOT_TEST_KEY</c>. The file keeps the unqualified
/// names - it is already scoped by living in the project - so nothing about the local flow changes.
///
/// An assembly without this attribute reads no environment variables at all. That is the safe direction:
/// a project nobody scoped skips its gated tests for want of credentials, rather than silently picking up
/// whichever <c>TEST_KEY</c> another provider left in the environment.
/// </remarks>
/// <param name="scope">The prefix, without the trailing underscore.</param>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class TestEnvScopeAttribute(string scope) : Attribute
{
    /// <summary>Gets the prefix this assembly's environment variables carry.</summary>
    public string Scope { get; } = scope;
}
