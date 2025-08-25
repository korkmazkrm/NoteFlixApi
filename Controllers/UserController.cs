using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoteFlixApi.Data;
using NoteFlixApi.DTOs;
using NoteFlixApi.Services;

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
                
                var response = new PremiumStatusResponse
                {
                    IsPremium = user.IsPremium,
                    PremiumExpiresAt = null // Şimdilik süresiz premium
                };
                
                return Ok(response);
            }
            catch (Exception ex)
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
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Profil bilgileri alınırken bir hata oluştu" });
            }
        }
    }
}
