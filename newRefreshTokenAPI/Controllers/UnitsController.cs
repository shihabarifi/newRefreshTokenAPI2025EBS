using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;

namespace newRefreshTokenAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnitsController : ControllerBase
    {
        private readonly string _connectionString;

        public UnitsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("UserContext1");
        }

        [HttpGet("{classId}")] // إضافة مسار لتمرير ClassID كمعامل
        [Authorize]
        public async Task<IActionResult> GetUnitsByClassIdAsync(long classId)
        {
            List<Unit> units = new List<Unit>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    // استخدام معلمات SQL لمنع حقن SQL
                    string query = "SELECT ID, UnitName, ExchangeFactor,PartingPrice FROM tblUnits WHERE ClassID = @ClassID order by ExchangeFactor ";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ClassID", classId); // إضافة المعامل

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Unit unit = new Unit
                                {
                                    ID = Convert.ToInt32(reader["ID"]),
                                    UnitName = reader["UnitName"].ToString(),
                                    ExchangeFactor = Convert.ToDouble(reader["ExchangeFactor"]) ,// أو Convert.ToDouble
                                    PartingPrice= Convert.ToDouble(reader["PartingPrice"]) // أو Convert.ToDouble
                                };
                                units.Add(unit);
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

            return Ok(units);
        }
        public class Unit
        {
            public int ID { get; set; }

            [Required(ErrorMessage = "اسم الوحدة مطلوب")]
            [StringLength(255, ErrorMessage = "اسم الوحدة يجب أن يكون أقل من 255 حرفًا")]
            public string? UnitName { get; set; }

            public double ExchangeFactor { get; set; } // أو double حسب نوع البيانات في قاعدة البيانات
            public double PartingPrice { get; set; } // أو double حسب نوع البيانات في قاعدة البيانات
            
        }
    }
}
