using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.MedicalRecords.Dtos;
using Zyara.Services.Features.MedicalRecords.Interfaces;

namespace Zyara.Web.Areas.Provider.Controllers
{
    /// <summary>Visit record of one booking: service note (doctor/nurse) + prescription (doctor only).</summary>
    [Area("Provider")]
    [Authorize(Roles = "Doctor,Nurse")]
    public sealed class MedicalRecordsController : Controller
    {
        private readonly IMedicalRecordService _records;
        private readonly ICurrentUser _currentUser;

        public MedicalRecordsController(IMedicalRecordService records, ICurrentUser currentUser)
        {
            _records = records;
            _currentUser = currentUser;
        }

        // GET /Provider/MedicalRecords/Edit?bookingId=5
        [HttpGet]
        public async Task<IActionResult> Edit(int bookingId, CancellationToken cancellationToken)
        {
            var result = await _records.GetForProviderAsync(_currentUser.RequireUserId(), bookingId, cancellationToken);
            if (!result.Succeeded)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction("Index", "Bookings");
            }

            return View(result.Value);
        }

        // POST /Provider/MedicalRecords/SavePrescription
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePrescription(SavePrescriptionDto dto, CancellationToken cancellationToken)
        {
            var result = await _records.SavePrescriptionAsync(_currentUser.RequireUserId(), dto, cancellationToken);
            Flash(result.Succeeded, "Prescription saved.", result.Error);
            return RedirectToAction(nameof(Edit), new { bookingId = dto.BookingId });
        }

        // POST /Provider/MedicalRecords/SaveNote
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveNote(SaveServiceNoteDto dto, CancellationToken cancellationToken)
        {
            var result = await _records.SaveNoteAsync(_currentUser.RequireUserId(), dto, cancellationToken);
            Flash(result.Succeeded, "Note saved.", result.Error);
            return RedirectToAction(nameof(Edit), new { bookingId = dto.BookingId });
        }

        private void Flash(bool ok, string success, string? error)
        {
            if (ok) TempData["Message"] = success;
            else TempData["Error"] = error;
        }
    }
}
