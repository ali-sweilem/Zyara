# Zyara Auth Test Cases

| ID | Area | Scenario | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| L-01 | Login | Valid Patient login | Enter email/phone + correct password; click Sign in | Redirect to Patient placeholder; session set | Auto | Pending |
| L-02 | Login | Valid Doctor login (Verified) | Log in with Doctor account, status Verified | Redirect to Provider Dashboard | Auto | Pending |
| L-03 | Login | Valid Nurse login | Log in with Nurse | Redirect to Provider | Auto | Pending |
| L-04 | Login | Valid Admin login | Log in with Admin | Redirect to Admin dashboard / placeholder | Auto | Pending |
| L-05 | Login | Wrong password | Correct email, wrong password | Generic message; no user enumeration; no redirect | Auto | Pending |
| L-06 | Login | Unknown email/phone | Non-existent identifier | Same generic message as L-05 | Auto | Pending |
| L-07 | Login | Login by email | Use email only | Works | Auto | Pending |
| L-08 | Login | Login by phone | Use +20 phone only | Works | Auto | Pending |
| L-09 | Login | Inactive account | Active user set IsActive=false; try login | Blocked with clear message | Auto | Pending |
| L-10 | Login | Pending provider | Doctor with VerificationStatus=Pending | Allowed login; redirect ONLY to /Account/UnderReview | Auto | Pending |
| L-11 | Login | Rejected provider | Doctor with Rejected + RejectionReason | Allowed login; redirect to /Account/UnderReview with rejection message | Auto | Pending |
| L-12 | Login | Lockout/delay | Repeat wrong attempts | Delay or lockout after threshold | Auto | Pending |
| L-13 | Login | Remember me | Check RememberMe; verify cookie persistent | Cookie HttpOnly Secure SameSite=Lax; 14d expiry | Auto | Pending |
| L-14 | Login | ReturnUrl local honored | /Account/Login?returnUrl=/Provider/Index; login success | Redirect to /Provider/Index | Auto | Pending |
| L-15 | Login | ReturnUrl external ignored | ?returnUrl=https://evil.com; login | Redirect to default (no open redirect) | Auto | Pending |
| L-16 | Login | Already signed in visits login | Auth user hits /Account/Login | Already shown / redirect to role page | Auto | Pending |
| L-17 | Login | Logout POST | Auth user submits Logout form | Clears session; redirects to Login; anti-forgery passes | Auto | Pending |
| L-18 | Login | Anonymous protected page | Anonymous accesses /Provider | Redirect to Login | Auto | Pending |

| ID | Area | Scenario | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| R-01 | Register | Patient registration | Fill all; role=Patient | User + Patient created; redirect Login | Auto | Pending |
| R-02 | Register | Doctor registration | Fill all + license; role=Doctor | User + Provider + Doctor; VerificationStatus=Pending; redirect UnderReview | Auto | Pending |
| R-03 | Register | Nurse registration | Fill all; role=Nurse | User + Provider + Nurse; Pending; redirect UnderReview | Auto | Pending |
| R-04 | Register | Gender saved | Select Male / Female | Stored in User.Gender (not null) | Auto | Pending |
| R-05 | Register | DOB saved (optional) | Patient sets DateOfBirth | Stored DateOnly? | Auto | Pending |
| R-06 | Register | Doctor license required | Doctor without LicenseNumber | Validation error; not saved | Auto | Pending |
| R-07 | Register | Duplicate email | Same email second time | Validation error | Auto | Pending |
| R-08 | Register | Duplicate phone | Same phone second time | Validation error | Auto | Pending |
| R-09 | Register | Invalid email format | Bad email | Validation error | Auto | Pending |
| R-10 | Register | Invalid phone (+20) | Wrong format | Validation error | Auto | Pending |
| R-11 | Register | Weak password | Short / simple | Validation error (rules to define) | Auto | Pending |
| R-12 | Register | Confirm mismatch | Password != Confirm | Error; not saved | Auto | Pending |
| R-13 | Register | Terms unchecked | TermsAccepted=false | Error; not saved | Auto | Pending |
| R-14 | Register | Admin self-register | Post role=Admin | Blocked / error | Auto | Pending |
| R-15 | Register | Password hashed | Verify PasswordHash not equal to input | BCrypt hash only | Auto | Pending |
| R-16 | Register | Email case-insensitive | "Ali@Ex" vs "ali@ex" | Treated same; trimmed | Auto | Pending |
| R-17 | Register | Role tampering | Post hidden role=Admin as Patient | Ignored / blocked by server logic | Auto | Pending |

