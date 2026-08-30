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
    /// Reads and parses `test.env` from the current working directory.
    /// </summary>
    static TestEnv()
    {
        var envFile = Path.Combine(Directory.GetCurrentDirectory(), "test.env");
        if (!File.Exists(envFile))
            throw new FileNotFoundException("Env file test.env not found. Use test.env.example as example", envFile);

        var variables = new Dictionary<string, string>();
        var raw = File.ReadAllLines(envFile)
            .Select(x => x.Split('='))
            .Where(x => x.Length == 2 && x[0].Trim().Length > 0)
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
