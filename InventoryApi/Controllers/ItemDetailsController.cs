using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;

namespace InventoryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemDetailsController : ControllerBase
    {

        private readonly ILogger<ItemsController> _logger;
        private readonly IConfiguration _configuration;

        public ItemDetailsController(ILogger<ItemsController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        private string FullConnection(string serverName, string databaseName, string username, string password, string year)
        {
            string fullDatabaseName = $"{databaseName}Stock{year}";
            return $"Server={serverName};Database={fullDatabaseName};User Id={username};Password={password};TrustServerCertificate=True;";
        }


        [HttpGet("ItemDetails")]
        public async Task<IActionResult> GetPrice(

            [FromQuery] string itemBarcode,
            [FromQuery] int currencyID,
            [FromHeader(Name = "X-Api-Key")] string apiKey,
            [FromHeader(Name = "servername")] string serverName,
            [FromHeader(Name = "databasename")] string databaseName,
            [FromHeader(Name = "username")] string username,
            [FromHeader(Name = "Year")] string year,
            [FromHeader(Name = "password")] string? password = null)
        {
            if (apiKey != "12345-ABCDE-67890-FGHIJ")
                return Unauthorized(new { error = "Unauthorized", message = "Invalid API key" });

            if (string.IsNullOrEmpty(itemBarcode) ||
                string.IsNullOrEmpty(serverName) ||
                string.IsNullOrEmpty(databaseName) ||
                string.IsNullOrEmpty(year))
            {
                return BadRequest(new { error = "Bad Request", message = "Missing required parameters" });
            }

            int[] validCurrencyIDs = { 1, 2 };
            if (!validCurrencyIDs.Contains(currencyID))
                return BadRequest(new { success = false, message = "Invalid CurrencyID (valid: 1, 2)" });

            try
            {
                var connectionString = FullConnection(serverName, databaseName, username, password, year);
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    decimal rate = 90000m;
                    if (currencyID != 1)
                    {
                        var rateCommand = new SqlCommand("OKSAccount2024.dbo.GetLatestCurrencyRate", connection);
                        rateCommand.CommandType = CommandType.StoredProcedure;
                        rateCommand.Parameters.AddWithValue("@CurrencyID", currencyID);
                        var rateParam = rateCommand.Parameters.Add("@CurrencyRate", SqlDbType.Decimal);
                        rateParam.Direction = ParameterDirection.Output;

                        await rateCommand.ExecuteNonQueryAsync();
                        rate = rateParam.Value != DBNull.Value ? Convert.ToDecimal(rateParam.Value) : rate;
                    }

                    var query = @"
                        SELECT 
                            i.ItemID,
                            i.ItemNum,
                            i.ItemName,
                            COALESCE(b.SalePrice, i.SalePrice) AS BasePrice,
                            COALESCE(b.SalePrice, i.SalePrice) * @Rate AS ConvertedPrice,
                            u.UnitDesc
                        FROM ItemFile i
                        LEFT JOIN ItemBarcode b ON b.ItemID = i.ItemID AND b.Barcode = @Barcode
                        LEFT JOIN UnitFile u ON u.UnitID = i.UnitIDSel
                        WHERE i.ItemNum = @Barcode 
                           OR i.ItemEquivalence = @Barcode 
                           OR i.Reference = @Barcode
                           OR b.Barcode = @Barcode";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandTimeout = 60;
                        command.Parameters.AddWithValue("@Barcode", itemBarcode);
                        command.Parameters.AddWithValue("@Rate", rate);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return Ok(new
                                {
                                    success = true,
                                    data = new
                                    {
                                        ItemID = reader["ItemID"],
                                        ItemNum = reader["ItemNum"].ToString(),
                                        ItemName = reader["ItemName"].ToString(),
                                        BasePrice = reader.IsDBNull("BasePrice") ? 0m : Convert.ToDecimal(reader["BasePrice"]),
                                        ConvertedPrice = reader.IsDBNull("ConvertedPrice") ? 0m : Convert.ToDecimal(reader["ConvertedPrice"]),
                                        Unit = reader["UnitDesc"].ToString(),
                                        CurrencyRate = rate,
                                        CurrencyID = currencyID
                                    }
                                });
                            }
                            return NotFound(new { success = false, message = "Item not found" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving item price");
                return StatusCode(500, new { success = false, message = $"Failed to execute price query: {ex.Message}" });
            }
        }


    }
}
