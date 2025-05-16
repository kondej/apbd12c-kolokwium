

using System.Data;
using System.Data.Common;
using kolokwium.Exceptions;
using kolokwium.Models;
using Microsoft.Data.SqlClient;

namespace kolokwium.Services;

public class DbService : IDbService
{
    private readonly string _connectionString;

    public DbService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default") ?? string.Empty;
    }

    public async Task<DeliveryDto> GetDeliveryById(int id)
    {
        var query = @"SELECT del.date, c.first_name, c.last_name, c.date_of_birth, 
                            drv.first_name, drv.last_name, drv.licence_number, p.name, p.price, pd.amount
                      FROM Delivery del
                      JOIN Customer c on c.customer_id = del.customer_id
                      JOIN Driver drv on del.driver_id = drv.driver_id
                      JOIN Product_Delivery pd on pd.delivery_id = del.delivery_id
                      JOIN Product p on p.product_id = pd.product_id
                      WHERE del.delivery_id = @DeliveryId";
        
        await using SqlConnection connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        command.CommandText = query;
        await connection.OpenAsync();
        
        command.Parameters.AddWithValue("@DeliveryId", id);
        var reader = await command.ExecuteReaderAsync();

        DeliveryDto? delivery = null;
        while (await reader.ReadAsync())
        {
            if (delivery == null)
            {
                delivery = new DeliveryDto
                {
                    Date = reader.GetDateTime(0),
                    Customer = new CustomerDto
                    {
                        FirstName = reader.GetString(1),
                        LastName = reader.GetString(2),
                        DateOfBirth = reader.GetDateTime(3),
                    },
                    Driver = new DriverDto
                    {
                        FirstName = reader.GetString(4),
                        LastName = reader.GetString(5),
                        LicenceNumber = reader.GetString(6),
                    },
                    Products = new List<ProductDto>()
                };
            }
            delivery.Products.Add(new ProductDto
            {
                Name = reader.GetString(7),
                Price = reader.GetDecimal(8),
                Amount = reader.GetInt32(9)
            });
        }

        if (delivery == null)
            throw new NotFoundException("Nie znaleziono dostawy!");
        
        return delivery;
    }

    public async Task AddDelivery(DeliveryCreateDto delivery)
    {
        await using SqlConnection connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();
        
        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        try
        {
            command.Parameters.Clear();
            command.CommandText = "SELECT 1 FROM Delivery WHERE delivery_id = @DeliveryId;";
            command.Parameters.AddWithValue("@DeliveryId", delivery.DeliveryId);
            if (await command.ExecuteScalarAsync() != null)
                throw new ConflictException($"Dostawa o id {delivery.DeliveryId} już istnieje!");
            
            command.Parameters.Clear();
            command.CommandText = "SELECT 1 FROM Customer WHERE customer_id = @CustomerId;";
            command.Parameters.AddWithValue("@CustomerId", delivery.CustomerId);
            if (await command.ExecuteScalarAsync() == null)
                throw new NotFoundException($"Klient o id {delivery.CustomerId} nie istnieje!");
            
            command.Parameters.Clear();
            command.CommandText = "SELECT driver_id FROM Driver WHERE licence_number = @LicenceNumber;";
            command.Parameters.AddWithValue("@LicenceNumber", delivery.LicenceNumber);

            var driverId = await command.ExecuteScalarAsync();
            
            if (driverId == null)
                throw new NotFoundException($"Kierowca o numerze {delivery.LicenceNumber} nie istnieje!");
            
            command.Parameters.Clear();
            command.CommandText = "INSERT INTO Delivery VALUES(@DeliveryId, @CustomerId, @DriverId, @Date)";
            command.Parameters.AddWithValue("@DeliveryId", delivery.DeliveryId);
            command.Parameters.AddWithValue("@CustomerId", delivery.CustomerId);
            command.Parameters.AddWithValue("@DriverId", driverId);
            command.Parameters.AddWithValue("@Date", DateTime.Now);
            
            await command.ExecuteNonQueryAsync();

            foreach (var product in delivery.Products)
            {
                command.Parameters.Clear();
                command.CommandText = "SELECT product_id FROM Product WHERE name = @Name;";
                command.Parameters.AddWithValue("@Name", product.Name);

                var productId = await command.ExecuteScalarAsync();
                
                if (productId == null)
                    throw new NotFoundException($"Produkt o nazwie {product.Name} nie istnieje!");
                
                command.Parameters.Clear();
                command.CommandText = "INSERT INTO Product_Delivery VALUES(@ProductId, @DeliveryId, @Amount)";
                command.Parameters.AddWithValue("@ProductId", productId);
                command.Parameters.AddWithValue("@DeliveryId", delivery.DeliveryId);
                command.Parameters.AddWithValue("@Amount", product.Amount);
                
                await command.ExecuteNonQueryAsync();
            }
            
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}