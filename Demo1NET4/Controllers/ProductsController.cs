using Demo1NET4.Data;
using Demo1NET4.Models;
using System;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Web;

namespace Demo1NET4.Controllers
{
    [RoutePrefix("api/products")]
    public class ProductsController : ApiController
    {
        private readonly AppDbContext _context;

        public ProductsController()
        {
            _context = new AppDbContext();
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAll()
        {
            try
            {
                var products = _context.Products
                    .GroupJoin(
                        _context.Groups,
                        p => p.GroupID,
                        g => g.ID,
                        (p, gList) => new { p, gList }
                    )
                    .SelectMany(
                        x => x.gList.DefaultIfEmpty(),
                        (x, g) => new
                        {
                            id = x.p.Id,
                            code = x.p.Code,
                            name = x.p.Name,
                            price = x.p.Price,
                            stock = x.p.Stock,
                            category = x.p.Category,
                            unitType = x.p.UnitType,
                            unitTypeId = x.p.UnitTypeId,
                            groupID = x.p.GroupID,
                            group = (g == null) ? null : g.MaNhom,
                            groupName = (g == null) ? null : g.TenNhom,
                            imageUrl = x.p.ImageUrl
                        }
                    )
                    .ToList();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Lỗi: " + ex.Message + " | Inner: " + ex.InnerException?.Message));
            }
        }

        [HttpPost]
        [Route("upload")]
        public async Task<IHttpActionResult> CreateWithFile()
        {
            try
            {
                if (!Request.Content.IsMimeMultipartContent())
                {
                    return BadRequest("Unsupported media type");
                }

                var uploadFolder = HttpContext.Current.Server.MapPath("~/Images/Products");

                System.Diagnostics.Debug.WriteLine($"=== FILE UPLOAD DEBUG ===");
                System.Diagnostics.Debug.WriteLine($"Upload folder: {uploadFolder}");

                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var provider = new MultipartFormDataStreamProvider(uploadFolder);
                await Request.Content.ReadAsMultipartAsync(provider);

                var product = new Product();
                // ✅ ĐẶT BIẾN NÀY Ở ĐÂY, NGOÀI PHẠM VI IF
                string imageUrl = null;

                // Đọc form data
                foreach (var key in provider.FormData.AllKeys)
                {
                    var value = provider.FormData[key];
                    switch (key.ToLower())
                    {
                        case "name":
                            product.Name = value;
                            break;
                        case "price":
                            product.Price = string.IsNullOrEmpty(value) ? 0 : decimal.Parse(value);
                            break;
                        case "stock":
                            product.Stock = string.IsNullOrEmpty(value) ? 0 : int.Parse(value);
                            break;
                        case "category":
                            product.Category = value;
                            break;
                        case "unittype":
                            product.UnitType = value;
                            break;
                        case "unittypeid":
                            product.UnitTypeId = string.IsNullOrEmpty(value) ? 1 : int.Parse(value);
                            break;
                        case "groupid":
                            product.GroupID = (string.IsNullOrEmpty(value) || value == "0") ? (int?)null : int.Parse(value);
                            break;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Files received: {provider.FileData.Count}");

                // Xử lý file upload
                if (provider.FileData.Count > 0)
                {
                    var file = provider.FileData[0];
                    System.Diagnostics.Debug.WriteLine($"Temp file: {file.LocalFileName}");
                    System.Diagnostics.Debug.WriteLine($"Temp file exists: {File.Exists(file.LocalFileName)}");

                    var originalFileName = file.Headers.ContentDisposition.FileName.Trim('"');
                    var extension = Path.GetExtension(originalFileName);
                    var newFileName = $"{Guid.NewGuid()}{extension}";
                    var newFilePath = Path.Combine(uploadFolder, newFileName);

                    System.Diagnostics.Debug.WriteLine($"Moving to: {newFilePath}");

                    File.Move(file.LocalFileName, newFilePath);

                    if (File.Exists(newFilePath))
                    {
                        var fileInfo = new FileInfo(newFilePath);
                        System.Diagnostics.Debug.WriteLine($"✅ SUCCESS - File size: {fileInfo.Length} bytes");
                    }

                    // ✅ GÁN ĐÚNG CHO BIẾN imageUrl
                    imageUrl = $"/Images/Products/{newFileName}";
                    System.Diagnostics.Debug.WriteLine($"ImageUrl set to: {imageUrl}");
                }

                // ✅ GÁN CHO PRODUCT.IMAGEURL
                product.ImageUrl = imageUrl;
                System.Diagnostics.Debug.WriteLine($"Product.ImageUrl: {product.ImageUrl}");

                // Tự động sinh mã
                if (string.IsNullOrEmpty(product.Code))
                {
                    var maxCode = _context.Products
                        .Where(p => !string.IsNullOrEmpty(p.Code))
                        .OrderByDescending(p => p.Code)
                        .Select(p => p.Code)
                        .FirstOrDefault();

                    if (!string.IsNullOrEmpty(maxCode) && int.TryParse(maxCode, out int maxNumber))
                    {
                        product.Code = (maxNumber + 1).ToString("D5");
                    }
                    else
                    {
                        product.Code = "00001";
                    }
                }

                _context.Products.Add(product);
                _context.SaveChanges();

                System.Diagnostics.Debug.WriteLine($"=== PRODUCT SAVED - ID: {product.Id} ===");

                return Ok(product);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                return InternalServerError(ex);
            }
        }

        // ✅ GIỮ NGUYÊN API CŨ - NHẬN JSON (BASE64)
        [HttpPost]
        [Route("")]
        public IHttpActionResult Create(Product product)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== CREATE PRODUCT REQUEST ===");
                System.Diagnostics.Debug.WriteLine($"Name: {product.Name}");
                System.Diagnostics.Debug.WriteLine($"ImageUrl Length: {product.ImageUrl?.Length ?? 0}");

                // Xử lý ảnh base64
                if (!string.IsNullOrEmpty(product.ImageUrl) && product.ImageUrl.StartsWith("data:image"))
                {
                    try
                    {
                        var base64Data = product.ImageUrl.Split(',')[1];
                        var imageBytes = Convert.FromBase64String(base64Data);
                        var fileName = $"{Guid.NewGuid()}.jpg";
                        var uploadFolder = HttpContext.Current.Server.MapPath("~/Images/Products");

                        if (!Directory.Exists(uploadFolder))
                        {
                            Directory.CreateDirectory(uploadFolder);
                        }

                        var filePath = Path.Combine(uploadFolder, fileName);
                        File.WriteAllBytes(filePath, imageBytes);

                        product.ImageUrl = $"/Images/Products/{fileName}";
                    }
                    catch (Exception imgEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error saving image: {imgEx.Message}");
                        product.ImageUrl = null;
                    }
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors);
                    foreach (var error in errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Validation Error: {error.ErrorMessage}");
                    }
                    return BadRequest(ModelState);
                }

                // Tự động sinh mã
                if (string.IsNullOrEmpty(product.Code))
                {
                    var maxCode = _context.Products
                        .Where(p => !string.IsNullOrEmpty(p.Code))
                        .OrderByDescending(p => p.Code)
                        .Select(p => p.Code)
                        .FirstOrDefault();

                    if (!string.IsNullOrEmpty(maxCode) && int.TryParse(maxCode, out int maxNumber))
                    {
                        product.Code = (maxNumber + 1).ToString("D5");
                    }
                    else
                    {
                        product.Code = "00001";
                    }
                }

                if (product.GroupID == 0)
                {
                    product.GroupID = null;
                }

                _context.Products.Add(product);
                _context.SaveChanges();

                return Ok(product);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                return InternalServerError(new Exception("Lỗi: " + ex.Message + " | Inner: " + ex.InnerException?.Message));
            }
        }

