using InventoryApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace InventoryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemsController : ControllerBase
    {
        private readonly ILogger<ItemsController> _logger;
        private readonly IConfiguration _configuration;

        public ItemsController(ILogger<ItemsController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        private string FullConnection(string serverName, string databaseName, string username, string password, string year)
        {
            string fullDatabaseName = $"{databaseName}Stock{year}";
            return $"Server={serverName};Database={fullDatabaseName};User Id={username};Password={password};TrustServerCertificate=True;";
        }

     
        [HttpGet("Items")]
        public async Task<IActionResult> GetItems(
            [FromHeader(Name = "X-Api-Key")] string apiKey,
            [FromHeader(Name = "servername")] string serverName,
            [FromHeader(Name = "databasename")] string databaseName,
            [FromHeader(Name = "username")] string username,
            [FromHeader(Name = "Year")] string year,
            [FromHeader(Name = "password")] string? password = null)
        {
            if (apiKey != "12345-ABCDE-67890-FGHIJ")
                return Unauthorized(new { error = "Unauthorized", message = "Invalid API key" });

            try
            {
                var connectionString = FullConnection(serverName, databaseName, username, password, year);

                var connectionInfo = new
                {
                    server = serverName,
                    database = $"{databaseName}Stock{year}",
                    user = username,
                   
                    connectionStringLength = connectionString.Length
                };
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"
                        SELECT DISTINCT
                            ItemFile.ItemNum,
                            ItemFile.ItemName,
                            ItemFile.SalePrice AS Price,
                            'http://dddsoft-001-site3.dtempurl.com/image/' + 
                                CASE 
                                    WHEN ItemFile.ItemPictures IS NULL THEN 'default'
                                    ELSE LEFT(ItemFile.ItemPictures, CHARINDEX('.', ItemFile.ItemPictures) - 1)
                                END AS ImageUrl,
                            ItemFile.ItemID,
                            UnitFile2.UnitDesc AS ItemNumUnit,  
                            UnitFile1.UnitDesc AS UnitDesc,    
                            ItemBarcode.Barcode,
                            ItemBarcode.SalePrice AS BarcodePrice
                        FROM  
                            UnitFile AS UnitFile1
                        RIGHT OUTER JOIN
                            ItemBarcode ON UnitFile1.UnitID = ItemBarcode.UnitID
                        RIGHT OUTER JOIN
                            ItemFile ON ItemBarcode.ItemID = ItemFile.ItemID
                        RIGHT OUTER JOIN
                            UnitFile AS UnitFile2 ON UnitFile2.UnitID = ItemFile.UnitIDSel";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandTimeout = 60;
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            try
                            {

                                var items = new List<dynamic>();
                                while (await reader.ReadAsync())
                                {
                                    items.Add(new
                                    {
                                        ItemNum = reader["ItemNum"]?.ToString() ?? string.Empty,
                                        ItemName = reader["ItemName"]?.ToString() ?? string.Empty,
                                        Price = reader.IsDBNull("Price") ? 0m : Convert.ToDecimal(reader["Price"]),
                                        ImageUrl = reader["ImageUrl"]?.ToString() ?? string.Empty,
                                        ItemID = reader.IsDBNull("ItemID") ? 0 : Convert.ToInt32(reader["ItemID"]),
                                        ItemNumUnit = reader["ItemNumUnit"]?.ToString() ?? string.Empty,
                                        UnitDesc = reader["UnitDesc"]?.ToString() ?? string.Empty,
                                        Barcode = reader["Barcode"]?.ToString() ?? string.Empty,
                                        BarcodePrice = reader.IsDBNull("BarcodePrice") ? 0m : Convert.ToDecimal(reader["BarcodePrice"])
                                    });
                                }
                                return Ok(new { data_query1 = items });
                            }
                            catch (SqlException sqlEx)
                            {
                                _logger.LogError("SQL error: {message}, Number: {number}", sqlEx.Message, sqlEx.Number);
                                return StatusCode(500, new { error = "Database Connection Error", message = sqlEx.Message, number = sqlEx.Number });

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving items");
                return StatusCode(500, new
                {
                    error = "Internal Server Error",
                    message = ex.Message,
                    stackTrace = ex.StackTrace,
                    innerException = ex.InnerException?.Message
                });
            }
        }


    }
}
