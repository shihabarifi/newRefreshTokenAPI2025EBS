using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace newRefreshTokenAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly string _connectionString;

        public ProductController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("UserContext");
        }

        [HttpGet]
        public async Task<IActionResult> GetClassesAsync()
        {
            List<Product> classes = new List<Product>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    string query = "SELECT top 10  ID, ClassName FROM tblClasses";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Product classItem = new Product
                                {
                                    ID = Convert.ToInt32(reader["ID"]),
                                    ClassName = reader["ClassName"].ToString()
                                };
                                classes.Add(classItem);
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

            return Ok(classes);
        }
        public class Product
        {
            public int ID { get; set; }

            [Required(ErrorMessage = "اسم الصف مطلوب")]
            [StringLength(255, ErrorMessage = "اسم الصف يجب أن يكون أقل من 255 حرفًا")]
            public string? ClassName { get; set; }
        }
    }
}
