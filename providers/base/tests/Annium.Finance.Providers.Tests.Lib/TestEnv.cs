using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Annium.Finance.Providers.Tests.Lib;

/// <summary>
/// Reads exchange credentials and other secrets tests need from a `test.env` file in the working directory
/// (see `test.env.example` for the expected format), so they never end up hardcoded or checked into git.
/// </summary>
public static class TestEnv
{
    /// <summary>The key/value pairs parsed from `test.env`.</summary>
    private static readonly IReadOnlyDictionary<string, string> _envVariables;

    /// <summary>
    /// Gets a value indicating whether credentials were found. The file is gitignored and absent on a
    /// machine that has not been given one - CI included - so its absence is an ordinary state, not a
    /// failure: tests that need credentials skip themselves, rather than every test in the assembly
    /// dying in type initialization.
    /// </summary>
    public static bool IsAvailable => _envVariables.Count > 0;

    /// <summary>
    /// Reads and parses `test.env` from the current working directory, if there is one.
    /// </summary>
    static TestEnv()
    {
        var envFile = Path.Combine(Directory.GetCurrentDirectory(), "test.env");
        if (!File.Exists(envFile))
        {
            _envVariables = new Dictionary<string, string>();

            return;
        }

        // split on the first `=` only. Splitting on every one of them, and then keeping the lines that
        // came back in exactly two pieces, silently dropped any secret containing an `=` - base64
        // padding above all - so the variable was simply absent and the failure surfaced far from here
        // a key with nothing after the `=` is not a variable. Kept as an empty string it counted towards
        // IsAvailable, so a half-filled file read as credentials being present: the gated tests ran, and
        // GetVariable handed back "" instead of saying which variable was missing - a signature mismatch
        // far from the cause, rather than the clean skip the gate exists to give
        var variables = new Dictionary<string, string>();
        var raw = File.ReadAllLines(envFile)
            .Select(x => x.Split('=', 2))
            .Where(x => x.Length == 2 && x[0].Trim().Length > 0 && x[1].Trim().Length > 0)
            .Select(x => (x[0].Trim(), x[1].Trim()))
            .ToArray();
        foreach (var (key, value) in raw)
            variables[key] = value;

        _envVariables = variables;
    }

    /// <summary>
    /// Gets the value of a variable read from `test.env`.
    /// </summary>
    /// <param name="key">The variable's name.</param>
    /// <returns>The variable's value.</returns>
    /// <exception cref="KeyNotFoundException">The variable is not present in `test.env`.</exception>
    public static string GetVariable(string key)
    {
        if (!_envVariables.TryGetValue(key, out var value))
            throw new KeyNotFoundException($"Variable '{key}' not found.");
        return value;
    }
}
