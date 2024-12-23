﻿using Microsoft.AspNetCore.Http;
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
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpPost]
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
                        invoiceCommand.Parameters.AddWithValue("@TheNumber", invoice.TheNumber);
                        invoiceCommand.Parameters.AddWithValue("@TheDate", invoice.TheDate);
                        invoiceCommand.Parameters.AddWithValue("@ThePay", invoice.ThePay);
                        invoiceCommand.Parameters.AddWithValue("@StoreID", invoice.StoreID);
                        invoiceCommand.Parameters.AddWithValue("@AccountID", invoice.AccountID);
                        invoiceCommand.Parameters.AddWithValue("@CustomerName", invoice.CustomerName);
                        invoiceCommand.Parameters.AddWithValue("@Notes", invoice.Notes ?? ""); // التعامل مع القيم الفارغة
                        invoiceCommand.Parameters.AddWithValue("@UserID", invoice.UserID);
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

                    return Ok(new { id = invoiceId }); // إرجاع ID الفاتورة التي تم إنشاؤها
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
        public class SPSellInvoice
        {
            public int ID { get; set; }
            public int PeriodNumber { get; set; }
            public int SalePointID { get; set; }
            public string TheNumber { get; set; }
            public DateTime TheDate { get; set; }
            public decimal ThePay { get; set; }
            public int StoreID { get; set; }
            public int AccountID { get; set; }
            public string CustomerName { get; set; }
            public string Notes { get; set; }
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
    }
}