// T13 - Link API by Geolocation (GPS)
// Buttons (inside a [data-geo-scope] element or a <form>):
//   data-geo-action="near-me"  -> fills lat/lng, sets SortBy=distance and submits the search form
//   data-geo-action="detect"   -> only fills lat/lng (Edit Profile: "Detect my location")
//   data-geo-action="clear"    -> empties lat/lng (search form: also resubmits)
// Fields: [data-geo-lat], [data-geo-lng], optional [data-geo-sort] (select), optional [data-geo-status] (text).
// navigator.geolocation needs HTTPS or localhost; denial/failure never breaks the page.
(function () {
    'use strict';

    function scopeOf(el) {
        return el.closest('[data-geo-scope]') || el.closest('form') || document;
    }

    function setStatus(scope, message, isError) {
        var node = scope.querySelector('[data-geo-status]');
        if (!node) return;
        node.textContent = message || '';
        node.classList.toggle('text-danger', !!isError);
        node.classList.toggle('text-muted', !isError);
    }

    function errorMessage(err) {
        switch (err && err.code) {
            case 1: return 'Location permission was denied. You can still search without it.';
            case 2: return 'Your location is currently unavailable.';
            case 3: return 'Getting your location timed out. Please try again.';
            default: return 'Could not get your location.';
        }
    }

    function ensureSortOption(select) {
        if (!select) return;
        var found = Array.prototype.some.call(select.options, function (o) { return o.value === 'distance'; });
        if (!found) {
            var opt = document.createElement('option');
            opt.value = 'distance';
            opt.textContent = 'Nearest First';
            select.appendChild(opt);
        }
        select.value = 'distance';
    }

    function setBusy(btn, busy) {
        btn.disabled = busy;
        btn.setAttribute('aria-busy', busy ? 'true' : 'false');
    }

    function locate(btn, onSuccess) {
        var scope = scopeOf(btn);

        if (!('geolocation' in navigator)) {
            setStatus(scope, 'Your browser does not support location.', true);
            return;
        }
        if (window.isSecureContext === false) {
            setStatus(scope, 'Location needs HTTPS (or localhost).', true);
            return;
        }

        setBusy(btn, true);
        setStatus(scope, 'Getting your location...', false);

        navigator.geolocation.getCurrentPosition(
            function (pos) {
                setBusy(btn, false);
                var lat = pos.coords.latitude.toFixed(6);
                var lng = pos.coords.longitude.toFixed(6);
                var latInput = scope.querySelector('[data-geo-lat]');
                var lngInput = scope.querySelector('[data-geo-lng]');
                if (latInput) latInput.value = lat;
                if (lngInput) lngInput.value = lng;
                onSuccess(scope, lat, lng);
            },
            function (err) {
                setBusy(btn, false);
                setStatus(scope, errorMessage(err), true); // page keeps working normally
            },
            { enableHighAccuracy: false, timeout: 10000, maximumAge: 300000 }
        );
    }

    function submitForm(form) {
        if (!form) return;
        if (typeof form.requestSubmit === 'function') form.requestSubmit();
        else form.submit();
    }

    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-geo-action]');
        if (!btn) return;

        var action = btn.getAttribute('data-geo-action');
        var scope = scopeOf(btn);

        if (action === 'near-me') {
            locate(btn, function (s) {
                ensureSortOption(s.querySelector('[data-geo-sort]'));
                submitForm(s.tagName === 'FORM' ? s : s.closest('form'));
            });
        } else if (action === 'detect') {
            locate(btn, function (s, lat, lng) {
                setStatus(s, 'Detected: ' + lat + ', ' + lng + ' (click "Save Changes" to keep it).', false);
            });
        } else if (action === 'clear') {
            var latInput = scope.querySelector('[data-geo-lat]');
            var lngInput = scope.querySelector('[data-geo-lng]');
            if (latInput) latInput.value = '';
            if (lngInput) lngInput.value = '';
            var sort = scope.querySelector('[data-geo-sort]');
            if (sort && sort.value === 'distance') sort.value = 'default';
            var radius = scope.querySelector('[name="RadiusKm"]');
            if (radius) radius.value = '';
            var form = scope.tagName === 'FORM' ? scope : scope.closest('form');
            if (form && form.id === 'filterForm') {
                submitForm(form);
            } else {
                setStatus(scope, 'Location cleared (click "Save Changes" to keep it).', false);
            }
        }
    });
})();
