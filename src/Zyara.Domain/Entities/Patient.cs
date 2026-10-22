namespace Zyara.Domain.Entities;

public class Patient : User
{
    public DateOnly? DateOfBirth { get; set; }
    public List<Address> Addresses { get; set; } = new();
}