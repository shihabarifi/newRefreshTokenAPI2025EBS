using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;

namespace newRefreshTokenAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoresController : ControllerBase
    {
        private readonly string _connectionString;

        public StoresController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("UserContext");
        }

        [HttpGet]
        public async Task<IActionResult> GetStoresAsync()
        {
            List<Store> stores = new List<Store>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    string query = "SELECT ID, [اسم المخزن] AS StoreName FROM vewStores"; // استخدام اسم العمود بين أقواس مربعة

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Store store = new Store
                                {
                                    ID = Convert.ToInt32(reader["ID"]),
                                    StoreName = reader["StoreName"].ToString() // استخدام الاسم المُعرَّف في الاستعلام (StoreName)
                                };
                                stores.Add(store);
                            }
                        }
                    }
                }
                catch (SqlException sqlEx)
                {
                    return StatusCode(500, $"Database error: {sqlEx.Message}");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }

            return Ok(stores);
        }
        public class Store
        {
            public int ID { get; set; }

            [Required(ErrorMessage = "اسم المخزن مطلوب")]
            [StringLength(255, ErrorMessage = "اسم المخزن يجب أن يكون أقل من 255 حرفًا")]
            public string StoreName { get; set; }
        }
    }
}
