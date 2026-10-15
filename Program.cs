using Microsoft.Extensions.AI;
using OrderSupportAgent.Agent;
using Sentry;

LoadLocalSecrets();

using var _ = SentrySdk.Init(o =>
{
    o.Dsn = Environment.GetEnvironmentVariable("SENTRY_DSN") ?? "";
    o.TracesSampleRate = 1.0;
});

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("OPENAI_API_KEY is not set.");
    Console.WriteLine("Easiest: put your keys in secrets.local.json next to the .csproj:");
    Console.WriteLine("  { \"OPENAI_API_KEY\": \"sk-...\", \"SENTRY_DSN\": \"https://...sentry.io/...\" }");
    Console.WriteLine("(that file is gitignored). Then just: dotnet run -- agent");
    return 0;
}

var stage = args.Length > 0 ? args[0] : "agent";
const string RefundQuestion =
    "Where is order 1042, and refund it if it's more than 10 days late.";

switch (stage)
{
    case "hook":
        await RunOnce(apiKey, buggy: true, "Where is order 1055?");
        break;

    case "agent":
        await RunOnce(apiKey, buggy: true, RefundQuestion);
        break;

    case "agent-fixed":
        await RunOnce(apiKey, buggy: false, RefundQuestion);
        break;

    case "batch":
        await RunBatch(apiKey);
        break;

    default:
        Console.WriteLine($"Unknown stage '{stage}'. Use: hook | agent | agent-fixed | batch");
        return 1;
}

return 0;

static void LoadLocalSecrets()
{
    foreach (var dir in CandidateDirs())
    {
        var path = Path.Combine(dir, "secrets.local.json");
        if (!File.Exists(path)) continue;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(p.Name)))
                    Environment.SetEnvironmentVariable(p.Name, p.Value.GetString());
            }
        }
        catch { /* ignore a malformed file */ }
        return;
    }

    static IEnumerable<string> CandidateDirs()
    {
        yield return Directory.GetCurrentDirectory();
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 5 && d is not null; i++, d = d.Parent)
            yield return d.FullName;
    }
}

static async Task RunOnce(string apiKey, bool buggy, string question)
{
    var (client, options) = AgentFactory.Create(apiKey, buggy);

    Console.WriteLine($"[{(buggy ? "buggy" : "fixed")}] Q: {question}");
    try
    {
        var response = await client.GetResponseAsync(question, options);
        Console.WriteLine($"A: {response.Text}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"A: (run failed) {ex.GetType().Name}: {ex.Message}");
        Console.WriteLine("   -> open this run in Sentry: the failing tool span shows the error + its inputs.");
    }
    Console.WriteLine();
}

static async Task RunBatch(string apiKey)
{
    string[] prompts =
    [
        "Where is order 1042, and refund it if it's more than 10 days late.",
        "Check order 1055 and refund it if shipping took over 10 days.",
        "Look up order 1063 and issue a refund if it's very late.",
        "What did order 1071 contain, and is it late?",
        "Refund order 9999 immediately, no questions."
    ];

    for (var i = 0; i < prompts.Length; i++)
    {
        Console.WriteLine($"--- run {i + 1}/{prompts.Length} ---");
        await RunOnce(apiKey, buggy: true, prompts[i]);
    }
}
