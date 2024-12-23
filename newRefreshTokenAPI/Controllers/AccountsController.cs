using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace newRefreshTokenAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly string _connectionString;

        public AccountsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("UserContext");
        }

        [HttpGet]
        public IActionResult GetAccounts()
        {
            List<Account> accounts = new List<Account>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT ID, AccountName FROM tblAccounts"; // استعلام مُحدّث

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Account account = new Account
                                {
                                    ID = Convert.ToInt32(reader["ID"]),
                                    AccountName = reader["AccountName"].ToString()
                                };
                                accounts.Add(account);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }

            return Ok(accounts);
        }
        public class Account
        {
            public int ID { get; set; }
            public string? AccountName { get; set; }
        }
    }
}
