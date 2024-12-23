using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace newRefreshTokenAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CurrenciesController : ControllerBase
    {
        private readonly string _connectionString;

        public CurrenciesController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("UserContext");
        }

        [HttpGet]
        public IActionResult GetCurrencies()
        {
            List<Currency> currencies = new List<Currency>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT ID, [CurrencyName] FROM tblCurrencies ";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Currency currency = new Currency
                                {
                                    ID = Convert.ToInt32(reader["ID"]),
                                    CurrencyName = reader["CurrencyName"].ToString()
                                };
                                currencies.Add(currency);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // تسجيل الخطأ
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }

            return Ok(currencies);
        }

        public class Currency
        {
            public int ID { get; set; }
            public string? CurrencyName { get; set; }
        }
    }
}
