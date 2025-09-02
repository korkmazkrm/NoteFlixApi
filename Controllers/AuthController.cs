using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoteFlixApi.Data;
using NoteFlixApi.DTOs;
using NoteFlixApi.Models;
using NoteFlixApi.Services;
using System.Security.Cryptography;
using System.Text;
using Serilog;

namespace NoteFlixApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly NoteFlixDbContext _context;
        private readonly JwtService _jwtService;
        
        public AuthController(NoteFlixDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }
        
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            Log.Information("Register isteği alındı: {Email}", request.Email);
            
            try
            {
                // Email kontrolü
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    Log.Warning("Register hatası: Email zaten kullanılıyor - {Email}", request.Email);
                    return BadRequest(new { error = "Bu email adresi zaten kullanılıyor" });
                }
                
                // Şifre hash'leme
                var passwordHash = HashPassword(request.Password);
                
                // Kullanıcı oluşturma
                var user = new User
                {
                    Name = request.Name,
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    IsPremium = true,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                Log.Information("Kullanıcı başarıyla kaydedildi: {Email}, UserId: {UserId}", request.Email, user.Id);
                return Ok(new { message = "Kayıt başarılı! Şimdi giriş yapabilirsiniz" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Register işlemi sırasında hata: {Email}", request.Email);
                return StatusCode(500, new { error = "Kayıt işlemi sırasında bir hata oluştu" });
            }
        }
        
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            Log.Information("Login isteği alındı: {Email}", request.Email);
            
            try
            {
                // Kullanıcıyı bul
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                {
                    Log.Warning("Login hatası: Kullanıcı bulunamadı - {Email}", request.Email);
                    return BadRequest(new { error = "Geçersiz email veya şifre" });
                }
                
                // Şifre kontrolü
                if (!VerifyPassword(request.Password, user.PasswordHash))
                {
                    Log.Warning("Login hatası: Yanlış şifre - {Email}", request.Email);
                    return BadRequest(new { error = "Geçersiz email veya şifre" });
                }
                
                // Son giriş zamanını güncelle
                user.LastLoginAt = DateTime.UtcNow;
                
                // Kullanıcının eski aktif refresh token'larını sil
                var oldRefreshTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync();
                
                Console.WriteLine($"Login: Eski refresh token sayısı: {oldRefreshTokens.Count}");
                
                foreach (var oldToken in oldRefreshTokens)
                {
                    oldToken.RevokedAt = DateTime.UtcNow;
                    oldToken.ReasonRevoked = "Replaced by new login";
                    Console.WriteLine($"Login: Refresh token iptal edildi: {oldToken.Token}");
                }
                
                // Token'ları oluştur
                var accessToken = _jwtService.GenerateAccessToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken(user.Id);
                
                // Yeni refresh token'ı veritabanına kaydet
                _context.RefreshTokens.Add(refreshToken);
                await _context.SaveChangesAsync();
                
                // Response
                var response = new AuthResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken.Token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        IsPremium = user.IsPremium,
                        CreatedAt = user.CreatedAt,
                        LastLoginAt = user.LastLoginAt
                    },
                    ExpiresAt = DateTime.UtcNow.AddMinutes(60)
                };
                
                Log.Information("Kullanıcı başarıyla giriş yaptı: {Email}, UserId: {UserId}", request.Email, user.Id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Login işlemi sırasında hata: {Email}", request.Email);
                return StatusCode(500, new { error = "Giriş işlemi sırasında bir hata oluştu" });
            }
        }
        
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                // Refresh token'ı bul
                var refreshToken = await _context.RefreshTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);
                
                if (refreshToken == null || refreshToken.RevokedAt != null || refreshToken.ExpiresAt <= DateTime.UtcNow)
                {
                    return BadRequest(new { error = "Geçersiz refresh token" });
                }
                
                // Yeni token'ları oluştur
                var newAccessToken = _jwtService.GenerateAccessToken(refreshToken.User);
                var newRefreshToken = _jwtService.GenerateRefreshToken(refreshToken.User.Id);
                
                // Eski refresh token'ı iptal et
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.ReplacedByToken = newRefreshToken.Token;
                refreshToken.ReasonRevoked = "Replaced by new token";
                
                // Yeni refresh token'ı kaydet
                _context.RefreshTokens.Add(newRefreshToken);
                await _context.SaveChangesAsync();
                
                // Response
                var response = new AuthResponse
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken.Token,
                    User = new UserDto
                    {
                        Id = refreshToken.User.Id,
                        Name = refreshToken.User.Name,
                        Email = refreshToken.User.Email,
                        IsPremium = refreshToken.User.IsPremium,
                        CreatedAt = refreshToken.User.CreatedAt,
                        LastLoginAt = refreshToken.User.LastLoginAt
                    },
                    ExpiresAt = DateTime.UtcNow.AddMinutes(60)
                };
                
                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Token yenileme sırasında bir hata oluştu" });
            }
        }
        
        [HttpGet("validate-token")]
        public async Task<IActionResult> ValidateToken([FromQuery] string? refreshToken = null)
        {
            try
            {
                // Authorization header'dan access token'ı al
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader == null || !authHeader.StartsWith("Bearer "))
                {
                    Console.WriteLine("ValidateToken: Authorization header eksik veya geçersiz");
                    return Unauthorized(new { error = "Authorization header eksik" });
                }

                var accessToken = authHeader.Substring("Bearer ".Length);
                
                // Access token'ı validate et
                if (!_jwtService.ValidateToken(accessToken))
                {
                    Console.WriteLine("ValidateToken: Access token geçersiz");
                    return Unauthorized(new { error = "Access token geçersiz" });
                }

                // Eğer refresh token da verilmişse, onu da kontrol et
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var refreshTokenEntity = await _context.RefreshTokens
                        .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

                    if (refreshTokenEntity == null || refreshTokenEntity.RevokedAt != null || refreshTokenEntity.ExpiresAt <= DateTime.UtcNow)
                    {
                        Console.WriteLine("ValidateToken: Refresh token iptal edilmiş veya geçersiz");
                        return Unauthorized(new { error = "Refresh token iptal edilmiş" });
                    }
                }

                // Access token'dan user ID'yi al
                var userId = _jwtService.GetUserIdFromToken(accessToken);
                Console.WriteLine($"ValidateToken: UserId: {userId} - Başarılı");
                return Ok(new { valid = true, userId = userId });
            }
                    catch (Exception)
        {
            return Unauthorized(new { error = "Token geçersiz" });
        }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromQuery] string refreshToken)
        {
            try
            {
                // Authorization header'dan token'ı al
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != null && authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader.Substring("Bearer ".Length);
                    var userId = _jwtService.GetUserIdFromToken(token);
                    
                    // Kullanıcının refresh token'ını iptal et
                    var activeTokens = await _context.RefreshTokens
                        .Where(rt => rt.UserId == userId && rt.Token == refreshToken && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
                        .ToListAsync();
                    
                    foreach (var activeToken in activeTokens)
                    {
                        activeToken.RevokedAt = DateTime.UtcNow;
                        activeToken.ReasonRevoked = "Logout";
                    }
                    
                    await _context.SaveChangesAsync();
                }
                
                return Ok(new { message = "Başarıyla çıkış yapıldı" });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Çıkış işlemi sırasında bir hata oluştu" });
            }
        }
        
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
        
        private bool VerifyPassword(string password, string hash)
        {
            var hashedPassword = HashPassword(password);
            return hashedPassword == hash;
        }
    }
}
