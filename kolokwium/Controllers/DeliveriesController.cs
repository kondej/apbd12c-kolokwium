using kolokwium.Exceptions;
using kolokwium.Models;
using kolokwium.Services;
using Microsoft.AspNetCore.Mvc;

namespace kolokwium.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeliveriesController : ControllerBase
    {
        private readonly IDbService _dbService;

        public DeliveriesController(IDbService dbService)
        {
            _dbService = dbService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDeliveryById(int id)
        {
            try
            {
                var delivery = await _dbService.GetDeliveryById(id);
                return Ok(delivery);
            }
            catch (NotFoundException e)
            {
                return NotFound(e.Message);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddDelivery(DeliveryCreateDto delivery)
        {
            if (!delivery.Products.Any())
                return BadRequest("Trzeba podać przynajmniej jeden produkt!");

            try
            {
                await _dbService.AddDelivery(delivery);
                return CreatedAtAction(nameof(GetDeliveryById), new { id = delivery.DeliveryId }, delivery);
            }
            catch (ConflictException e)
            {
                return Conflict(e.Message);
            }
            catch (NotFoundException e)
            {
                return NotFound(e.Message);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}