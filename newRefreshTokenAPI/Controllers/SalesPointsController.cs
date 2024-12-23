using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;

namespace newRefreshTokenAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesPointsController : ControllerBase
    {
        private readonly string _connectionString;

        public SalesPointsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("UserContext1");
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesPointsAsync()
        {
            List<SalePointUser> salesPointsUsers = new List<SalePointUser>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    string query = @"SELECT vewSalesPointsUsers.SalePointID, vewSalesPointsUsers.[رقم نقطة البيع] as SalePointNumber FROM vewSalesPointsUsers join 
                                    tblSalesPointsUsers on tblSalesPointsUsers.ID=vewSalesPointsUsers.ID";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                SalePointUser salePointUser = new SalePointUser
                                {
                                    SalePointID = Convert.ToInt32(reader["SalePointID"]),
                                    SalePointNumber = reader["SalePointNumber"].ToString()
                                };
                                salesPointsUsers.Add(salePointUser);
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

            return Ok(salesPointsUsers);
        }
        public class SalePointUser
        {
            public int SalePointID { get; set; }

            [Required(ErrorMessage = "رقم نقطة البيع مطلوب")]
            [StringLength(50, ErrorMessage = "رقم نقطة البيع يجب أن يكون أقل من 50 حرفًا")] // تعديل طول السلسلة حسب الحاجة
            public string? SalePointNumber { get; set; }
        }
    }
}