| ID | Area | Scenario | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| F-01 | Forgot | Same message existing/non-existing | Submit real / fake email/phone | Same success message; no reveal | Auto | Pending |
| F-02 | Forgot | Code delivered | Submit; check console | 4-digit random; 5 min expiry | Manual | Pending |
| F-03 | Reset | Correct OTP | Enter correct 4-digit | New password saved; old fails | Auto | Pending |
| F-04 | Reset | Wrong OTP | Incorrect code | Error; attempts increment | Auto | Pending |
| F-05 | Reset | Expired OTP | Wait 5 min; submit | Expired message | Auto | Pending |
| F-06 | Reset | 5 failed attempts | Submit wrong 5x | Code locked / invalid | Auto | Pending |
| F-07 | Reset | Single-use | Correct code used once; reuse same | Rejected | Auto | Pending |
| F-08 | Reset | New request invalidates old | New forgot-request before old expires | Old code invalid | Auto | Pending |
| F-09 | Reset | Resend countdown | UI shows timer | Countdown visible | Manual | Pending |
| F-10 | Reset | Login with new password | After reset | New password works; old fails | Auto | Pending |

| ID | Area | Scenario | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| P-01 | Provider Cred | Upload allowed types | pdf/jpg/png | Accepted | Auto | Pending |
| P-02 | Provider Cred | Wrong extension | .exe | Rejected | Auto | Pending |
| P-03 | Provider Cred | Wrong content type | Fake extension | Rejected (content check) | Auto | Pending |
| P-04 | Provider Cred | Max size 5MB | >5MB | Rejected | Auto | Pending |
| P-05 | Provider Cred | Empty file | 0 bytes | Rejected | Auto | Pending |
| P-06 | Provider Cred | Path traversal | name=../../etc/file | Stored with random name only | Auto | Pending |
| P-07 | Provider Cred | Random file name | Request has original name; stored as random | Path uses random only; original only in display | Auto | Pending |
| P-08 | Provider Cred | Outside wwwroot | Check file system | Not under wwwroot | Auto | Pending |
| P-09 | Provider Cred | Not other provider's file | Log in as Provider A; try Provider B URL | 403 / redirect | Auto | Pending |
| P-10 | Provider Cred | Anonymous access | No auth | 403 / redirect to login | Auto | Pending |

| ID | Area | Scenario | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| S-01 | Security | Anti-forgery all POSTs | Submit without token | 400 / blocked | Auto | Pending |
| S-02 | Security | No user enumeration | Wrong email vs wrong pass | Same message | Auto | Pending |
| S-03 | Security | No hash/OTP in response | Check login/reset responses | Not present | Auto | Pending |
| S-04 | Security | Cookie flags | Check Set-Cookie | HttpOnly; Secure; SameSite=Lax | Auto | Pending |
| S-05 | Security | Authorization | Anonymous / Patient / Admin on /Provider | 403/redirect accordingly | Auto | Pending |

| ID | Area | Scenario | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| U-01 | UI | Responsive 360 | DevTools 360 width | Card fits; no horizontal scroll | Manual | Pending |
| U-02 | UI | Responsive 768 | DevTools 768 width | Card centered; brand hidden if 2-col used | Manual | Pending |
| U-03 | UI | Responsive 1280 | DevTools 1280 width | Full 2-col or single centered | Manual | Pending |
| U-04 | UI | Keyboard OTP | Type 1-2-3-4 | Advances; backspace goes back; paste works | Manual | Pending |
| U-05 | UI | Labels bound | Inspect label for | Matches input id | Manual | Pending |
| U-06 | UI | Focus states | Tab through | Visible outline / ring | Manual | Pending |
| U-07 | UI | Aria-live errors | Trigger error | Announced | Manual | Pending |
