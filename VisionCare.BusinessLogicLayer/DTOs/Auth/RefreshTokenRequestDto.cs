using System;

namespace VisionCare.BusinessLogicLayer.DTOs.Auth;

public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = null!;
}
