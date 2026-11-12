using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Bookings.Dtos;
using Zyara.Services.Features.Bookings.Interfaces;
using Zyara.Web.Models;

namespace Zyara.Web.Controllers;

public sealed class BookingController : Controller
{
    // TODO: replace with CurrentUser once Abdelrhman pushes it
    private const int CurrentPatientId = 1;

    private readonly ISlotService _slotService;
    private readonly IBookingService _bookingService;

    public BookingController(ISlotService slotService, IBookingService bookingService)
    {
        _slotService = slotService;
        _bookingService = bookingService;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int providerId, int serviceId, CancellationToken cancellationToken)
    {
        var slots = await _slotService.GetAvailableSlotsAsync(providerId,null, cancellationToken);

        return View(new BookingCreateViewModel
        {
            ProviderId = providerId,
            ServiceId = serviceId,
            Slots = slots.ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingCreateViewModel vm, CancellationToken cancellationToken)
    {
        if (vm.SelectedSlotId == 0)
        {
            vm.ErrorMessage = "Please choose a time slot.";
        }
        else
        {
            var dto = new CreateBookingDto
            {
                ProviderId = vm.ProviderId,
                ServiceId = vm.ServiceId,
                SlotId = vm.SelectedSlotId,

                PatientName = vm.PatientName,
                PatientAge = vm.PatientAge,
                PatientPhone = vm.PatientPhone,
                PatientNote = vm.PatientNote,

                Street = vm.Street,
                City = vm.City,

                AddressLabel = vm.AddressLabel,
                District = vm.District
            };

            var result = await _bookingService.CreateBookingAsync(CurrentPatientId, dto, cancellationToken);

            if (result.Succeeded)
            {
                // Payment page is built by another teammate
                return Redirect($"/Payment/Create?bookingId={result.BookingId}");
            }

            vm.ErrorMessage = result.Error;
        }

        var slots = await _slotService.GetAvailableSlotsAsync(vm.ProviderId,null, cancellationToken);
        vm.Slots = slots.ToList();
        return View(vm);
    }
}
