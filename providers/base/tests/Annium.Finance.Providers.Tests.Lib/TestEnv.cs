using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Annium.Finance.Providers.Tests.Lib;

public static class TestEnv
{
    private static readonly IReadOnlyDictionary<string, string> _envVariables;

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

    public static string GetVariable(string key)
    {
        if (!_envVariables.TryGetValue(key, out var value))
            throw new KeyNotFoundException($"Variable '{key}' not found.");
        return value;
    }
}
