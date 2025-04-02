using InventoryApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly ILogger<ItemsController> _logger;
        private readonly IConfiguration _configuration;

        public InvoiceController(ILogger<ItemsController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        private string FullConnection(string serverName, string databaseName, string username, string password, string year)
        {
            string fullDatabaseName = $"{databaseName}Stock{year}";
            return $"Server={serverName};Database={fullDatabaseName};User Id={username};Password={password};TrustServerCertificate=True;";
        }


        [HttpPost("InvoiceData")]
        public async Task<IActionResult> CreateInvoice(
            [FromBody] InvoiceRequest invoiceRequest,
            [FromHeader(Name = "X-Api-Key")] string apiKey)
        {
            if (apiKey != "12345-ABCDE-67890-FGHIJ")
                return Unauthorized(new { error = "Unauthorized", message = "Invalid API key" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                return CreatedAtAction(nameof(CreateInvoice), new
                {
                    success = true,
                    message = "Invoice created successfully",
                    InvoiceID = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice");
                return StatusCode(500, new { success = false, message = $"Failed to create invoice: {ex.Message}" });
            }
        }
    }
}
