namespace kolokwium.Models;

public class DeliveryCreateDto
{
    public int DeliveryId { get; set; }
    public int CustomerId { get; set; }
    public string LicenceNumber { get; set; }
    public List<ProductDto> Products { get; set; }
}