using System;
using System.ComponentModel.DataAnnotations;

namespace VisionCare.BusinessLogicLayer.DTOs.ManagerPreOrder;

public class UpdateDepositConfigDto
{
    [Range(0.01, 1.0, ErrorMessage = "Tỉ lệ cọc phải từ 1% đến 100% (0.01 - 1.0)")]
    public decimal DepositRatio { get; set; }
}
