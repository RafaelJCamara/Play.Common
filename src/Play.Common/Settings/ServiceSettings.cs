namespace Play.Common.Settings
{
    public class ServiceSettings
    {
        //We use init here because we don't want to change it after it is configured
        public string ServiceName { get; init; }
        public string Authority { get; init; }
        public string MessageBroker { get; init; }
    }
}
