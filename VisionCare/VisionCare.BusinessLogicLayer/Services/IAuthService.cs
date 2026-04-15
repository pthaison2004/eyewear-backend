using System.Threading.Tasks;
using VisionCare.BusinessLogicLayer.DTOs.Auth;

namespace VisionCare.BusinessLogicLayer.Services;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);
    Task<bool> LogoutAsync(string email);
}
