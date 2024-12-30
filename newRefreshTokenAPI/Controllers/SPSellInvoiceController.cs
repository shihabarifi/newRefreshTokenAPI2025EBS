using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Transactions;

namespace newRefreshTokenAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SPSellInvoiceController : ControllerBase
    {
        private readonly string _connectionString;

        public SPSellInvoiceController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("UserContext1");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddInvoiceAsync(SPSellInvoice invoice)
        {
            if (invoice == null || invoice.InvoiceDetails == null || invoice.InvoiceDetails.Count == 0)
            {
                return BadRequest("Invoice data is invalid.");
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)) // استخدام TransactionScope
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    // 1. إضافة الفاتورة الرئيسية
                    string invoiceQuery = @"
                    INSERT INTO tblSPSellInvoice (PeriodNumber, SalePointID, TheNumber, TheDate, ThePay, StoreID, AccountID, CustomerName, Notes, UserID, Descount, Debited, PayAmount)
                    VALUES (@PeriodNumber, @SalePointID, @TheNumber, @TheDate, @ThePay, @StoreID, @AccountID, @CustomerName, @Notes, @UserID, @Descount, @Debited, @PayAmount);
                    SELECT SCOPE_IDENTITY();"; // لجلب ID الفاتورة المُضافة

                    int invoiceId;
                    using (SqlCommand invoiceCommand = new SqlCommand(invoiceQuery, connection))
                    {
                        invoiceCommand.Parameters.AddWithValue("@PeriodNumber", invoice.PeriodNumber);
                        invoiceCommand.Parameters.AddWithValue("@SalePointID", invoice.SalePointID);
                        invoiceCommand.Parameters.AddWithValue("@TheNumber", invoice.TheNumber ?? "");
                        invoiceCommand.Parameters.AddWithValue("@TheDate", invoice.TheDate );
                        invoiceCommand.Parameters.AddWithValue("@ThePay", invoice.ThePay);
                        invoiceCommand.Parameters.AddWithValue("@StoreID", invoice.StoreID);
                        invoiceCommand.Parameters.AddWithValue("@AccountID", invoice.AccountID);
                        invoiceCommand.Parameters.AddWithValue("@CustomerName", invoice.CustomerName ?? "");
                        invoiceCommand.Parameters.AddWithValue("@Notes", invoice.Notes ?? ""); // التعامل مع القيم الفارغة
                        invoiceCommand.Parameters.AddWithValue("@UserID", invoice.UserID );
                        invoiceCommand.Parameters.AddWithValue("@Descount", invoice.Descount);
                        invoiceCommand.Parameters.AddWithValue("@Debited", invoice.Debited);
                        invoiceCommand.Parameters.AddWithValue("@PayAmount", invoice.PayAmount);

                        invoiceId = Convert.ToInt32(await invoiceCommand.ExecuteScalarAsync());
                    }

                    // 2. إضافة تفاصيل الفاتورة
                    string detailsQuery = @"
                    INSERT INTO tblSPSellInvoiceDetailes (ParentID, ClassID, UnitID, Quantity, UnitPrice, SubDescount, TotalAMount)
                    VALUES (@ParentID, @ClassID, @UnitID, @Quantity, @UnitPrice, @SubDescount, @TotalAMount)";

                    using (SqlCommand detailsCommand = new SqlCommand(detailsQuery, connection))
                    {
                        foreach (var detail in invoice.InvoiceDetails)
                        {
                            detailsCommand.Parameters.Clear(); // مهم جداً لمسح المعلمات في كل تكرار
                            detailsCommand.Parameters.AddWithValue("@ParentID", invoiceId);
                            detailsCommand.Parameters.AddWithValue("@ClassID", detail.ClassID);
                            detailsCommand.Parameters.AddWithValue("@UnitID", detail.UnitID);
                            detailsCommand.Parameters.AddWithValue("@Quantity", detail.Quantity);
                            detailsCommand.Parameters.AddWithValue("@UnitPrice", detail.UnitPrice);
                            detailsCommand.Parameters.AddWithValue("@SubDescount", detail.SubDescount);
                            detailsCommand.Parameters.AddWithValue("@TotalAMount", detail.TotalAMount);

                            await detailsCommand.ExecuteNonQueryAsync();
                        }
                    }

                    scope.Complete(); // إتمام المعاملة في حالة النجاح

                    return Ok(new { id = invoiceId , Success =true}); // إرجاع ID الفاتورة التي تم إنشاؤها
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
        }


        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateInvoiceAsync(int id, SPSellInvoice updatedInvoice)
        {
            if (id != updatedInvoice.ID)
            {
                return BadRequest("ID mismatch");
            }

            if (updatedInvoice == null || updatedInvoice.InvoiceDetails == null)
            {
                return BadRequest("Invoice data is invalid.");
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    // 1. تحديث الفاتورة الرئيسية
                    string updateInvoiceQuery = @"
                    UPDATE tblSPSellInvoice
                    SET PeriodNumber = @PeriodNumber, SalePointID = @SalePointID, TheNumber = @TheNumber, TheDate = @TheDate,
                        ThePay = @ThePay, StoreID = @StoreID, AccountID = @AccountID, CustomerName = @CustomerName,
                        Notes = @Notes, UserID = @UserID, Descount = @Descount, Debited = @Debited, PayAmount = @PayAmount
                    WHERE ID = @ID;";

                    using (SqlCommand updateInvoiceCommand = new SqlCommand(updateInvoiceQuery, connection))
                    {
                        updateInvoiceCommand.Parameters.AddWithValue("@ID", id);
                        updateInvoiceCommand.Parameters.AddWithValue("@PeriodNumber", updatedInvoice.PeriodNumber ?? "");
                        updateInvoiceCommand.Parameters.AddWithValue("@SalePointID", updatedInvoice.SalePointID);
                        updateInvoiceCommand.Parameters.AddWithValue("@TheNumber", updatedInvoice.TheNumber ?? "");
                        updateInvoiceCommand.Parameters.AddWithValue("@TheDate", updatedInvoice.TheDate);
                        updateInvoiceCommand.Parameters.AddWithValue("@ThePay", updatedInvoice.ThePay);
                        updateInvoiceCommand.Parameters.AddWithValue("@StoreID", updatedInvoice.StoreID);
                        updateInvoiceCommand.Parameters.AddWithValue("@AccountID", updatedInvoice.AccountID);
                        updateInvoiceCommand.Parameters.AddWithValue("@CustomerName", updatedInvoice.CustomerName ?? "");
                        updateInvoiceCommand.Parameters.AddWithValue("@Notes", updatedInvoice.Notes ?? "");
                        updateInvoiceCommand.Parameters.AddWithValue("@UserID", updatedInvoice.UserID);
                        updateInvoiceCommand.Parameters.AddWithValue("@Descount", updatedInvoice.Descount);
                        updateInvoiceCommand.Parameters.AddWithValue("@Debited", updatedInvoice.Debited);
                        updateInvoiceCommand.Parameters.AddWithValue("@PayAmount", updatedInvoice.PayAmount);

                        int rowsAffected = await updateInvoiceCommand.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            return NotFound(); // لم يتم العثور على الفاتورة للتحديث
                        }
                    }

                    // 2. حذف تفاصيل الفاتورة القديمة
                    string deleteDetailsQuery = "DELETE FROM tblSPSellInvoiceDetailes WHERE ParentID = @ParentID;";
                    using (SqlCommand deleteDetailsCommand = new SqlCommand(deleteDetailsQuery, connection))
                    {
                        deleteDetailsCommand.Parameters.AddWithValue("@ParentID", id);
                        await deleteDetailsCommand.ExecuteNonQueryAsync();
                    }

                    // 3. إضافة تفاصيل الفاتورة الجديدة
                    string insertDetailsQuery = @"
                    INSERT INTO tblSPSellInvoiceDetailes (ParentID, ClassID, UnitID, Quantity, UnitPrice, SubDescount, TotalAMount)
                    VALUES (@ParentID, @ClassID, @UnitID, @Quantity, @UnitPrice, @SubDescount, @TotalAMount);";

                    using (SqlCommand insertDetailsCommand = new SqlCommand(insertDetailsQuery, connection))
                    {
                        foreach (var detail in updatedInvoice.InvoiceDetails)
                        {
                            insertDetailsCommand.Parameters.Clear();
                            insertDetailsCommand.Parameters.AddWithValue("@ParentID", id);
                            insertDetailsCommand.Parameters.AddWithValue("@ClassID", detail.ClassID);
                            insertDetailsCommand.Parameters.AddWithValue("@UnitID", detail.UnitID);
                            insertDetailsCommand.Parameters.AddWithValue("@Quantity", detail.Quantity);
                            insertDetailsCommand.Parameters.AddWithValue("@UnitPrice", detail.UnitPrice);
                            insertDetailsCommand.Parameters.AddWithValue("@SubDescount", detail.SubDescount);
                            insertDetailsCommand.Parameters.AddWithValue("@TotalAMount", detail.TotalAMount);

                            await insertDetailsCommand.ExecuteNonQueryAsync();
                        }
                    }

                    scope.Complete();
                    return Ok(new { id = updatedInvoice.ID, Success = true }); // إرجاع ID الفاتورة التي تم تعديلها
                  
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
        }


        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteInvoiceAsync(int id)
        {
            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    // 1. حذف تفاصيل الفاتورة
                    string deleteDetailsQuery = "DELETE FROM tblSPSellInvoiceDetailes WHERE ParentID = @ParentID;";
                    using (SqlCommand deleteDetailsCommand = new SqlCommand(deleteDetailsQuery, connection))
                    {
                        deleteDetailsCommand.Parameters.AddWithValue("@ParentID", id);
                        await deleteDetailsCommand.ExecuteNonQueryAsync();
                    }

                    // 2. حذف الفاتورة الرئيسية
                    string deleteInvoiceQuery = "DELETE FROM tblSPSellInvoice WHERE ID = @ID;";
                    using (SqlCommand deleteInvoiceCommand = new SqlCommand(deleteInvoiceQuery, connection))
                    {
                        deleteInvoiceCommand.Parameters.AddWithValue("@ID", id);

                        int rowsAffected = await deleteInvoiceCommand.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            return NotFound(); // لم يتم العثور على الفاتورة للحذف
                        }
                    }

                    scope.Complete();
                    return Ok(new { id = id, Success = true }); // إرجاع ID الفاتورة التي تم حذفها
                   // return NoContent(); // إرجاع 204 No Content عند النجاح
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
        }

        [HttpGet("{id}")] // إضافة مسار لتمرير ID الفاتورة كمعامل
       
        public async Task<IActionResult> GetInvoiceByIdAsync(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid Invoice ID.");
            }

            SPSellInvoice invoice = null;
            List<SPSellInvoiceDetails> details = new List<SPSellInvoiceDetails>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    // 1. جلب بيانات الفاتورة الرئيسية
                    string invoiceQuery = @"
                    SELECT PeriodNumber, SalePointID, TheNumber, TheDate, ThePay, StoreID, AccountID, CustomerName, Notes, UserID, Descount, Debited, PayAmount
                    FROM tblSPSellInvoice
                    WHERE ID = @ID";

                    using (SqlCommand invoiceCommand = new SqlCommand(invoiceQuery, connection))
                    {
                        invoiceCommand.Parameters.AddWithValue("@ID", id);

                        using (SqlDataReader invoiceReader = await invoiceCommand.ExecuteReaderAsync())
                        {
                            if (await invoiceReader.ReadAsync())
                            {
                                invoice = new SPSellInvoice
                                {
                                    ID = id, // تعيين ID هنا
                                    PeriodNumber = invoiceReader["PeriodNumber"].ToString(),
                                    SalePointID = Convert.ToInt32(invoiceReader["SalePointID"]),
                                    TheNumber = invoiceReader["TheNumber"].ToString(),
                                    TheDate = Convert.ToDateTime(invoiceReader["TheDate"]),
                                    ThePay = invoiceReader["ThePay"].ToString(),
                                    StoreID = Convert.ToInt32(invoiceReader["StoreID"]),
                                    AccountID = Convert.ToInt32(invoiceReader["AccountID"]),
                                    CustomerName = invoiceReader["CustomerName"].ToString(),
                                    Notes = invoiceReader["Notes"].ToString(),
                                    UserID = Convert.ToInt32(invoiceReader["UserID"]),
                                    Descount = Convert.ToDecimal(invoiceReader["Descount"]),
                                    Debited = Convert.ToDecimal(invoiceReader["Debited"]),
                                    PayAmount = Convert.ToDecimal(invoiceReader["PayAmount"])
                                };
                            }
                            else
                            {
                                return NotFound($"Invoice with ID {id} not found."); // مهم للارجاع في حالة عدم وجود الفاتورة
                            }
                        }
                    }

                    // 2. جلب تفاصيل الفاتورة
                    string detailsQuery = @"
                    SELECT ID, ClassID, UnitID, Quantity, UnitPrice, SubDescount, TotalAMount
                    FROM tblSPSellInvoiceDetailes
                    WHERE ParentID = @ParentID";

                    using (SqlCommand detailsCommand = new SqlCommand(detailsQuery, connection))
                    {
                        detailsCommand.Parameters.AddWithValue("@ParentID", id);

                        using (SqlDataReader detailsReader = await detailsCommand.ExecuteReaderAsync())
                        {
                            while (await detailsReader.ReadAsync())
                            {
                                details.Add(new SPSellInvoiceDetails
                                {
                                    ID = Convert.ToInt32(detailsReader["ID"]),
                                    ParentID = id, // تأكيد أن ParentID صحيح
                                    ClassID = Convert.ToInt32(detailsReader["ClassID"]),
                                    UnitID = Convert.ToInt32(detailsReader["UnitID"]),
                                    Quantity = Convert.ToDecimal(detailsReader["Quantity"]),
                                    UnitPrice = Convert.ToDecimal(detailsReader["UnitPrice"]),
                                    SubDescount = Convert.ToDecimal(detailsReader["SubDescount"]),
                                    TotalAMount = Convert.ToDecimal(detailsReader["TotalAMount"])
                                });
                            }
                        }
                    }

                    if (invoice != null)
                    {
                        invoice.InvoiceDetails = details; // تعيين التفاصيل في الفاتورة
                        return Ok(invoice);
                    }
                    else
                    {
                        return NotFound($"Invoice with ID {id} not found.");
                    }
                }
                catch (SqlException sqlEx)
                {
                    return StatusCode(500, $"Database error: {sqlEx.Message}");
                }
                catch (System.Exception ex)
                {
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }
            }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllInvoicesAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                    SELECT 
                        tblSPSellInvoice.ID, 
                        PeriodNumber, 
                        SalePointID, 
                        TheNumber, 
                        TheDate, 
                        ThePay,
                        StoreAccount.AccountName AS StoreName, 
                        dbo.tblAccounts.AccountName, 
                        CustomerName, 
                        Notes,  
                        Descount, 
                        Debited, 
                        PayAmount
                    FROM tblSPSellInvoice 
                    INNER JOIN dbo.tblAccounts 
                        ON dbo.tblAccounts.ID = dbo.tblSPSellInvoice.AccountID
                    INNER JOIN dbo.tblAccounts AS StoreAccount 
                        ON StoreAccount.ID = dbo.tblSPSellInvoice.StoreID 
                    ORDER BY ID DESC";

                    var invoices = await connection.QueryAsync<HeaderInvoice>(query);

                    return Ok(invoices);
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


        [HttpGet("details/{invoiceId}")]
    public async Task<IActionResult> GetInvoiceDetailsAsync(int invoiceId)
    {
        try
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                SELECT
                    dbo.tblClasses.ClassNumber AS ClassNumber,
                    dbo.tblClasses.ClassName AS ClassName,
                    dbo.tblUnits.UnitName AS UnitName,
                    dbo.tblSPSellInvoiceDetailes.Quantity AS Quantity,
                    dbo.tblSPSellInvoiceDetailes.UnitPrice AS UnitPrice,
                    dbo.tblSPSellInvoiceDetailes.SubDescount AS SubDescount,
                    dbo.tblSPSellInvoiceDetailes.TotalAMount AS TotalAmount
                FROM dbo.tblSPSellInvoiceDetailes
                INNER JOIN dbo.tblClasses ON dbo.tblSPSellInvoiceDetailes.ClassID = dbo.tblClasses.ID
                INNER JOIN dbo.tblUnits ON dbo.tblSPSellInvoiceDetailes.UnitID = dbo.tblUnits.ID
                WHERE tblSPSellInvoiceDetailes.ParentID = @invoiceId;
            ";

                // استخدام Dapper لجلب البيانات
                var details = await connection.QueryAsync<InvoiceDetail>(query, new { invoiceId });

                return Ok(details);
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


    public class SPSellInvoice
        {
            public int ID { get; set; }
            public string? PeriodNumber { get; set; }
            public int SalePointID { get; set; }
            public string? TheNumber { get; set; }
            public DateTime TheDate { get; set; }
            public string? ThePay { get; set; }
            public int StoreID { get; set; }
            public int AccountID { get; set; }
            public string? CustomerName { get; set; }
            public string? Notes { get; set; }
            public int UserID { get; set; }
            public decimal Descount { get; set; }
            public decimal Debited { get; set; }
            public decimal PayAmount { get; set; }

            public List<SPSellInvoiceDetails> InvoiceDetails { get; set; } // علاقة مع التفاصيل
        }

        public class SPSellInvoiceDetails
        {
            public int ID { get; set; }
            public int ParentID { get; set; } // مفتاح خارجي يشير إلى الفاتورة الرئيسية
            public int ClassID { get; set; }
            public int UnitID { get; set; }
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal SubDescount { get; set; }
            public decimal TotalAMount { get; set; }
        }


        public class InvoiceDetail
        {
            public string ClassNumber { get; set; }
            public string ClassName { get; set; }
            public string UnitName { get; set; }
            public string Quantity { get; set; } // تأكد من النوع الصحيح
            public string UnitPrice { get; set; }
            public string SubDescount { get; set; }
            public string TotalAmount { get; set; }
        }

        public class HeaderInvoice
        {
            public int ID { get; set; }
            public string PeriodNumber { get; set; }
            public int SalePointID { get; set; }
            public string TheNumber { get; set; }
            public DateTime TheDate { get; set; }
            public string ThePay { get; set; }
            public string StoreName { get; set; }
            public string AccountName { get; set; }
            public string CustomerName { get; set; }
            public string Notes { get; set; }
            public decimal Descount { get; set; }
            public decimal Debited { get; set; }
            public decimal PayAmount { get; set; }
        }

    }
}
