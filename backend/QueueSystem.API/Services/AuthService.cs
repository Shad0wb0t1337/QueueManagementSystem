using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QueueSystem.API.Common;
using QueueSystem.API.Data;
using QueueSystem.API.DTOs.Requests;
using QueueSystem.API.DTOs.Responses;
using QueueSystem.API.Models;
using QueueSystem.API.Services.Interfaces;

namespace QueueSystem.API.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<AppUser> _passwordHasher;

    public AuthService(AppDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _passwordHasher = new PasswordHasher<AppUser>();
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        AppUser appUser = await _dbContext.AppUsers
            .Include(user => user.Operator)
                .ThenInclude(@operator => @operator!.Counter)
                    .ThenInclude(counter => counter!.CounterServiceTypes)
                        .ThenInclude(item => item.ServiceType)
            .FirstOrDefaultAsync(user => user.Username == request.Username, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid username or password.");

        PasswordVerificationResult verifyResult = _passwordHasher.VerifyHashedPassword(appUser, appUser.PasswordHash, request.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        if (appUser.Role == UserRole.Operator && appUser.OperatorId == null)
        {
            throw new ForbiddenException("Operator account is missing linked operator profile.");
        }

        string role = appUser.Role.ToString();
        int? counterId = appUser.Operator?.CounterId;

        string token = GenerateJwt(appUser.Id, appUser.Username, role, appUser.OperatorId, counterId);

        return MapAuthResponse(appUser, token);
    }

    public async Task<AuthResponseDto> GetMeAsync(int appUserId, string token, CancellationToken cancellationToken)
    {
        AppUser appUser = await _dbContext.AppUsers
            .Include(user => user.Operator)
                .ThenInclude(@operator => @operator!.Counter)
                    .ThenInclude(counter => counter!.CounterServiceTypes)
                        .ThenInclude(item => item.ServiceType)
            .FirstOrDefaultAsync(user => user.Id == appUserId, cancellationToken)
            ?? throw new NotFoundException("Authenticated user was not found.");

        return MapAuthResponse(appUser, token);
    }

    public string GenerateJwt(int userId, string username, string role, int? operatorId, int? counterId)
    {
        string key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is missing.");
        string issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT issuer is missing.");
        string audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT audience is missing.");
        string expiryHoursRaw = _configuration["Jwt:ExpiryHours"] ?? "8";

        if (!double.TryParse(expiryHoursRaw, out double expiryHours))
        {
            throw new InvalidOperationException("JWT expiry is invalid.");
        }

        List<Claim> claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role)
        };

        if (operatorId.HasValue)
        {
            claims.Add(new Claim("operatorId", operatorId.Value.ToString()));
        }

        if (counterId.HasValue)
        {
            claims.Add(new Claim("counterId", counterId.Value.ToString()));
        }

        SymmetricSecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        SigningCredentials credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static AuthResponseDto MapAuthResponse(AppUser appUser, string token)
    {
        List<CounterServiceType> supportedServiceTypeLinks = appUser.Operator?.Counter?.CounterServiceTypes
            .Where(item => item.ServiceType != null)
            .OrderBy(item => item.ServiceType!.Name)
            .ToList() ?? new List<CounterServiceType>();

        SupportedServiceTypeResponseDto? firstSupportedServiceType = supportedServiceTypeLinks
            .Select(item => new SupportedServiceTypeResponseDto
            {
                Id = item.ServiceTypeId,
                Name = item.ServiceType!.Name,
                IsEnabled = item.IsEnabled
            })
            .FirstOrDefault();

        return new AuthResponseDto
        {
            Token = token,
            Username = appUser.Username,
            Role = appUser.Role.ToString(),
            OperatorId = appUser.OperatorId,
            OperatorName = appUser.Operator?.Name,
            CounterId = appUser.Operator?.CounterId,
            CounterName = appUser.Operator?.Counter?.Name,
            CounterStatus = appUser.Operator?.Counter?.Status.ToString(),
            ServiceTypeId = firstSupportedServiceType?.Id,
            ServiceTypeName = firstSupportedServiceType?.Name,
            SupportedServiceTypeIds = supportedServiceTypeLinks
                .Select(item => item.ServiceTypeId)
                .OrderBy(id => id)
                .ToList(),
            SupportedServiceTypes = supportedServiceTypeLinks
                .Select(item => new SupportedServiceTypeResponseDto
                {
                    Id = item.ServiceTypeId,
                    Name = item.ServiceType!.Name,
                    IsEnabled = item.IsEnabled
                })
                .ToList()
        };
    }
}
