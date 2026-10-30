| Item | Status | Notes |
|---|---|---|
| Login | Done | Page + controller + view; card layout; no role selector; DB role read; isActive check; generic error; redirect by role; provider pending/rejected handled |
| Register | Done | VM + view + controller (Patient/Doctor/Nurse); gender; DOB (optional); license (Doctor); terms checkbox; hash; redirect Doctor/Nurse to under-review |
| Forgot Password | Done | Email/phone entry; success message same for all; sends to dev console |
| Reset / OTP | Done | 4-box OTP; new password; hash update; redirect login |
| Under-review page | Done | Minimal message; upload form deferred to Phase C |
| Logout | Done | POST + anti-forgery; dropdown when auth |
| Navbar | Done | Left: Home/Provider/Privacy; Right: Login (plain)/Register (pill) or dropdown + Logout; mobile hamburger preserved |
| AddDoctorLicenseNumber | Partial | Migration NOT generated (entity edited; DB not updated) |
| AddPasswordResetCode | Done | Migration 20261027000001 created; only table added; NOT applied to DB |
| dotnet-ef installed | Not done | Not installed globally; migrations done manually |
| Password hasher | Done | BCrypt.Net-Next 4.0.3 (existing package); not framework IPasswordHasher (custom entity) |
| Provider/Admin redirects | Partial | Patient -> /Patient/Placeholder; Provider -> existing /Provider; Admin -> /Home (placeholder) |
