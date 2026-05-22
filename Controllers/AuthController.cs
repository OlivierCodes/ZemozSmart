using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ZemozSmart.Data;
using ZemozSmart.Models;

namespace ZemozSmart.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ZemozDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ZemozDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var agent = await _context.Agents
                .FirstOrDefaultAsync(a => a.Username == request.Username && a.PasswordHash == request.Password);

            if (agent == null)
                return Unauthorized("Identifiants incorrects.");

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, agent.Username),
                    new Claim(ClaimTypes.NameIdentifier, agent.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new { Token = tokenString, Username = agent.Username });
        }

        [HttpPost("register-agent")]
        public async Task<IActionResult> Register([FromBody] LoginRequest request)
        {
            if (await _context.Agents.AnyAsync(a => a.Username == request.Username))
                return BadRequest("Cet agent existe déjà.");

            var agent = new Agent
            {
                Username = request.Username,
                PasswordHash = request.Password // Simplifié pour le test
            };

            _context.Agents.Add(agent);
            await _context.SaveChangesAsync();
            return Ok("Agent enregistré avec succès.");
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
