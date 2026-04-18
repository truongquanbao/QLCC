using System;

namespace ApartmentManager.DTO;

/// <summary>
/// Data Transfer Object for Apartment
/// </summary>
public class ApartmentDTO
{
    public int ApartmentID { get; set; }
    public string? ApartmentCode { get; set; }
    public string? BuildingCode { get; set; }
    public int FloorID { get; set; }
    public int? FloorNumber { get; set; }
    public int? BlockID { get; set; }
    public string? BlockName { get; set; }
    public int? BuildingID { get; set; }
    public string? BuildingName { get; set; }
    public double Area { get; set; }
    public string? ApartmentType { get; set; }
    public string? Status { get; set; }
    public int MaxResidents { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
