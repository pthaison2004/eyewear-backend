using System.ComponentModel.DataAnnotations;

namespace VisionCare.BusinessLogicLayer.DTOs.SalesComplaint;

public class ProcessComplaintRequestDto
{
    public string? ProcessedNote { get; set; }
    public int? AssignedTo { get; set; }  // staff user id to assign
    public string Priority { get; set; } = "normal";  // can reassign priority
}
