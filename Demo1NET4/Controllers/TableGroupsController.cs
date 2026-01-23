using Demo1NET4.Data;
using Demo1NET4.Models;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace Demo1NET4.Controllers
{
    [RoutePrefix("api/TableGroups")]
    public class TableGroupsController : ApiController
    {
        private readonly AppDbContext _context;

        public TableGroupsController()
        {
            _context = new AppDbContext();
        }

        // GET: api/TableGroups
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetGroups()
        {
            var groups = await _context.TableGroups
                .Include(g => g.Tables)
                .ToListAsync();

            // Map sang format đúng cho Angular
            var result = groups.Select(g => new
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
            });

            return Ok(result);
        }

        // GET: api/TableGroups/{id}
        [HttpGet]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> GetGroupById(int id)
        {
            var group = await _context.TableGroups
                .Include(g => g.Tables)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound();

            // Map sang format đúng
            var result = new
            {
                id = group.Id,
                name = group.Name,
                tables = group.Tables.Select(t => new
                {
                    id = t.Id,
                    name = t.Name,
                    status = t.Status,
                    tableGroupId = t.TableGroupId
                }).ToList()
            };

            return Ok(result);
        }

        // POST: api/TableGroups
        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> CreateGroup([FromBody] TableGroup group)
        {
            if (string.IsNullOrWhiteSpace(group.Name))
                return BadRequest("Name is required");

            _context.TableGroups.Add(group);
            await _context.SaveChangesAsync();

            return CreatedAtRoute("DefaultApi", new { id = group.Id }, group);
        }

        // DELETE: api/TableGroups/{id}
        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> DeleteGroup(int id)
        {
            var group = await _context.TableGroups.FindAsync(id);
            if (group == null) return NotFound();

            _context.TableGroups.Remove(group);
            await _context.SaveChangesAsync();

            return StatusCode(System.Net.HttpStatusCode.NoContent);
        }

        // POST: api/TableGroups/{id}/tables
        [HttpPost]
        [Route("{id:int}/tables")]
        public async Task<IHttpActionResult> AddTable(int id, [FromBody] Table table)
        {
            var group = await _context.TableGroups.FindAsync(id);
            if (group == null) return NotFound();

            table.TableGroupId = id;
            table.Status = "empty";

            _context.Tables.Add(table);
            await _context.SaveChangesAsync();

            return CreatedAtRoute("DefaultApi", new { id = id }, table);
        }

        // DELETE: api/TableGroups/{groupId}/tables/{tableId}
        [HttpDelete]
        [Route("{groupId:int}/tables/{tableId:int}")]
        public async Task<IHttpActionResult> DeleteTable(int groupId, int tableId)
        {
            var table = await _context.Tables
                .FirstOrDefaultAsync(t => t.Id == tableId && t.TableGroupId == groupId);

            if (table == null) return NotFound();

            _context.Tables.Remove(table);
            await _context.SaveChangesAsync();

            return StatusCode(System.Net.HttpStatusCode.NoContent);
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