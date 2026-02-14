using HorstMFG.Core.Entities;

namespace HorstMFG.Core.Interfaces;

public interface IOrderService
{
    Task<IEnumerable<Order>> GetOrdersAsync(int? plantId = null);
    Task<Order?> GetOrderByIdAsync(int id);
    Task<Order> CreateOrderAsync(Order order);
    Task UpdateOrderAsync(Order order);
    Task<IEnumerable<OrderItem>> GetOrderItemsAsync(int orderId);
}
