using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Annium.Finance.Providers.Tests.Lib;

/// <summary>
/// Reads exchange credentials and other secrets tests need from a `test.env` file in the working directory
/// (see `test.env.example` for the expected format), falling back to process environment variables prefixed
/// with the assembly's <see cref="TestEnvScopeAttribute"/>, so they never end up hardcoded or checked into git.
/// </summary>
/// <remarks>
/// Two sources for two situations. Locally a `test.env` file is the convenient one: written once, gitignored,
/// copied next to the test binary by the project. In CI there is no file - writing one would put the secret
/// into the workspace and, through `CopyToOutputDirectory`, into every build output under it - so the same
/// secrets arrive as environment variables, which the runner masks in logs and which never touch disk.
///
/// The file wins where both carry a variable. Not the usual precedence - configuration normally lets the
/// environment override a file - but here the file is what someone configured deliberately, and a forgotten
/// <c>export BINANCE_SPOT_TEST_KEY=...</c> in a shell must not quietly redirect a trading test at a different
/// account.
/// </remarks>
public static class TestEnv
{
    /// <summary>The key/value pairs parsed from `test.env`.</summary>
    private static readonly IReadOnlyDictionary<string, string> _fileVariables;

    /// <summary>The prefix this assembly's environment variables carry, or null when none was declared.</summary>
    private static readonly string? _scope;

    /// <summary>
    /// Reads and parses `test.env` from the current working directory, if there is one, and picks up the
    /// environment scope the entry assembly declares, if it declares one.
    /// </summary>
    static TestEnv()
    {
        _scope = Assembly.GetEntryAssembly()?.GetCustomAttribute<TestEnvScopeAttribute>()?.Scope;
        _fileVariables = ReadFile(Path.Combine(Directory.GetCurrentDirectory(), "test.env"));
    }

    /// <summary>
    /// Gets a value indicating whether a variable can be resolved from either source.
    /// </summary>
    /// <remarks>
    /// Asked about the variables a test actually needs, rather than answered by counting what happened to
    /// be in the file. A count cannot span the environment - there is no set of names to count there - and
    /// it never said the right thing about the file either: any one entry made every gated test look
    /// runnable, so a half-filled file failed deep inside a request instead of skipping.
    /// </remarks>
    /// <param name="key">The variable's unqualified name, as it appears in `test.env`.</param>
    /// <returns><c>true</c> when the variable has a non-empty value in either source.</returns>
    public static bool Has(string key) => Resolve(key) is not null;

    /// <summary>
    /// Gets the value of a variable, from `test.env` if it is there and from the process environment otherwise.
    /// </summary>
    /// <param name="key">The variable's unqualified name, as it appears in `test.env`.</param>
    /// <returns>The variable's value.</returns>
    /// <exception cref="KeyNotFoundException">The variable is in neither source.</exception>
    public static string GetVariable(string key) => Resolve(key) ?? throw new KeyNotFoundException(Describe(key));

    /// <summary>
    /// Looks the variable up in `test.env` first, then in the scoped process environment.
    /// </summary>
    /// <param name="key">The variable's unqualified name.</param>
    /// <returns>The value, or <c>null</c> when neither source carries a non-empty one.</returns>
    private static string? Resolve(string key)
    {
        if (_fileVariables.TryGetValue(key, out var fromFile))
            return fromFile;

        if (_scope is null)
            return null;

        // the same rule the file parser applies: a variable set to blank is a variable that is not set,
        // and treating it as present is exactly how a half-filled source fails far from its cause
        var fromEnvironment = Environment.GetEnvironmentVariable($"{_scope}_{key}")?.Trim();

        return string.IsNullOrEmpty(fromEnvironment) ? null : fromEnvironment;
    }

    /// <summary>
    /// Builds the message for a variable that resolved from neither source, naming both places looked.
    /// </summary>
    /// <param name="key">The variable's unqualified name.</param>
    /// <returns>The message.</returns>
    private static string Describe(string key) =>
        _scope is null
            ? $"Variable '{key}' not found in test.env, and this assembly declares no [assembly: TestEnvScope] to read it from the environment."
            : $"Variable '{key}' found neither in test.env nor as environment variable '{_scope}_{key}'.";

    /// <summary>
    /// Parses a `test.env` file into its key/value pairs, or returns an empty set when there is no file.
    /// </summary>
    /// <param name="path">Full path to the file.</param>
    /// <returns>The variables the file declares.</returns>
    private static IReadOnlyDictionary<string, string> ReadFile(string path)
    {
        // the file is gitignored and absent on a machine that has not been given one - CI included - so its
        // absence is an ordinary state, not a failure: tests that need credentials skip themselves, rather
        // than every test in the assembly dying in type initialization
        if (!File.Exists(path))
            return new Dictionary<string, string>();

        // split on the first `=` only. Splitting on every one of them, and then keeping the lines that
        // came back in exactly two pieces, silently dropped any secret containing an `=` - base64
        // padding above all - so the variable was simply absent and the failure surfaced far from here
        // a key with nothing after the `=` is not a variable. Kept as an empty string it counted as
        // present, so a half-filled file read as credentials being there: the gated tests ran, and
        // GetVariable handed back "" instead of saying which variable was missing - a signature mismatch
        // far from the cause, rather than the clean skip the gate exists to give
        var variables = new Dictionary<string, string>();
        var raw = File.ReadAllLines(path)
            .Select(x => x.Split('=', 2))
            .Where(x => x.Length == 2 && x[0].Trim().Length > 0 && x[1].Trim().Length > 0)
            .Select(x => (x[0].Trim(), x[1].Trim()))
            .ToArray();
        foreach (var (key, value) in raw)
            variables[key] = value;

        return variables;
    }
}
