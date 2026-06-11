using OpenAI.Chat;

namespace WeatherAgent.Api.Services
{
    public sealed class ChatHistoryTrimmer
    {
        private const long MaxInputTokens = 90_000;

        public void Trim(List<ChatMessage> history)
        {
            while (EstimateTokens(history) > MaxInputTokens && history.Count >= 2)
            {
                int idx = -1;
                for (int i = 0; i < history.Count - 1; i++)
                {
                    if (history[i] is UserChatMessage && history[i + 1] is AssistantChatMessage)
                    {
                        idx = i;
                        break;
                    }
                }

                if (idx < 0) break;

                int removeCount = 2;
                if (history[idx + 1] is AssistantChatMessage { ToolCalls.Count: > 0 } asst)
                {
                    for (int j = idx + 2; j < history.Count && removeCount - 2 < asst.ToolCalls.Count; j++)
                    {
                        if (history[j] is ToolChatMessage) removeCount++;
                        else break;
                    }
                }

                history.RemoveRange(idx, removeCount);
            }
        }

        private static long EstimateTokens(List<ChatMessage> messages)
        {
            long total = 0;
            foreach (var msg in messages)
                total += EstimateTokens(msg);
            return total;
        }

        private static long EstimateTokens(ChatMessage msg)
        {
            long t = 4;

            if (msg is UserChatMessage userMsg)
                t += EstimateTokens(userMsg.Content);
            else if (msg is AssistantChatMessage asstMsg)
                t += EstimateTokens(asstMsg.Content);
            else if (msg is ToolChatMessage toolMsg)
                t += EstimateTokens(toolMsg.Content);

            return t;
        }

        private static long EstimateTokens(IReadOnlyList<ChatMessageContentPart>? content)
        {
            if (content is null) return 0;
            long total = 0;
            foreach (var part in content)
            {
                if (part.Kind == ChatMessageContentPartKind.Text && part.Text is not null)
                    total += EstimateTokens(part.Text);
            }
            return total;
        }

        private static long EstimateTokens(string? text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int nonAscii = text.Count(c => c > 127);
            int ascii = text.Length - nonAscii;
            return (long)Math.Ceiling(ascii / 4.0 + nonAscii / 2.0);
        }
    }
}
