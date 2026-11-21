using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zyara.Services.Features.Identity.Interfaces;
using Zyara.Services.Features.Wallet.Dtos;
using Zyara.Services.Features.Wallet.Interfaces;

namespace Zyara.Web.Areas.Provider.Controllers
{
    /// <summary>The provider's wallet: balance, ledger, withdrawal request. The provider id always comes from ICurrentUser.</summary>
    [Area("Provider")]
    [Authorize(Roles = "Doctor,Nurse")]
    public sealed class WalletController : Controller
    {
        private readonly IWalletService _wallet;
        private readonly ICurrentUser _currentUser;

        public WalletController(IWalletService wallet, ICurrentUser currentUser)
        {
            _wallet = wallet;
            _currentUser = currentUser;
        }

        // GET /Provider/Wallet
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
        {
            var model = await _wallet.GetWalletAsync(_currentUser.RequireUserId(), page, 10, cancellationToken);
            return View(model);
        }

        // POST /Provider/Wallet/Withdraw
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(WithdrawRequestDto dto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                                    ?? "Enter a valid amount.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _wallet.RequestWithdrawalAsync(_currentUser.RequireUserId(), dto.Amount, cancellationToken);

            if (result.Succeeded) TempData["Message"] = "Withdrawal requested.";
            else TempData["Error"] = result.Error;

            return RedirectToAction(nameof(Index));
        }
    }
}
