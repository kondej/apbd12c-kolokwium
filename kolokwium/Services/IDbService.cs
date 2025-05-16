using kolokwium.Models;

namespace kolokwium.Services;

public interface IDbService
{
    Task<DeliveryDto> GetDeliveryByIdAsync(int id);
    Task AddDeliveryAsync(DeliveryCreateDto delivery);
}
