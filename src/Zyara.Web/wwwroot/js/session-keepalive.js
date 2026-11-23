/**
 * T9 (Adham): Session keep-alive.
 * Pings /Session/Refresh every 10 minutes so sliding expiration does not expire while the tab is open.
 * If server responds with 401 (e.g. deactivated or session expired), redirects to /Account/Login.
 */
(function () {
    var intervalMs = 10 * 60 * 1000;   // 10 minutes as specified in task sheet
    var url = '/Session/Refresh';

    function ping() {
        var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]') ||
                         document.getElementById('__sessionKeepAliveToken');

        var headers = {
            'X-Requested-With': 'XMLHttpRequest'
        };

        var body = null;
        if (tokenInput && tokenInput.value) {
            headers['RequestVerificationToken'] = tokenInput.value;
            body = new URLSearchParams();
            body.append('__RequestVerificationToken', tokenInput.value);
        }

        fetch(url, {
            method: 'POST',
            credentials: 'same-origin',
            headers: headers,
            body: body
        }).then(function (response) {
            if (response.status === 401) {
                // Session expired or account deactivated server-side; redirect to login
                window.location.href = '/Account/Login';
            }
        }).catch(function () {
            // Network error – silently ignore; the next ping will retry
        });
    }

    setInterval(ping, intervalMs);
})();

