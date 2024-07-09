// namespace Debounce.Api;
//
// public class OrderEventMessageHandler : IEventMessageHandler<Order>
// {
//     private readonly ILogger<OrderEventMessageHandler> _logger;
//
//     public OrderEventMessageHandler(ILogger<OrderEventMessageHandler> logger)
//     {
//         _logger = logger;
//     }
//
//     public void Handle(Order order)
//     {
//         _logger.LogInformation($"Order received: {order.Id}");
//         // Handle order
//     }
// }
