namespace SimpleEventStore.Tests.Events
{
    public class OrderSnapshot
    {
        public string OrderId { get; private set; }

        public OrderSnapshot(string orderId)
        {
            OrderId = orderId;
        }
    }
}