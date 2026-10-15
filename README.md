# OrderSupportAgent - .NET AI agent with Sentry Agent Monitoring

A tiny but real order-support agent built with `Microsoft.Extensions.AI`, instrumented with
Sentry's Agent Monitoring for .NET (`Sentry.Extensions.AI`). Every model call and tool call
shows up in Sentry as a span.

## Quickstart (2 minutes)

1. Create a Sentry project (platform: .NET) and copy its DSN. Get an OpenAI API key.
2. Set two env vars (never hardcode keys - this repo is public):
   ```
   setx OPENAI_API_KEY "sk-..."     # open a NEW terminal after setx
   setx SENTRY_DSN "https://...ingest.sentry.io/..."
   ```
3. Run it:
   ```
   cd demo/OrderSupportAgent
   dotnet run -- agent         # buggy build: a tool throws on late orders
   dotnet run -- agent-fixed   # fixed build: the tool handles them
   dotnet run -- batch         # several runs to populate Sentry
   ```
4. Open Sentry -> Explore -> Agents (or Explore -> Traces) and look at the run.

## What's inside

- `Agent/AgentFactory.cs` - builds the `IChatClient`. Note the order: `AddSentry(...)` on the
  inner client, THEN `UseFunctionInvocation()`. If you AddSentry on a client that already has
  function invocation, the spans do not show up correctly.
- `Tools/OrderTools.cs` - `get_order`, `get_shipping_status` (buggy vs fixed), `issue_refund`
  over fake in-memory data. The buggy `get_shipping_status` throws on orders older than 10 days
  (a planted "legacy tracking format" bug), so order 1042 fails every time.
- `Program.cs` - `SentrySdk.Init` (tracing on) + the `agent | agent-fixed | batch` stages.
- `ci-workflow/agent-runs.yml` - optional GitHub Actions workflow (move it to
  `.github/workflows/` on GitHub) that runs `dotnet run -- batch` with repo secrets.

## Notes

- The Sentry AI agent module is **experimental** (analyzer id `SENTRY0001`, opted in via
  `<NoWarn>` in the .csproj).
- Target framework: `net9.0`. On .NET 10, bump `<TargetFramework>` to `net10.0`.
