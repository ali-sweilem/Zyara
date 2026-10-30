document.addEventListener('DOMContentLoaded', () => {
  // Role pills
  document.querySelectorAll('.btn-role').forEach(b => b.addEventListener('click', () => {
    const role = b.dataset.role;
    const hidden = document.getElementById('Role');
    if (hidden) hidden.value = role;
    document.querySelectorAll('.btn-role').forEach(x => {
      x.classList.remove('btn-primary');
      x.classList.add('btn-outline-secondary');
    });
    b.classList.remove('btn-outline-secondary');
    b.classList.add('btn-primary');
    const doctorOnly = document.getElementById('doctorOnly');
    if (doctorOnly) {
      doctorOnly.classList.toggle('hidden', role !== 'Doctor');
      const lic = document.getElementById('LicenseNumber');
      if (lic) {
        lic.disabled = role !== 'Doctor';
        if (role !== 'Doctor') lic.value = '';
      }
    }
  }));

  // Toggle initial state
  const initialRole = document.getElementById('Role')?.value || 'Patient';
  document.querySelectorAll('.btn-role').forEach(b => {
    if (b.dataset.role === initialRole) {
      b.classList.remove('btn-outline-secondary');
      b.classList.add('btn-primary');
    }
  });
  const doctorOnly = document.getElementById('doctorOnly');
  if (doctorOnly) doctorOnly.classList.toggle('hidden', initialRole !== 'Doctor');
  const lic = document.getElementById('LicenseNumber');
  if (lic) { lic.disabled = initialRole !== 'Doctor'; }

  // Password toggle
  document.querySelectorAll('[data-toggle-password]').forEach(btn => {
    btn.addEventListener('click', () => {
      const id = btn.dataset.togglePassword;
      const input = document.getElementById(id);
      if (!input) return;
      const isHidden = input.type === 'password';
      input.type = isHidden ? 'text' : 'password';
      btn.setAttribute('aria-label', isHidden ? 'Hide password' : 'Show password');
    });
  });

  // Caps Lock warning
  const caps = document.getElementById('capsWarning');
  if (caps) {
    const checkCaps = e => {
      if (e.getModifierState && e.getModifierState('CapsLock')) {
        caps.classList.remove('hidden');
      } else {
        caps.classList.add('hidden');
      }
    };
    document.addEventListener('keydown', checkCaps);
    document.addEventListener('keyup', checkCaps);
    caps.classList.add('hidden');
  }

  // Strength meter (basic)
  const pw = document.getElementById('Password');
  if (pw) {
    pw.addEventListener('input', () => {
      // Only set if strength element exists; otherwise no-op
      const bar = document.getElementById('strengthBar');
      if (!bar) return;
      const val = pw.value;
      let pct = 0;
      if (val.length >= 8) pct = 40;
      if (val.length >= 12 && /[A-Z]/.test(val) && /[0-9]/.test(val)) pct = 100;
      else if (val.length >= 10) pct = 70;
      bar.style.width = pct + '%';
      bar.className = pct >= 70 ? 'strength-strong' : pct >= 40 ? 'strength-medium' : 'strength-weak';
    });
  }

  // Loading state on submit
  document.querySelectorAll('form').forEach(form => {
    form.addEventListener('submit', () => {
      const btn = form.querySelector('.auth-btn');
      if (btn) { btn.disabled = true; btn.textContent = 'Please wait...'; }
    });
  });

  // OTP boxes
  document.querySelectorAll('.otp-box').forEach(box => {
    box.addEventListener('input', () => {
      const val = box.value.replace(/\D/g, '').slice(-1);
      box.value = val;
      const idx = parseInt(box.dataset.otpIndex || '1', 10);
      if (val && idx < 4) {
        const next = document.querySelector('.otp-box[data-otp-index="' + (idx + 1) + '"]');
        if (next) next.focus();
      }
      if (idx === 4 && val) {
        const frm = document.getElementById('otp-form');
        if (frm) frm.submit();
      }
    });
    box.addEventListener('keydown', e => {
      const idx = parseInt(box.dataset.otpIndex || '1', 10);
      if (e.key === 'Backspace' && box.value === '' && idx > 1) {
        e.preventDefault();
        const prev = document.querySelector('.otp-box[data-otp-index="' + (idx - 1) + '"]');
        if (prev) prev.focus();
      }
      if (e.ctrlKey && (e.key === 'v' || e.key === 'V')) {
        e.preventDefault();
        navigator.clipboard.readText().then(t => {
          const clean = t.replace(/\D/g, '').slice(0, 4);
          const boxes = document.querySelectorAll('.otp-box');
          clean.split('').forEach((ch, i) => { if (boxes[i]) boxes[i].value = ch; });
          const focusIdx = Math.min(clean.length, 3);
          if (boxes[focusIdx]) boxes[focusIdx].focus();
        }).catch(() => {});
      }
    });
  });
});
