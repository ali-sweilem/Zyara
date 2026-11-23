// T9 (Adham): ping /Session/Refresh every 10 minutes so an open page does not get logged out
// (cookie = 30 min sliding). If the server answers 401 the session is over -> go to Login.
(function () {
    var form = document.getElementById('session-keepalive-form');
    if (!form) return;

    var tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');
    var token = tokenInput ? tokenInput.value : '';
    var TEN_MINUTES = 10 * 60 * 1000;

    function goToLogin() {
        var returnUrl = encodeURIComponent(window.location.pathname + window.location.search);
        window.location.href = '/Account/Login?returnUrl=' + returnUrl;
    }

    function ping() {
        fetch(form.action, {
            method: 'POST',
            credentials: 'same-origin',
            headers: {
                'RequestVerificationToken': token,
                'X-Requested-With': 'XMLHttpRequest'   // makes the server answer 401 instead of a 302 to the login page
            }
        }).then(function (response) {
            if (response.status === 401) goToLogin();
        }).catch(function () { /* offline / network error: try again next time */ });
    }

    window.setInterval(ping, TEN_MINUTES);
})();
