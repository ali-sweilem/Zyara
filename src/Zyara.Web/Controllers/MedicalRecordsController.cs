using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.MedicalRecords.Interfaces;

namespace Zyara.Web.Controllers
{
    /// <summary>The patient reads the prescription and note of HIS OWN booking.</summary>
    [Authorize(Roles = "Patient")]
    public sealed class MedicalRecordsController : Controller
    {
        private readonly IMedicalRecordService _records;
        private readonly ICurrentUser _currentUser;

        public MedicalRecordsController(IMedicalRecordService records, ICurrentUser currentUser)
        {
            _records = records;
            _currentUser = currentUser;
        }

        // GET /MedicalRecords/Details?bookingId=5
        [HttpGet]
        public async Task<IActionResult> Details(int bookingId, CancellationToken cancellationToken)
        {
            var result = await _records.GetForPatientAsync(_currentUser.RequireUserId(), bookingId, cancellationToken);
            if (!result.Succeeded) return NotFound();   // someone else's booking looks exactly like a missing one

            return View(result.Value);
        }
    }
}
