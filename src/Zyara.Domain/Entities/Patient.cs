namespace Zyara.Domain.Entities;

public class Patient : User
{
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>Cancellation penalty the patient still owes (cash / unpaid bookings). Collected with his next completed visit.</summary>
    public decimal OutstandingPenalty { get; set; }
    public List<Address> Addresses { get; set; } = new();
}
