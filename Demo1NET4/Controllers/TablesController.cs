using Demo1NET4.Data;
using Demo1NET4.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;

namespace Demo1NET4.Controllers
{
    [RoutePrefix("api/tables")]
    public class TablesController : ApiController
    {
        private readonly AppDbContext _context;

        public TablesController()
        {
            _context = new AppDbContext();
        }

        // ===============================
        // GET: api/tables?groupId=1
        // ===============================
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAll([FromUri] int? groupId)
        {
            var query = _context.TableGroups
                .Include(g => g.Tables)
                .AsQueryable();

            if (groupId.HasValue && groupId.Value != 0)
                query = query.Where(g => g.Id == groupId);

            var result = query.Select(g => new
            {
                id = g.Id,
                name = g.Name,
                tables = g.Tables.Select(t => new
                {
                    id = t.Id,
                    name = t.Name,
                    status = t.Status,
                    tableGroupId = t.TableGroupId
                }).ToList()
            }).ToList();

            return Ok(result);
        }

        // ===============================
        // GET: api/tables/{id}
        // ===============================
        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult GetById(int id)
        {
            var table = _context.Tables
                .Include(t => t.TableGroup)
                .FirstOrDefault(t => t.Id == id);

            if (table == null)
                return NotFound();

            return Ok(new
            {
                table.Id,
                table.Name,
                table.Status,
                table.TableGroupId,
                TableGroupName = table.TableGroup.Name
            });
        }

        // ===============================
        // POST: api/tables
        // ===============================
        [HttpPost]
        [Route("")]
        public IHttpActionResult Create([FromBody] Table table)
        {
            if (string.IsNullOrWhiteSpace(table.Name))
                return BadRequest("Name is required");

            var groupExists = _context.TableGroups.Any(g => g.Id == table.TableGroupId);
            if (!groupExists)
                return BadRequest("TableGroup not found");

            table.Status = "empty";

            _context.Tables.Add(table);
            _context.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = table.Id }, table);
        }

        // ===============================
        // PUT: api/tables/{id}
        // ===============================
        [HttpPut]
        [Route("{id:int}")]
        public IHttpActionResult Update(int id, [FromBody] Table updated)
        {
            var table = _context.Tables.Find(id);
            if (table == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(updated.Name))
                table.Name = updated.Name;

            _context.SaveChanges();
            return Ok(table);
        }

        // ===============================
        // PUT: api/tables/{id}/status
        // ===============================
        [HttpPut]
        [Route("{id:int}/status")]
        public IHttpActionResult UpdateStatus(int id, [FromBody] string status)
        {
            var table = _context.Tables.Find(id);
            if (table == null)
                return NotFound();

            status = status.ToLower().Trim();
            var allowed = new[] { "empty", "serving", "reserved" };

            if (!allowed.Contains(status))
                return BadRequest("Status must be: empty | serving | reserved");

            table.Status = status;
            _context.SaveChanges();

            return Ok();
        }

        // ===============================
        // DELETE: api/tables/{id}
        // ===============================
        [HttpDelete]
        [Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            var table = _context.Tables.Find(id);
            if (table == null)
                return NotFound();

            if (table.Status == "serving")
                return BadRequest("Cannot delete table that is currently serving");

            _context.Tables.Remove(table);
            _context.SaveChanges();

            return StatusCode(System.Net.HttpStatusCode.NoContent);
        }

        // ===============================
        // POST: api/tables/{id}/open
        // ===============================
        [HttpPost]
        [Route("{id:int}/open")]
        public IHttpActionResult OpenTable(int id)
        {
            var table = _context.Tables.FirstOrDefault(t => t.Id == id);
            if (table == null)
                return NotFound();

            if (table.Status != "empty")
                return BadRequest("Table is not empty");

            // 1️⃣ Tạo invoice mới
            var invoice = new Invoice
            {
                TableId = table.Id,
                InvoiceDate = DateTime.Now,
                Status = "Open",
                TotalAmount = 0
            };

            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            // 2️⃣ Update bàn
            table.Status = "serving";
            table.CurrentInvoiceId = invoice.Id;

            _context.SaveChanges();

            return Ok(new
            {
                invoiceId = invoice.Id,
                tableId = table.Id
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}