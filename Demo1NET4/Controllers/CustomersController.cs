using System;
using System.Linq;
using System.Web.Http;
using Demo1NET4.Data;
using Demo1NET4.Models;

namespace Demo1NET4.Controllers
{
    [RoutePrefix("api/customers")]
    public class CustomersController : ApiController
    {
        private readonly AppDbContext _context;

        public CustomersController()
        {
            _context = new AppDbContext();
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAll()
        {
            var customers = _context.Customers
                .GroupJoin(
                    _context.Groups,
                    c => c.GroupID,
                    g => g.ID,
                    (c, gList) => new { c, gList }
                )
                .SelectMany(
                    x => x.gList.DefaultIfEmpty(),
                    (x, g) => new
                    {
                        id = x.c.Id,
                        code = x.c.MaKH,
                        name = x.c.Name,
                        phone = x.c.Phone,
                        email = x.c.Email,
                        address = x.c.Address,
                        taxCode = x.c.TaxCode,
                        cccd = x.c.IdCard,
                        groupID = x.c.GroupID,
                        group = (g == null) ? null : g.MaNhom,
                        groupName = (g == null) ? null : g.TenNhom,
                        createdAt = x.c.CreatedAt
                    }
                )
                .ToList();

            return Ok(customers);
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult Create(Customer customer)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // ❌ KHÔNG được có dòng: customer.MaKH = "..."

                customer.CreatedAt = DateTime.Now;

                if (customer.GroupID == 0)
                {
                    customer.GroupID = null;
                }

                _context.Customers.Add(customer);
                _context.SaveChanges();

                return Ok(customer);
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException dbEx)
            {
                var msg = dbEx.InnerException?.InnerException?.Message ?? dbEx.Message;
                return Content(
                    System.Net.HttpStatusCode.InternalServerError,
                    new { error = msg }
                );
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPut]
        [Route("{id:int}")]
        public IHttpActionResult Update(int id, Customer customer)
        {
            var existing = _context.Customers.Find(id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.MaKH = customer.MaKH;
            existing.Name = customer.Name;
            existing.Phone = customer.Phone;
            existing.Email = customer.Email;
            existing.Address = customer.Address;
            existing.TaxCode = customer.TaxCode;
            existing.IdCard = customer.IdCard;
            existing.GroupID = customer.GroupID;

            _context.SaveChanges();
            return Ok(existing);
        }

        [HttpDelete]
        [Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            var existing = _context.Customers.Find(id);
            if (existing == null)
            {
                return NotFound();
            }

            _context.Customers.Remove(existing);
            _context.SaveChanges();

            return Ok(new { message = "Deleted successfully" });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_context != null)
                {
                    _context.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}