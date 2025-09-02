using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoteFlixApi.Data;
using NoteFlixApi.DTOs;
using NoteFlixApi.Services;
using Serilog;

namespace NoteFlixApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly NoteFlixDbContext _context;
        private readonly JwtService _jwtService;
        
        public UserController(NoteFlixDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }
        
        [HttpGet("premium-status")]
        public async Task<IActionResult> GetPremiumStatus()
        {
            try
            {
                // Token'dan user ID'yi al
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader == null || !authHeader.StartsWith("Bearer "))
                {
                    Log.Warning("Premium status hatası: Geçersiz token");
                    return Unauthorized(new { error = "Geçersiz token" });
                }
                
                var token = authHeader.Substring("Bearer ".Length);
                var userId = _jwtService.GetUserIdFromToken(token);
                
                Log.Information("Premium status isteği: UserId: {UserId}", userId);
                
                // Kullanıcıyı bul
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { error = "Kullanıcı bulunamadı" });
                }
                
                var response = new PremiumStatusResponse
                {
                    IsPremium = user.IsPremium,
                    PremiumExpiresAt = null // Şimdilik süresiz premium
                };
                
                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Premium durum kontrolü sırasında bir hata oluştu" });
            }
        }
        
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                // Token'dan user ID'yi al
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader == null || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { error = "Geçersiz token" });
                }
                
                var token = authHeader.Substring("Bearer ".Length);
                var userId = _jwtService.GetUserIdFromToken(token);
                
                // Kullanıcıyı bul
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { error = "Kullanıcı bulunamadı" });
                }
                
                var userDto = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    IsPremium = user.IsPremium,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                };
                
                return Ok(userDto);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Profil bilgileri alınırken bir hata oluştu" });
            }
        }
        
        [HttpGet("debug")]
        [AllowAnonymous]
        public IActionResult GetDebugInfo()
        {
            try
            {
                Log.Information("Debug endpoint çağrıldı");
                
                var debugInfo = new
                {
                    timestamp = DateTime.UtcNow,
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                    machineName = Environment.MachineName,
                    osVersion = Environment.OSVersion.ToString(),
                    dotnetVersion = Environment.Version.ToString(),
                    workingDirectory = Environment.CurrentDirectory,
                    databaseConnection = _context.Database.CanConnect() ? "Connected" : "Not Connected",
                    headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                    queryParams = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString())
                };
                
                return Ok(debugInfo);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Debug endpoint hatası");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
}
