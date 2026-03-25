using HorstMFG.Core.Entities;

namespace HorstMFG.Core.Interfaces;

public interface INestOrderService
{
    Task<IEnumerable<NestOrder>> GetOrdersAsync(int? plantId = null);
    Task<NestOrder?> GetOrderByIdAsync(int id);
    Task<NestOrder> CreateOrderAsync(NestOrder order);
    Task UpdateOrderAsync(NestOrder order);
    Task<IEnumerable<OrderItem>> GetOrderItemsAsync(int orderId);
}
