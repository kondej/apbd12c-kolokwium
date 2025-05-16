using kolokwium.Models;

namespace kolokwium.Services;

public interface IDbService
{
    Task<DeliveryDto> GetDeliveryById(int id);
    Task AddDelivery(DeliveryCreateDto delivery);
}