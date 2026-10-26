using System;
using System.Collections.Generic;
using Zyara.Domain.Enums;

namespace Zyara.Web.Areas.Provider.ViewModels;

public class BookingDetailsViewModel
{
    public BookingItemViewModel Booking { get; set; } = new();
    public UserRole CurrentProviderRole { get; set; }
    public bool IsDoctor => CurrentProviderRole == UserRole.Doctor;
    public bool IsNurse => CurrentProviderRole == UserRole.Nurse;

    // Patient medical background preview
    public int PatientAge { get; set; } = 38;
    public string PatientGender { get; set; } = "Male";
    public string PatientNotes { get; set; } = string.Empty;

    // Medical Prescriptions (for Doctors)
    public List<PrescriptionItemViewModel> Prescriptions { get; set; } = new();

    // Clinical / Nursing Notes
    public ClinicalNotesViewModel ClinicalNotes { get; set; } = new();
}

public class PrescriptionItemViewModel
{
    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
}

public class ClinicalNotesViewModel
{
    public string BloodPressure { get; set; } = string.Empty;
    public string HeartRate { get; set; } = string.Empty;
    public string Temperature { get; set; } = string.Empty;
    public string BloodSugar { get; set; } = string.Empty;
    public string ProceduresDone { get; set; } = string.Empty;
    public string GeneralAssessment { get; set; } = string.Empty;
}

