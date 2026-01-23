using System;
using System.Linq;
using System.Web.Http;
using Demo1NET4.Data;
using Demo1NET4.DTOs;
using Demo1NET4.Models;

namespace Demo1NET4.Controllers
{
    [RoutePrefix("api/groups")]
    public class GroupsController : ApiController
    {
        private readonly AppDbContext _context;

        public GroupsController()
        {
            _context = new AppDbContext();
        }

        // GET /api/groups?type=customer
        // GET /api/groups?type=product
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAll(string type = null)
        {
            try
            {
                var query = _context.Groups.AsQueryable();

                // Filter theo type nếu có
                if (!string.IsNullOrEmpty(type))
                {
                    query = query.Where(g => g.Type == type);
                }

                var groups = query
                    .OrderBy(g => g.MaNhom)
                    .Select(g => new
                    {
                        id = g.ID,
                        code = g.MaNhom,
                        name = g.TenNhom,
                        type = g.Type
                    })
                    .ToList();

                return Ok(groups);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi GetAll: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        // POST /api/groups
        [HttpPost]
        [Route("")]
        public IHttpActionResult Create([FromBody] GroupCreateDto dto)
        {
            try
            {
                // Validate input
                if (dto == null)
                {
                    return BadRequest("Dữ liệu không hợp lệ");
                }
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return BadRequest("Tên nhóm không được để trống");
                }

                // Validate Type
                var validTypes = new[] { "customer", "product" };
                if (string.IsNullOrEmpty(dto.Type) || !validTypes.Contains(dto.Type))
                {
                    return BadRequest("Type phải là 'customer' hoặc 'product'");
                }

                // KHÔNG GÁN MaNhom - để trigger tự sinh
                var group = new Group
                {
                    TenNhom = dto.Name,
                    Type = dto.Type
                };

                _context.Groups.Add(group);
                _context.SaveChanges();

                System.Diagnostics.Debug.WriteLine($"✅ Tạo nhóm thành công: {group.MaNhom} - {dto.Name} ({dto.Type})");

                return Ok(new
                {
                    id = group.ID,
                    code = group.MaNhom,
                    name = group.TenNhom,
                    type = group.Type,
                    message = "Tạo nhóm thành công"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi tạo nhóm: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return InternalServerError(ex);
            }
        }

        // DELETE /api/groups/{id}
        [HttpDelete]
        [Route("{id}")]
        public IHttpActionResult Delete(int id)
        {
            try
            {
                var group = _context.Groups.Find(id);

                if (group == null)
                {
                    return NotFound();
                }

                // Kiểm tra xem nhóm có đang được sử dụng không
                if (group.Type == "customer")
                {
                    var hasCustomers = _context.Customers.Any(c => c.GroupID == id);
                    if (hasCustomers)
                    {
                        return BadRequest("Không thể xóa nhóm vì đang có khách hàng sử dụng");
                    }
                }
                // TODO: Kiểm tra product group nếu có bảng Products liên kết

                _context.Groups.Remove(group);
                _context.SaveChanges();

                return Ok(new { message = "Đã xóa nhóm thành công" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi xóa nhóm: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        // PUT /api/groups/{id}
        [HttpPut]
        [Route("{id}")]
        public IHttpActionResult Update(int id, [FromBody] GroupCreateDto dto)
        {
            try
            {
                // Validate input
                if (dto == null)
                {
                    return BadRequest("Dữ liệu không hợp lệ");
                }
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return BadRequest("Tên nhóm không được để trống");
                }

                // Tìm nhóm cần update
                var group = _context.Groups.Find(id);
                if (group == null)
                {
                    return NotFound();
                }

                // Chỉ cho phép cập nhật TenNhom, KHÔNG cho phép đổi Type
                group.TenNhom = dto.Name;

                _context.SaveChanges();

                System.Diagnostics.Debug.WriteLine($"✅ Cập nhật nhóm thành công: {group.MaNhom} - {dto.Name}");

                return Ok(new
                {
                    id = group.ID,
                    code = group.MaNhom,
                    name = group.TenNhom,
                    type = group.Type,
                    message = "Cập nhật nhóm thành công"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi cập nhật nhóm: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return InternalServerError(ex);
            }
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