namespace Circuit.Events
{
    internal class StaminaDrain : EventBase
    {
        public StaminaDrain(EventType eventType) : base(eventType) { }

        public override string GetDisplayName()
        {
            return "Stamina Drain";
        }

        public override string GetChatWarningMessage()
        {
            return "Everything you do starts to feel harder...";
        }

        public override string GetChatStartMessage()
        {
            return "Even standing is a struggle!";
        }

        public override string GetDescription()
        {
            return "All stamina usage is doubled.";
        }
    }
}
