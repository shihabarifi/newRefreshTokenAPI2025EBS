using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace newRefreshTokenAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersInfoController : ControllerBase
    {
        private readonly string _connectionString;

        public CustomersInfoController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("UserContext");
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomersInfoAsync()
        {
            List<CustomerInfo> customers = new List<CustomerInfo>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    string query = "SELECT ID, CustomerName FROM tblCustomersInfo";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                CustomerInfo customer = new CustomerInfo
                                {
                                    ID = Convert.ToInt32(reader["ID"]),
                                    CustomerName = reader["CustomerName"].ToString()
                                };
                                customers.Add(customer);
                            }
                        }
                    }
                }
                catch (SqlException sqlEx)
                {
                    // معالجة أخطاء SQL بشكل خاص
                    return StatusCode(500, $"Database error: {sqlEx.Message}");
                }
                catch (Exception ex)
                {
                    // معالجة الأخطاء الأخرى
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }

            return Ok(customers);
        }
        public class CustomerInfo
        {
            public int ID { get; set; }
            public string? CustomerName { get; set; }
        }
    }
}
