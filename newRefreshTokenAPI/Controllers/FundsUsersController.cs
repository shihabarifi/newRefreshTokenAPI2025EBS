using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;

namespace newRefreshTokenAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FundsUsersController : ControllerBase
    {
        private readonly string _connectionString;

        public FundsUsersController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("UserContext");
        }

        [HttpGet]
        public async Task<IActionResult> GetFundsUsersAsync()
        {
            List<FundUser> fundsUsers = new List<FundUser>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    string query = @" Select Distinct ID,Accountname as FundName From tblaccounts Where Len(Accountname)>0 AND 
                                      AccountType='فرعي' AND FatherNumber Like '1211%' Order BY Accountname  ";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                FundUser fundUser = new FundUser
                                {
                                    ID = Convert.ToInt32(reader["ID"]),
                                    FundName = reader["FundName"].ToString()
                                };
                                fundsUsers.Add(fundUser);
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

            return Ok(fundsUsers);
        }

        public class FundUser
        {
            public int ID { get; set; }

            [Required(ErrorMessage = "اسم الصندوق مطلوب")]
            [StringLength(255, ErrorMessage = "اسم الصندوق يجب أن يكون أقل من 255 حرفًا")]
            public string? FundName { get; set; }
        }
    }
}
