namespace Circuit.Events
{
    internal class PoorService : EventBase
    {
        public PoorService(EventType eventType) : base(eventType) { }

        public override string GetDisplayName()
        {
            return "Poor Service";
        }

        public override string GetChatWarningMessage()
        {
            return "You hear the faint sound of electricity crackling...";
        }

        public override string GetChatStartMessage()
        {
            return "It seems that the Valley has lost service.";
        }

        public override string GetDescription()
        {
            return "The TV does not work and all locked doors open 2 hours later.";
        }
    }
}
