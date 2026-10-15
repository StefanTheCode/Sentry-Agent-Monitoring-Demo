using Microsoft.Extensions.AI;
using OrderSupportAgent.Tools;

namespace OrderSupportAgent.Agent;

public static class AgentFactory
{
    public const string ModelId = "gpt-4o-mini";

    public static (IChatClient client, ChatOptions options) Create(string apiKey, bool buggy)
    {
        var openAi = new OpenAI.Chat.ChatClient(ModelId, apiKey)
            .AsIChatClient()
            .AddSentry(o =>
            {
                o.Experimental.AgentName = "OrderSupportAgent";
                o.Experimental.RecordInputs = true;
                o.Experimental.RecordOutputs = true;
            });

        var client = new ChatClientBuilder(openAi)
            .UseFunctionInvocation()
            .Build();

        var shipping = buggy
            ? AIFunctionFactory.Create(OrderTools.GetShippingStatusBuggy, "get_shipping_status")
            : AIFunctionFactory.Create(OrderTools.GetShippingStatusFixed, "get_shipping_status");

        var options = new ChatOptions
        {
            ModelId = ModelId,
            Tools =
            [
                AIFunctionFactory.Create(OrderTools.GetOrder, "get_order"),
                shipping,
                AIFunctionFactory.Create(OrderTools.IssueRefund, "issue_refund"),
            ]
        }.AddSentryToolInstrumentation();

        return (client, options);
    }
}
