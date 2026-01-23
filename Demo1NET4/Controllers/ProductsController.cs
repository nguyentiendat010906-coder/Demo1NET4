using Demo1NET4.Data;
using Demo1NET4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

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

        // GET /api/products
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAll()
        {
            var products = _context.Products.ToList();
            return Ok(products);
        }

        // POST /api/products
        [HttpPost]
        [Route("")]
        public IHttpActionResult Create(Product product)
        {
            // Validate Category
            var validCategories = new[] { "Bar", "Bếp", "Tính thời gian", "Khác" };
            if (!validCategories.Contains(product.Category))
            {
                return BadRequest("Category không hợp lệ");
            }

            // Validate Unit Type
            if (product.UnitType == "Thời gian")
            {
                product.Stock = null;
            }
            else if (product.UnitType == "Số lượng")
            {
                if (product.Stock == null)
                {
                    return BadRequest("Sản phẩm theo số lượng phải có tồn kho");
                }
            }
            else
            {
                return BadRequest("UnitType không hợp lệ");
            }

            _context.Products.Add(product);
            _context.SaveChanges();
            return Ok(product);
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

