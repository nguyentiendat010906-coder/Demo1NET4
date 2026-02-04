using Demo1NET4.Data;
using Demo1NET4.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.UI.WebControls;

namespace Demo1NET4.Controllers
{
    [RoutePrefix("api/Invoices")]
    public class InvoicesController : ApiController
    {
        private readonly AppDbContext _context;
        private const decimal VAT_RATE = 0.10m;

        public InvoicesController()
        {
            _context = new AppDbContext();
        }

        #region Invoice CRUD

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAll()
        {
            var invoices = _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Table)
                .OrderByDescending(i => i.InvoiceDate)
                .Select(i => new
                {
                    id = i.Id,
                    invoiceDate = i.InvoiceDate,
                    status = i.Status,
                    subtotal = i.Subtotal,
                    vatAmount = i.VatAmount,
                    totalAmount = i.TotalAmount,
                    tableId = i.TableId,
                    tableName = i.Table.Name,
                    customerName = i.CustomerName ?? (i.Customer != null ? i.Customer.Name : null) ?? "Khách lẻ"
                })
                .ToList();

            return Ok(invoices);
        }

        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult GetDetail(int id)
        {
            var invoice = _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Table.TableGroup)
                .Include(i => i.InvoiceDetails)
                .Where(i => i.Id == id)
                .Select(i => new
                {
                    i.Id,
                    i.InvoiceDate,
                    i.EndTime,
                    i.Status,
                    i.Subtotal,
                    i.VatAmount,
                    i.TotalAmount,
                    TableName = i.Table.Name,
                    GroupName = i.Table.TableGroup.Name,
                    i.CashierName,
                    i.CustomerName,
                    i.CustomerPhone,
                    i.CustomerTaxCode,
                    i.CustomerIdCard,
                    i.CustomerEmail,
                    i.CustomerAddress,
                    InvoiceDetails = i.InvoiceDetails.Select(d => new
                    {
                        d.Id,
                        d.ProductName,
                        d.Quantity,
                        d.UnitPrice
                    }).ToList()
                })
                .FirstOrDefault();

            if (invoice == null)
                return NotFound();