        [HttpPut]
        [Route("{id:int}")]
        public IHttpActionResult Update(int id, Product product)
        {
            try
            {
                var existing = _context.Products.Find(id);
                if (existing == null)
                {
                    return NotFound();
                }

                existing.Name = product.Name;
                existing.Price = product.Price;
                existing.Stock = product.Stock;

                if (product.Category != null)
                    existing.Category = product.Category;

                if (product.UnitType != null)
                    existing.UnitType = product.UnitType;

                existing.UnitTypeId = product.UnitTypeId;
                existing.GroupID = product.GroupID == 0 ? null : product.GroupID;

                // Xử lý ảnh khi update
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    if (product.ImageUrl.StartsWith("data:image"))
                    {
                        try
                        {
                            var base64Data = product.ImageUrl.Split(',')[1];
                            var imageBytes = Convert.FromBase64String(base64Data);
                            var fileName = $"{Guid.NewGuid()}.jpg";
                            var uploadFolder = HttpContext.Current.Server.MapPath("~/Images/Products");

                            if (!Directory.Exists(uploadFolder))
                            {
                                Directory.CreateDirectory(uploadFolder);
                            }

                            var filePath = Path.Combine(uploadFolder, fileName);
                            File.WriteAllBytes(filePath, imageBytes);

                            // Xóa ảnh cũ
                            if (!string.IsNullOrEmpty(existing.ImageUrl))
                            {
                                var oldFilePath = HttpContext.Current.Server.MapPath("~" + existing.ImageUrl);
                                if (File.Exists(oldFilePath))
                                {
                                    File.Delete(oldFilePath);
                                }
                            }

                            existing.ImageUrl = $"/Images/Products/{fileName}";
                        }
                        catch { }
                    }
                    else
                    {
                        existing.ImageUrl = product.ImageUrl;
                    }
                }

                _context.SaveChanges();
                return Ok(existing);
            }
            catch (DbEntityValidationException ex)
            {
                var errors = ex.EntityValidationErrors
                    .SelectMany(e => e.ValidationErrors)
                    .Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage });

