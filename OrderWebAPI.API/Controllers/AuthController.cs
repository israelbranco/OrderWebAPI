using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace OrderWebAPI.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    // Exemplo simples de usuários em memória (não usar em produção)
    private static readonly Dictionary<string, string> _users = new()
    {
        { "admin", "admin123" },
        { "user", "user123" }
    };

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public record LoginRequest(string Username, string Password);

    [HttpPost("token")]
    [AllowAnonymous]
    public IActionResult GenerateToken([FromBody] LoginRequest? request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "É preciso informar o Usuário e Senha correta." });

        if (!ValidateCredentials(request.Username, request.Password))
            return Unauthorized(new { error = "O Usuário ou a Senha estão incorretos." });

        var jwtKey = _configuration["Jwt:Key"] ?? "SuperSecretKeyForJwtAuthenticationMustBeLongEnough123!";
        var issuer = _configuration["Jwt:Issuer"] ?? "OrderWebAPI";
        var audience = _configuration["Jwt:Audience"] ?? "OrderWebAPIUsers";

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(jwtKey);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, request.Username)
        };

        // exemplo: atribuir role para o usuário "admin"
        if (request.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return Ok(new { token = tokenString });
    }

    private static bool ValidateCredentials(string username, string password)
    {
        return _users.TryGetValue(username, out var storedPassword) && storedPassword == password;
    }
}
