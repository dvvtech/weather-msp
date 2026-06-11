namespace WeatherAgent.Api.Models
{
    public sealed class SseEvent
    {
        public string Type { get; }
        public object Data { get; }

        public SseEvent(string type, object data)
        {
            Type = type;
            Data = data;
        }
    }
}
