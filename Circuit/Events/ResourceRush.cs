namespace Circuit.Events
{
    internal class ResourceRush : EventBase
    {
        public ResourceRush(EventType eventType) : base(eventType) { }

        public override string GetDisplayName()
        {
            return "Resource Rush";
        }

        public override string GetChatWarningMessage()
        {
            return "The ground seems to be rumbling...";
        }

        public override string GetChatStartMessage()
        {
            return "A loud collapse was heard from the mine.";
        }

        public override string GetDescription()
        {
            return "The mines has increased gem node and ore node spawn rates.";
        }
    }
}