            return Ok(invoice);
        }

        [HttpGet]
        [Route("by-table/{tableId:int}")]
        public IHttpActionResult GetByTable(int tableId)
        {
            var invoice = _context.Invoices
                .FirstOrDefault(i => i.TableId == tableId && i.Status == "open");

            if (invoice == null)
                return Content(System.Net.HttpStatusCode.NotFound, "No open invoice found for this table");

            return Ok(invoice);
        }

        [HttpPost]
        [Route("create-for-table/{tableId:int}")]
        public IHttpActionResult CreateForTable(int tableId)
        {
            var table = _context.Tables.Find(tableId);
            if (table == null)
                return Content(System.Net.HttpStatusCode.NotFound, "Table not found");

            var existingInvoice = _context.Invoices
                .FirstOrDefault(i => i.TableId == tableId && i.Status == "open");

            if (existingInvoice != null)
                return Ok(existingInvoice);

            var invoice = new Invoice
            {
                InvoiceDate = DateTime.Now,
                Status = "open",
                TableId = tableId
            };

            table.Status = "serving";
            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            return Ok(invoice);
        }

        [HttpPut]
        [Route("{id:int}/customer")]
        public IHttpActionResult UpdateCustomerInfo(int id, [FromBody] UpdateCustomerDto dto)
        {
            var invoice = _context.Invoices.Find(id);
            if (invoice == null)
                return Content(System.Net.HttpStatusCode.NotFound, "Invoice not found");

            invoice.CustomerName = dto.CustomerName;
            invoice.CustomerPhone = dto.CustomerPhone;
            invoice.CustomerTaxCode = dto.CustomerTaxCode;
            invoice.CustomerIdCard = dto.CustomerIdCard;
            invoice.CustomerEmail = dto.CustomerEmail;
            invoice.CustomerAddress = dto.CustomerAddress;

            _context.SaveChanges();
            return Ok(invoice);
        }

        [HttpPut]
        [Route("{id:int}/checkout")]
        public IHttpActionResult Checkout(int id)
        {
            try
            {
                var invoice = _context.Invoices
                    .Include(i => i.Table)
                    .Include(i => i.InvoiceDetails)
                    .FirstOrDefault(i => i.Id == id);

                if (invoice == null)
                    return NotFound();

                if (invoice.InvoiceDetails == null || !invoice.InvoiceDetails.Any())
                {
                    return BadRequest("Cannot checkout an invoice with no items");
                }

                var subtotal = invoice.InvoiceDetails.Sum(d => d.Quantity * d.UnitPrice);
                invoice.Subtotal = subtotal;
                invoice.VatAmount = subtotal * VAT_RATE;
                invoice.TotalAmount = subtotal + invoice.VatAmount;

                if (!string.IsNullOrWhiteSpace(invoice.CustomerPhone))
                {
                    var customer = _context.Customers
                        .FirstOrDefault(c => c.Phone == invoice.CustomerPhone);

                    if (customer == null)
                    {
                        customer = new Customer
                        {
                            Name = invoice.CustomerName ?? "Khách hàng",
                            Phone = invoice.CustomerPhone,
                            TaxCode = invoice.CustomerTaxCode,
                            IdCard = invoice.CustomerIdCard,
                            Email = invoice.CustomerEmail,
                            Address = invoice.CustomerAddress ?? "",
                            CreatedAt = DateTime.Now
                        };
                        _context.Customers.Add(customer);
                        _context.SaveChanges();
                    }

                    invoice.CustomerId = customer.Id;
                }

                invoice.Status = "paid";
                invoice.EndTime = DateTime.Now;

                if (invoice.Table != null)
                {
                    invoice.Table.Status = "empty";
                    invoice.Table.CurrentInvoiceId = null;
                }

                _context.SaveChanges();

                return Ok(new
                {
                    success = true,
                    message = "Checkout successful",
                    invoice = new
                    {
                        invoice.Id,
                        invoice.Status,
                        invoice.TotalAmount,
                        invoice.EndTime
                    }
                });
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                // ⭐ LOG CHI TIẾT LỖI VALIDATION
                System.Diagnostics.Debug.WriteLine("❌ VALIDATION ERRORS:");

                var errorMessages = new System.Text.StringBuilder();

                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        var message = $"Property: {validationError.PropertyName}, Error: {validationError.ErrorMessage}";
                        System.Diagnostics.Debug.WriteLine($"   - {message}");
                        errorMessages.AppendLine(message);
                    }
                }

                // Trả về Content thay vì BadRequest
                return Content(System.Net.HttpStatusCode.BadRequest, new
                {
                    error = "Validation failed",
                    details = errorMessages.ToString()
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Checkout Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack Trace: {ex.StackTrace}");
                return InternalServerError(ex);
            }
        }

        #endregion

        #region Invoice Details Management

        [HttpGet]
        [Route("{invoiceId:int}/items")]
        public IHttpActionResult GetInvoiceItems(int invoiceId)
        {
            var items = _context.InvoiceDetails
                .Where(d => d.InvoiceId == invoiceId)
                .Select(d => new
                {
                    id = d.Id,
                    invoiceId = d.InvoiceId,
                    productId = d.ProductId,
                    productName = d.ProductName,
                    quantity = d.Quantity,
                    unitPrice = d.UnitPrice,
                    total = d.Quantity * d.UnitPrice
                })
                .ToList();

            return Ok(items);
        }

        [HttpPost]
        [Route("{invoiceId:int}/items")]
        public IHttpActionResult AddInvoiceItem(int invoiceId, [FromBody] InvoiceItemDto dto)
        {
            var invoice = _context.Invoices.Find(invoiceId);
            if (invoice == null)
                return Content(System.Net.HttpStatusCode.NotFound, "Invoice not found");

            var product = _context.Products.Find(dto.ProductId);
            if (product == null)
                return Content(System.Net.HttpStatusCode.NotFound, "Product not found");

            var existingItem = _context.InvoiceDetails
                .FirstOrDefault(d => d.InvoiceId == invoiceId && d.ProductId == dto.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += dto.Quantity;
                existingItem.UnitPrice = dto.UnitPrice;
            }
            else
            {
                _context.InvoiceDetails.Add(new InvoiceDetail
                {
                    InvoiceId = invoiceId,
                    ProductId = dto.ProductId,
                    ProductName = product.Name,
                    Quantity = dto.Quantity,
                    UnitPrice = dto.UnitPrice
                });
            }

            _context.SaveChanges();
            UpdateInvoiceTotal(invoiceId);

            return Ok();
        }

        // POST: api/invoices/items/add - Alternative endpoint for adding items
        [HttpPost]
        [Route("items/add")]
        public IHttpActionResult AddDetail([FromBody] AddDetailDto dto)
        {
            var invoice = _context.Invoices
                .Include(i => i.InvoiceDetails)
                .FirstOrDefault(i => i.Id == dto.InvoiceId);

            if (invoice == null)
                return Content(System.Net.HttpStatusCode.NotFound, "Invoice not found");

            var product = _context.Products.Find(dto.ProductId);
            if (product == null)
                return Content(System.Net.HttpStatusCode.NotFound, "Product not found");

            var detail = invoice.InvoiceDetails
                .FirstOrDefault(d => d.ProductId == dto.ProductId);

            if (detail == null)
            {
                detail = new InvoiceDetail
                {
                    InvoiceId = dto.InvoiceId,
                    ProductId = dto.ProductId,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = dto.Quantity
                };
                _context.InvoiceDetails.Add(detail);
            }
            else
            {
                detail.Quantity += dto.Quantity;
            }

            _context.SaveChanges();
            UpdateInvoiceTotal(dto.InvoiceId);

            return Ok(detail);
        }

        [HttpPut]
        [Route("{invoiceId:int}/items/{itemId:int}")]
        public IHttpActionResult UpdateInvoiceItem(int invoiceId, int itemId, [FromBody] InvoiceItemDto dto)
        {
            var item = _context.InvoiceDetails
                .FirstOrDefault(d => d.Id == itemId && d.InvoiceId == invoiceId);

            if (item == null)
                return Content(System.Net.HttpStatusCode.NotFound, "Item not found");

            item.Quantity = dto.Quantity;
            item.UnitPrice = dto.UnitPrice;

            _context.SaveChanges();
            UpdateInvoiceTotal(invoiceId);

            return Ok();
        }

        // PUT: api/invoices/items/update - Alternative endpoint for updating quantity
        [HttpPut]
        [Route("items/update")]
        public IHttpActionResult UpdateDetailQuantity([FromBody] UpdateDetailDto dto)
        {
            var detail = _context.InvoiceDetails
                .Include(d => d.Invoice.InvoiceDetails)
                .FirstOrDefault(d => d.Id == dto.DetailId);

            if (detail == null)
                return NotFound();

            detail.Quantity = dto.Quantity;

            _context.SaveChanges();
            UpdateInvoiceTotal(detail.InvoiceId);

            return Ok(detail);
        }

        [HttpDelete]
        [Route("{invoiceId:int}/items/{itemId:int}")]
        public IHttpActionResult DeleteInvoiceItem(int invoiceId, int itemId)
        {
            var item = _context.InvoiceDetails
                .FirstOrDefault(d => d.Id == itemId && d.InvoiceId == invoiceId);

            if (item == null)
                return Content(System.Net.HttpStatusCode.NotFound, "Item not found");

            _context.InvoiceDetails.Remove(item);
            _context.SaveChanges();
            UpdateInvoiceTotal(invoiceId);

            return StatusCode(System.Net.HttpStatusCode.NoContent);
        }

        // DELETE: api/invoices/items/{id} - Alternative endpoint for deleting items
        [HttpDelete]
        [Route("items/{id:int}")]
        public IHttpActionResult DeleteDetail(int id)
        {
            var detail = _context.InvoiceDetails
                .Include(d => d.Invoice)
                .FirstOrDefault(d => d.Id == id);

            if (detail == null)
                return NotFound();

            var invoiceId = detail.InvoiceId;
            _context.InvoiceDetails.Remove(detail);
            _context.SaveChanges();
            UpdateInvoiceTotal(invoiceId);

            return Ok();
        }


        [HttpDelete]
        [Route("{id:int}")]
        public IHttpActionResult DeleteInvoice(int id)
        {
            try
            {
                var invoice = _context.Invoices
                    .Include(i => i.InvoiceDetails)
                    .Include(i => i.Table)
                    .FirstOrDefault(i => i.Id == id);

                if (invoice == null)
                    return NotFound();

                // Xóa tất cả invoice details trước
                if (invoice.InvoiceDetails != null && invoice.InvoiceDetails.Any())
                {
                    _context.InvoiceDetails.RemoveRange(invoice.InvoiceDetails);
                }

                // Nếu có bàn, cập nhật trạng thái bàn về empty
                if (invoice.Table != null)
                {
                    invoice.Table.Status = "empty";
                    invoice.Table.CurrentInvoiceId = null;
                }

                // Xóa invoice
                _context.Invoices.Remove(invoice);
                _context.SaveChanges();

                return Ok(new
                {
                    success = true,
                    message = "Invoice deleted successfully"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Delete Invoice Error: {ex.Message}");
                return InternalServerError(ex);
            }
        }


        #endregion

        #region Helper Methods

        private void UpdateInvoiceTotal(int invoiceId)
        {
            var invoice = _context.Invoices.Find(invoiceId);
            if (invoice == null) return;

            var subtotal = _context.InvoiceDetails
                .Where(d => d.InvoiceId == invoiceId)
                .Sum(d => (decimal?)d.Quantity * d.UnitPrice) ?? 0;

            invoice.Subtotal = subtotal;
            invoice.VatAmount = subtotal * VAT_RATE;
            invoice.TotalAmount = subtotal + invoice.VatAmount;

            _context.SaveChanges();
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    #region DTO Classes

    public class InvoiceItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class UpdateCustomerDto
    {
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerTaxCode { get; set; }
        public string CustomerIdCard { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerAddress { get; set; }
    }

    public class AddDetailDto
    {
        public int InvoiceId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateDetailDto
    {
        public int DetailId { get; set; }
        public int Quantity { get; set; }
    }

    #endregion
}