using Microsoft.EntityFrameworkCore;
using Zyara.Domain.Entities;
using Zyara.Domain.Enums;
using Zyara.Domain.Rules;
using Zyara.Services.Common;
using Zyara.Services.Data;

namespace Zyara.Services.Features.Bookings;

public class BookingService : IBookingService
{
    private readonly ZyaraDbContext _db;
    private readonly TimeProvider _timeProvider;

    public BookingService(ZyaraDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResult<Booking>> GetProviderBookingsAsync(int providerId, BookingFilter filter)
    {
        var page = Math.Max(filter.Page, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var q = _db.Bookings.AsNoTracking()
            .Where(b => b.ProviderId == providerId)
            .Include(b => b.Patient)
            .Include(b => b.Service)
            .Include(b => b.TimeSlot)
            .Include(b => b.Payment)
            .OrderByDescending(b => b.Id) as IQueryable<Booking>;

        if (filter.Status.HasValue)
            q = q.Where(b => b.Status == filter.Status.Value);
        if (filter.PaymentMethod.HasValue)
            q = q.Where(b => b.Payment != null && b.Payment.Method == filter.PaymentMethod.Value);
        if (filter.ServiceType.HasValue)
            q = q.Where(b => b.Service != null && b.Service.Type == filter.ServiceType.Value);
        if (filter.DateFrom.HasValue)
            q = q.Where(b => b.TimeSlot != null && b.TimeSlot.Date >= filter.DateFrom.Value);
        if (filter.DateTo.HasValue)
            q = q.Where(b => b.TimeSlot != null && b.TimeSlot.Date <= filter.DateTo.Value);
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var t = filter.SearchText.ToLower();
            q = q.Where(b => b.Patient != null && (b.Patient.FullName.ToLower().Contains(t) || b.Patient.Phone.Contains(t)));
        }

        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize)
            .Take(pageSize).ToListAsync();

        return new PagedResult<Booking> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<ServiceResult<BookingDetailsResult>> GetBookingDetailsAsync(int providerId, int bookingId)
    {
        var b = await _db.Bookings
            .Include(x => x.Patient)
            .Include(x => x.Service)
            .Include(x => x.TimeSlot)
            .Include(x => x.Payment)
            .Include(x => x.Address)
            .Include(x => x.Rating)
            .FirstOrDefaultAsync(x => x.Id == bookingId);
        if (b == null || b.ProviderId != providerId)
            return ServiceResult<BookingDetailsResult>.Fail("Booking not found or not owned by this provider.");

        var notes = await _db.ServiceNotes
            .AsNoTracking()
            .Where(s => s.BookingId == bookingId)
            .OrderByDescending(s => s.Id)
            .ToListAsync();

        return ServiceResult<BookingDetailsResult>.Ok(new BookingDetailsResult
        {
            Booking = b,
            ServiceNotes = notes
        });
    }

    public async Task<ServiceResult> AcceptAsync(int providerId, int bookingId)
    {
        var b = await LoadForActionAsync(bookingId, providerId);
        if (b == null) return ServiceResult.Fail("Booking not found.");
        if (b.Status != BookingStatus.PendingProviderApproval) return ServiceResult.Fail("Can only accept pending bookings.");
        if (b.Payment == null) return ServiceResult.Fail("Booking data is incomplete (Payment missing).");
        if (b.TimeSlot == null) return ServiceResult.Fail("Booking data is incomplete (TimeSlot missing).");

        if (b.Payment.Method == PaymentMethod.OnlinePayment)
        {
            if (b.Payment.Status != PaymentStatus.Authorized)
                return ServiceResult.Fail("Online payment must be Authorized before capture.");
            b.Payment.Capture();
        }

        b.Status = b.Payment.Method == PaymentMethod.OnlinePayment
            ? BookingStatus.Confirmed
            : BookingStatus.ConfirmedCollectionPending;
        b.TimeSlot.Status = TimeSlotStatus.Booked;

        AddPatientNotification(b.PatientId, "Your booking has been confirmed.");
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RejectAsync(int providerId, int bookingId)
    {
        var b = await LoadForActionAsync(bookingId, providerId);
        if (b == null) return ServiceResult.Fail("Booking not found.");
        if (b.Status != BookingStatus.PendingProviderApproval) return ServiceResult.Fail("Can only reject pending bookings.");
        if (b.Payment == null) return ServiceResult.Fail("Booking data is incomplete (Payment missing).");
        if (b.TimeSlot == null) return ServiceResult.Fail("Booking data is incomplete (TimeSlot missing).");

        b.Status = BookingStatus.Rejected;
        b.TimeSlot.Status = TimeSlotStatus.Available;

        if (b.Payment.Method == PaymentMethod.OnlinePayment && b.Payment.Status == PaymentStatus.Authorized)
            b.Payment.Release();

        AddPatientNotification(b.PatientId, "Your booking was rejected.");
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> StartAsync(int providerId, int bookingId, string otp)
    {
        var b = await LoadForActionAsync(bookingId, providerId);
        if (b == null) return ServiceResult.Fail("Booking not found.");
        if (b.Status != BookingStatus.Confirmed && b.Status != BookingStatus.ConfirmedCollectionPending)
            return ServiceResult.Fail("Can only start confirmed bookings.");
        if (b.Service == null) return ServiceResult.Fail("Booking data is incomplete (Service missing).");

        if (b.Service.Type == ServiceType.DoctorHomeVisit || b.Service.Type == ServiceType.NurseHomeVisit)
        {
            if (string.IsNullOrWhiteSpace(otp) || otp.Trim() != (b.OtpCode ?? string.Empty).Trim())
                return ServiceResult.Fail("Invalid OTP.");
        }

        b.Status = BookingStatus.Started;
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> CompleteAsync(int providerId, int bookingId)
    {
        var b = await LoadForActionAsync(bookingId, providerId);
        if (b == null) return ServiceResult.Fail("Booking not found.");
        if (b.Status != BookingStatus.Started) return ServiceResult.Fail("Can only complete started bookings.");
        if (b.Payment == null) return ServiceResult.Fail("Booking data is incomplete (Payment missing).");
        if (b.Provider == null) return ServiceResult.Fail("Booking data is incomplete (Provider missing).");

        var settings = await _db.PlatformSettings.FirstOrDefaultAsync(x => x.Id == 1);
        if (settings == null) return ServiceResult.Fail("Platform settings missing.");

        if (b.Payment.Method == PaymentMethod.CashOnArrival && b.Payment.Status != PaymentStatus.Pending)
            return ServiceResult.Fail("Cash payment is not in a collectable state.");
        if (b.Payment.Method == PaymentMethod.OnlinePayment && b.Payment.Status != PaymentStatus.Captured)
            return ServiceResult.Fail("Online payment must be Captured before completing.");

        b.Commission = CommissionCalculator.Calculate(b.TotalPrice, settings.CommissionPercentage / 100m);

        if (b.Payment.Method == PaymentMethod.CashOnArrival)
        {
            b.Payment.Collect();
            b.Provider.WalletBalance -= b.Commission;
        }
        else if (b.Payment.Method == PaymentMethod.OnlinePayment)
        {
            b.Provider.WalletBalance += (b.TotalPrice - b.Commission);
            // TODO: spec does not define online payout, team to review
        }

        b.Status = BookingStatus.Completed;
        AddPatientNotification(b.PatientId, "Your booking is completed.");
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> CancelAsync(int providerId, int bookingId)
    {
        var b = await LoadForActionAsync(bookingId, providerId);
        if (b == null) return ServiceResult.Fail("Booking not found.");
        if (b.Status != BookingStatus.PendingProviderApproval && b.Status != BookingStatus.Confirmed && b.Status != BookingStatus.ConfirmedCollectionPending)
            return ServiceResult.Fail("Cannot cancel from this status.");
        if (b.Payment == null) return ServiceResult.Fail("Booking data is incomplete (Payment missing).");
        if (b.TimeSlot == null) return ServiceResult.Fail("Booking data is incomplete (TimeSlot missing).");

        var visitStart = b.TimeSlot.Date.Date + b.TimeSlot.StartTime;
        var now = _timeProvider.GetLocalNow().DateTime;
        if (visitStart - now < TimeSpan.FromHours(24))
            return ServiceResult.Fail("Cancellation is allowed only 24 hours or more before the visit.");

        b.Status = BookingStatus.Cancelled;
        b.TimeSlot.Status = TimeSlotStatus.Available;

        if (b.Payment.Method == PaymentMethod.OnlinePayment && b.Payment.Status == PaymentStatus.Authorized)
            b.Payment.Release();
        else if (b.Payment.Method == PaymentMethod.OnlinePayment && b.Payment.Status == PaymentStatus.Captured)
            b.Payment.Refund(b.Payment.Amount);

        AddPatientNotification(b.PatientId, "Your booking was cancelled.");
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> AddServiceNoteAsync(int providerId, int bookingId, string content)
    {
        var b = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId);
        if (b == null || b.ProviderId != providerId) return ServiceResult.Fail("Booking not found.");
        if (b.Status != BookingStatus.Started && b.Status != BookingStatus.Completed)
            return ServiceResult.Fail("Notes can only be added after start.");

        _db.ServiceNotes.Add(new ServiceNote { BookingId = bookingId, Content = content });
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    private void AddPatientNotification(int patientId, string message)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = patientId,
            Title = "Booking Update",
            Message = message,
            IsRead = false
        });
    }

    private async Task<Booking?> LoadForActionAsync(int bookingId, int providerId)
    {
        return await _db.Bookings
            .Include(b => b.Payment)
            .Include(b => b.TimeSlot)
            .Include(b => b.Service)
            .Include(b => b.Provider)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.ProviderId == providerId);
    }
}
