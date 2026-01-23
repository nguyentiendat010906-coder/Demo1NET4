using System;
using System.Linq;
using System.Web.Http;
using Demo1NET4.Data;
using Demo1NET4.DTOs;
using Demo1NET4.Models;

namespace Demo1NET4.Controllers
{
    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {
        // ✅ TEST - Endpoint đơn giản nhất
        [HttpGet]
        [Route("test")]
        public IHttpActionResult Test()
        {
            return Ok(new { message = "API hoạt động!", timestamp = DateTime.Now });
        }

        // ✅ TEST - Kiểm tra kết nối database
        [HttpGet]
        [Route("test-db")]
        public IHttpActionResult TestDatabase()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    var canConnect = context.Database.Exists();

                    if (!canConnect)
                    {
                        return Ok(new
                        {
                            message = "Database KHÔNG tồn tại!",
                            connectionString = context.Database.Connection.ConnectionString
                        });
                    }

                    var userCount = context.Users.Count();
                    var allUsers = context.Users.ToList();

                    return Ok(new
                    {
                        message = "Database kết nối thành công!",
                        databaseExists = canConnect,
                        userCount = userCount,
                        users = allUsers.Select(u => new { u.Id, u.Username })
                    });
                }
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, new
                {
                    message = "Lỗi kết nối database",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    innerInnerError = ex.InnerException?.InnerException?.Message
                });
            }
        }

        // ✅ REGISTER
        [HttpPost]
        [Route("register")]
        public IHttpActionResult Register([FromBody] RegisterDTo request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Dữ liệu không hợp lệ");

                if (string.IsNullOrWhiteSpace(request.Username))
                    return BadRequest("Username không được để trống");

                if (string.IsNullOrWhiteSpace(request.Password))
                    return BadRequest("Password không được để trống");

                using (var context = new AppDbContext())
                {
                    if (context.Users.Any(u => u.Username == request.Username.Trim()))
                        return BadRequest("Tài khoản đã tồn tại");

                    var user = new User
                    {
                        Username = request.Username.Trim(),
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password.Trim())
                    };

                    context.Users.Add(user);
                    context.SaveChanges();

                    return Ok(new
                    {
                        message = "Đăng ký thành công",
                        userId = user.Id,
                        username = user.Username
                    });
                }
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, new
                {
                    message = "Lỗi đăng ký",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // ✅ LOGIN
        [HttpPost]
        [Route("login")]
        public IHttpActionResult Login([FromBody] LoginDTo request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== LOGIN REQUEST ===");
                System.Diagnostics.Debug.WriteLine($"Request is null: {request == null}");

                if (request == null)
                    return BadRequest("Dữ liệu không hợp lệ");

                System.Diagnostics.Debug.WriteLine($"Username: {request.Username}");
                System.Diagnostics.Debug.WriteLine($"Password length: {request.Password?.Length ?? 0}");

                if (string.IsNullOrWhiteSpace(request.Username))
                    return BadRequest("Username không được để trống");

                if (string.IsNullOrWhiteSpace(request.Password))
                    return BadRequest("Password không được để trống");

                using (var context = new AppDbContext())
                {
                    System.Diagnostics.Debug.WriteLine("Đang tìm user trong database...");

                    var user = context.Users
                        .FirstOrDefault(u => u.Username == request.Username.Trim());

                    if (user == null)
                    {
                        System.Diagnostics.Debug.WriteLine("User không tồn tại");
                        return Content(System.Net.HttpStatusCode.Unauthorized,
                            new { message = "Tài khoản không tồn tại" });
                    }

                    System.Diagnostics.Debug.WriteLine($"Tìm thấy user ID: {user.Id}");
                    System.Diagnostics.Debug.WriteLine("Đang verify password...");

                    bool passwordMatches =
                        BCrypt.Net.BCrypt.Verify(request.Password.Trim(), user.PasswordHash);

                    System.Diagnostics.Debug.WriteLine($"Password matches: {passwordMatches}");

                    if (!passwordMatches)
                    {
                        return Content(System.Net.HttpStatusCode.Unauthorized,
                            new { message = "Mật khẩu không đúng" });
                    }

                    System.Diagnostics.Debug.WriteLine("Login thành công!");

                    return Ok(new
                    {
                        message = "Đăng nhập thành công",
                        userId = user.Id,
                        username = user.Username
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LỖI: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"INNER: {ex.InnerException?.Message}");
                System.Diagnostics.Debug.WriteLine($"STACK: {ex.StackTrace}");

                return Content(System.Net.HttpStatusCode.InternalServerError, new
                {
                    message = "Lỗi đăng nhập",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}