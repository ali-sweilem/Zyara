namespace Zyara.Services.Data
{
    public interface IOtpSender { void Send(string destination, string code); }
    public class ConsoleOtpSender : IOtpSender { public void Send(string d, string c) => System.Console.WriteLine($"OTP to {d}: {c}"); }
}