                return Content(HttpStatusCode.BadRequest, new { message = "Validation error", errors });
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Lỗi: " + ex.Message));
            }
        }

        // ✅ THÊM METHOD NÀY - UPDATE VỚI FILE
        [HttpPut]
        [Route("upload/{id:int}")]
        public async Task<IHttpActionResult> UpdateWithFile(int id)
        {
            try
            {
                var existing = _context.Products.Find(id);
                if (existing == null)
                {
                    return NotFound();
                }

                if (!Request.Content.IsMimeMultipartContent())
                {
                    return BadRequest("Unsupported media type");
                }

                var uploadFolder = HttpContext.Current.Server.MapPath("~/Images/Products");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var provider = new MultipartFormDataStreamProvider(uploadFolder);
                await Request.Content.ReadAsMultipartAsync(provider);

                string newImageUrl = null;
                bool shouldRemoveImage = false;

                // Đọc form data
                foreach (var key in provider.FormData.AllKeys)
                {
                    var value = provider.FormData[key];
                    System.Diagnostics.Debug.WriteLine($"Form key: {key}, value: {value}");

                    switch (key.ToLower())
                    {
                        case "name":
                            existing.Name = value;
                            break;
                        case "price":
                            existing.Price = string.IsNullOrEmpty(value) ? 0 : decimal.Parse(value);
                            break;
                        case "stock":
                            existing.Stock = string.IsNullOrEmpty(value) ? 0 : int.Parse(value);
                            break;
                        case "category":
                            existing.Category = value;
                            break;
                        case "unittype":
                            existing.UnitType = value;
                            break;
                        case "unittypeid":
                            existing.UnitTypeId = string.IsNullOrEmpty(value) ? 1 : int.Parse(value);
                            break;
                        case "groupid":
                            existing.GroupID = (string.IsNullOrEmpty(value) || value == "0") ? (int?)null : int.Parse(value);
                            break;
                        case "removeimage":
                            shouldRemoveImage = value.ToLower() == "true";
                            break;
                        case "imageurl":
                            // Giữ imageUrl cũ nếu không có file mới
                            newImageUrl = value;
                            break;
                    }
                }

                // Xử lý file upload (nếu có file mới)
                if (provider.FileData.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ New file uploaded, count: {provider.FileData.Count}");

                    // Xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(existing.ImageUrl))
                    {
                        var oldFilePath = HttpContext.Current.Server.MapPath("~" + existing.ImageUrl);
                        if (File.Exists(oldFilePath))
                        {
                            File.Delete(oldFilePath);
                            System.Diagnostics.Debug.WriteLine($"Deleted old image: {oldFilePath}");
                        }
                    }

                    var file = provider.FileData[0];
                    var originalFileName = file.Headers.ContentDisposition.FileName.Trim('"');
                    var extension = Path.GetExtension(originalFileName);
                    var newFileName = $"{Guid.NewGuid()}{extension}";
                    var newFilePath = Path.Combine(uploadFolder, newFileName);

                    File.Move(file.LocalFileName, newFilePath);
                    newImageUrl = $"/Images/Products/{newFileName}";
                    System.Diagnostics.Debug.WriteLine($"✅ New image saved: {newImageUrl}");
                }
                else if (shouldRemoveImage)
                {
                    // User muốn xóa ảnh
                    System.Diagnostics.Debug.WriteLine("🗑️ Removing image");
                    if (!string.IsNullOrEmpty(existing.ImageUrl))
                    {
                        var oldFilePath = HttpContext.Current.Server.MapPath("~" + existing.ImageUrl);
                        if (File.Exists(oldFilePath))
                        {
                            File.Delete(oldFilePath);
                        }
                    }
                    newImageUrl = null;
                }
                else
                {
                    // Giữ nguyên ảnh cũ
                    System.Diagnostics.Debug.WriteLine("📷 Keeping existing image");
                    newImageUrl = existing.ImageUrl;
                }

                existing.ImageUrl = newImageUrl;
                _context.SaveChanges();

                System.Diagnostics.Debug.WriteLine($"=== PRODUCT UPDATED - ID: {existing.Id} ===");

                return Ok(existing);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                return InternalServerError(ex);
            }
        }

        [HttpDelete]
        [Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            try
            {
                var existing = _context.Products.Find(id);
                if (existing == null)
                {
                    return NotFound();
                }

                // Xóa file ảnh nếu có
                if (!string.IsNullOrEmpty(existing.ImageUrl))
                {
                    var filePath = HttpContext.Current.Server.MapPath("~" + existing.ImageUrl);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }

                _context.Products.Remove(existing);
                _context.SaveChanges();

                return Ok(new { message = "Deleted successfully" });
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Lỗi: " + ex.Message));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}