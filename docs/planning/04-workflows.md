# 04 - Clinical & Operational Workflows

**Status:** 🟢 DRAFT  
**Last Updated:** 2026-10-21  
**Dependencies:** 03-product-scope.md  
**Feeds Into:** 05-domain-model.md, 11-backend-api-specification.md, 10-frontend-specification.md

---

## Purpose

Document complete end-to-end workflows for all clinical and operational processes in ClinicCare. Each workflow includes actors, preconditions, steps, postconditions, exceptions, and UI wireframe references.

---

## Workflow Categories

1. **Patient Management:** Registration, search, profile updates
2. **Scheduling:** Appointment booking, rescheduling, cancellation
3. **Queue Management:** Walk-in check-in, queue advancement, patient calling
4. **Clinical:** Encounter documentation, prescription writing, diagnosis coding
5. **Billing:** Invoice generation, payment recording, refunds
6. **Administrative:** User management, branch management, system configuration
7. **Reporting:** Financial reports, operational reports, clinical analytics

---

## Workflow Notation

```
WORKFLOW: Workflow Name
ACTOR: Primary user role
PRECONDITIONS: What must be true before workflow starts
STEPS: Numbered sequence of actions
POSTCONDITIONS: What is true after successful completion
EXCEPTIONS: Error cases and handling
UI: Wireframe/mockup references
```

---

## 1. Patient Management Workflows

### 1.1 Patient Registration (New Patient)

**ACTOR:** Receptionist  
**PRECONDITIONS:**
- User logged in with Receptionist or Doctor role
- Branch selected (for Receptionist)

**STEPS:**
1. Navigate to Patients → Register New Patient
2. Enter required fields:
   - Full Name (First, Middle, Last)
   - Date of Birth (date picker)
   - Gender (dropdown: Male, Female, Other)
3. Enter at least one identifier:
   - Passport Number, OR
   - National ID Number, OR
   - Other (specify type + value)
4. Enter contact information:
   - Phone Number (required)
   - Email (optional)
   - Address (Line 1, Line 2, City, State, Postal Code, Country)
5. Enter emergency contact:
   - Name
   - Relationship
   - Phone Number
6. (Optional) Enter medical information:
   - Blood Type (dropdown)
   - Allergies (free text)
   - Chronic Conditions (free text)
7. (Optional) Upload photo
8. Click "Register Patient"
9. System validates:
   - Required fields not empty
   - DOB not in future
   - Phone/Email format valid
   - Identifier not duplicate
10. System checks for potential duplicates (same name + DOB within 1 year)
11. If duplicates found:
    - Show warning: "Potential duplicate patients found"
    - Display list of similar patients
    - Options: "This is a new patient" OR "Link to existing patient"
12. If no duplicates or user confirms new patient:
    - Generate unique MRN (format: MRN-YYYYMMDD-####)
    - Save patient to database
    - Record audit log (CreatedBy, CreatedDate)
13. Show success message: "Patient registered successfully - MRN: [MRN]"
14. Navigate to Patient Profile

**POSTCONDITIONS:**
- Patient exists in database with unique MRN
- Patient searchable by name, MRN, identifier, phone
- Audit log created

**EXCEPTIONS:**
- **Duplicate Identifier:** "This [passport/national ID] is already registered to [Patient Name - MRN]"
  - Action: User must verify or use different identifier
- **Validation Errors:** Highlight invalid fields in red, show error messages
- **Network Error:** Show error message, allow retry, save draft to local storage

**UI MOCKUP:** `wireframes/patient-registration.png`

---

### 1.2 Patient Search

**ACTOR:** Receptionist, Doctor  
**PRECONDITIONS:**
- User logged in

**STEPS:**
1. Navigate to Patients → Search (or use global search bar)
2. Enter search term:
   - Name (partial match, case-insensitive)
   - MRN (exact match)
   - Phone number
   - Passport/National ID
3. System searches as-you-type (debounced 300ms)
4. Display results in table:
   - Photo (thumbnail)
   - Name
   - MRN
   - DOB (Age)
   - Gender
   - Phone
   - Last Visit Date
5. Click result row → Navigate to Patient Profile

**POSTCONDITIONS:**
- Matching patients displayed
- User can select patient

**EXCEPTIONS:**
- **No Results:** "No patients found matching '[search term]'"
  - Suggestion: "Check spelling or try different search term"
- **Too Many Results:** "Showing first 50 results, please refine search"

**UI MOCKUP:** `wireframes/patient-search.png`

---

### 1.3 Patient Profile View/Edit

**ACTOR:** Receptionist, Doctor  
**PRECONDITIONS:**
- Patient selected from search

**STEPS:**
1. System loads patient profile:
   - Demographics panel (left)
   - Tabs (right): Overview, Encounters, Appointments, Billing
2. **Overview Tab:**
   - Contact information
   - Identifiers
   - Medical summary (allergies, chronic conditions, blood type)
   - Recent encounters (last 5)
   - Outstanding balance
3. Click "Edit" button
4. Fields become editable (except MRN, CreatedDate)
5. Make changes
6. Click "Save Changes"
7. System validates changes
8. Update database with audit log (ModifiedBy, ModifiedDate)
9. Show success message: "Patient updated successfully"
10. Return to view mode

**POSTCONDITIONS:**
- Changes saved
- Audit log updated
- Profile reflects new data

**EXCEPTIONS:**
- **Concurrent Edit:** "This patient was modified by [User] at [Time]. Please refresh and try again."
- **Validation Errors:** Same as registration

**UI MOCKUP:** `wireframes/patient-profile.png`

---

## 2. Scheduling Workflows

### 2.1 Book Appointment (Pre-Scheduled)

**ACTOR:** Receptionist  
**PRECONDITIONS:**
- Patient exists in system
- Branch selected

**STEPS:**
1. From Patient Profile → Click "Book Appointment"
   OR Navigate to Schedule → Click "New Appointment"
2. If from Schedule, search and select patient
3. Enter appointment details:
   - Date (date picker, future dates only)
   - Time (dropdown, 15-min intervals, 8 AM - 8 PM)
   - Branch (dropdown, defaults to user's branch)
   - Reason for Visit (free text, optional)
4. System checks for conflicts (same patient, same time at same branch)
5. If conflict: Show warning "Appointment conflict detected"
   - Options: "Overbook" OR "Choose different time"
6. Click "Book Appointment"
7. System saves appointment with status = Scheduled
8. Record audit log
9. Show success message: "Appointment booked for [Patient Name] on [Date] at [Time]"
10. Optional: Send SMS reminder (if phone number valid)

**POSTCONDITIONS:**
- Appointment exists in database
- Appointment visible in schedule view
- Patient can see appointment in their profile

**EXCEPTIONS:**
- **Past Date:** "Cannot book appointment in the past"
- **Branch Closed:** "Selected branch is closed on [Date]"
- **Patient Already Has Appointment:** Warning, allow proceed

**UI MOCKUP:** `wireframes/book-appointment.png`

---

### 2.2 Check-In Appointment

**ACTOR:** Receptionist  
**PRECONDITIONS:**
- Appointment exists with status = Scheduled or Confirmed
- Appointment date is today

**STEPS:**
1. Navigate to Schedule → Today's View
2. Find appointment in list
3. Click "Check In" button
4. Confirm patient arrival
5. System changes appointment status = CheckedIn
6. System adds patient to queue automatically:
   - QueueNumber assigned (next available)
   - QueueStatus = Waiting
   - QueuedAt = Current timestamp
7. Show success message: "Patient checked in - Queue #[Number]"
8. Patient notified to wait

**POSTCONDITIONS:**
- Appointment status = CheckedIn
- Patient in queue with assigned number
- Doctor sees patient in queue

**EXCEPTIONS:**
- **Appointment Not Found:** "No appointment found for this patient today"
- **Already Checked In:** "Patient already checked in at [Time]"

**UI MOCKUP:** `wireframes/checkin.png`

---

### 2.3 Reschedule Appointment

**ACTOR:** Receptionist  
**PRECONDITIONS:**
- Appointment exists with status ≠ Completed, Cancelled

**STEPS:**
1. From Patient Profile → Appointments Tab → Select appointment
   OR From Schedule → Select appointment
2. Click "Reschedule" button
3. Select new date/time
4. System checks for conflicts
5. Click "Confirm Reschedule"
6. System updates appointment:
   - Date, Time updated
   - Status remains unchanged
   - Record audit log with reason "Rescheduled from [Old Date/Time]"
7. Show success message: "Appointment rescheduled to [New Date] at [New Time]"
8. Optional: Send SMS notification

**POSTCONDITIONS:**
- Appointment updated with new date/time
- Audit log records old and new values

**EXCEPTIONS:**
- **Past Date:** "Cannot reschedule to past date"
- **Same Date/Time:** "New date/time is the same as current"

**UI MOCKUP:** `wireframes/reschedule.png`

---

### 2.4 Cancel Appointment

**ACTOR:** Receptionist, Patient (future)  
**PRECONDITIONS:**
- Appointment exists with status ≠ Completed, Cancelled

**STEPS:**
1. From appointment detail view → Click "Cancel Appointment"
2. Prompt: "Are you sure you want to cancel this appointment?"
3. Enter cancellation reason (dropdown + optional notes):
   - Patient requested
   - Doctor unavailable
   - Emergency
   - Other (specify)
4. Click "Confirm Cancellation"
5. System updates appointment status = Cancelled
6. Record audit log with reason
7. Show success message: "Appointment cancelled"
8. Optional: Send SMS notification

**POSTCONDITIONS:**
- Appointment status = Cancelled
- Appointment still visible in history but greyed out
- Slot freed for rebooking

**EXCEPTIONS:**
- **Appointment Already Completed:** "Cannot cancel completed appointment"
- **Appointment Today (Already Checked In):** Warning: "Patient already checked in. Mark as No-Show instead?"

**UI MOCKUP:** `wireframes/cancel-appointment.png`

---

## 3. Queue Management Workflows

### 3.1 Add Walk-In Patient to Queue

**ACTOR:** Receptionist  
**PRECONDITIONS:**
- Patient exists in system (or register first)
- Branch selected

**STEPS:**
1. Search and select patient
2. From Patient Profile → Click "Add to Queue"
   OR Navigate to Queue → Click "Add Walk-In"
3. Confirm branch (defaults to user's branch)
4. Enter reason for visit (optional)
5. Click "Add to Queue"
6. System assigns queue number (next available for today at this branch)
7. System creates QueueEntry:
   - QueueNumber assigned
   - QueueStatus = Waiting
   - QueuedAt = Current timestamp
   - Branch = User's branch
8. Show success message: "Patient added to queue - Queue #[Number]"
9. Display queue number ticket (printable)

**POSTCONDITIONS:**
- Patient in queue with assigned number
- Queue number visible on queue display (waiting room)
- Doctor sees patient in queue list

**EXCEPTIONS:**
- **Patient Already in Queue:** "Patient is already in queue (Queue #[Number]). Remove from queue first."
- **Queue Full:** Warning if >50 patients in queue

**UI MOCKUP:** `wireframes/add-to-queue.png`

---

### 3.2 Call Next Patient (Doctor)

**ACTOR:** Doctor  
**PRECONDITIONS:**
- At least one patient in queue with status = Waiting

**STEPS:**
1. Navigate to Queue View (doctor's dashboard)
2. System displays queue sorted by QueueNumber ascending
3. Highlight next patient (first with status = Waiting)
4. Click "Call Next Patient" button
5. System updates QueueEntry:
   - QueueStatus = InProgress
   - CalledAt = Current timestamp
6. System displays patient details:
   - Name, Age, Gender
   - Reason for visit
   - Recent encounters (last 3)
   - Allergies, chronic conditions
7. Queue display (waiting room) shows: "Now Serving: Queue #[Number]"
8. Audio announcement (optional): "Queue number [Number], please proceed to doctor's room"
9. Doctor clicks "Start Encounter" → Navigate to Encounter form

**POSTCONDITIONS:**
- Queue entry status = InProgress
- Patient details visible to doctor
- Ready to start encounter

**EXCEPTIONS:**
- **No Patients Waiting:** "Queue is empty"
- **Patient Left:** Doctor can mark as "Patient Did Not Show" → QueueStatus = Cancelled

**UI MOCKUP:** `wireframes/call-next-patient.png`

---

### 3.3 Complete Queue Entry

**ACTOR:** System (automatic after encounter saved)  
**PRECONDITIONS:**
- QueueEntry status = InProgress
- Encounter saved successfully

**STEPS:**
1. Encounter save triggers queue completion
2. System updates QueueEntry:
   - QueueStatus = Completed
   - CompletedAt = Current timestamp
3. Queue entry removed from active queue list
4. Recorded in history (searchable)

**POSTCONDITIONS:**
- Queue entry marked complete
- Next patient can be called

**EXCEPTIONS:**
- None (automatic process)

---

## 4. Clinical Workflows

### 4.1 Start Encounter (From Queue)

**ACTOR:** Doctor  
**PRECONDITIONS:**
- Patient called from queue (QueueStatus = InProgress)
  OR Selected from patient profile

**STEPS:**
1. Click "Start Encounter" button
2. System creates new Encounter:
   - Patient ID
   - Doctor ID (current user)
   - Branch ID (current queue entry branch OR patient's last branch)
   - EncounterDate = Current date/time
   - EncounterType = OutPatient (default)
   - Status = InProgress
3. Navigate to Encounter form with tabs:
   - Vitals
   - SOAP Notes
   - Diagnosis
   - Treatment Plan
   - Prescriptions
   - Attachments

**POSTCONDITIONS:**
- Encounter created with status = InProgress
- Encounter form open for doctor to complete

**EXCEPTIONS:**
- **Encounter Already Started:** "An encounter is already in progress for this patient"

**UI MOCKUP:** `wireframes/start-encounter.png`

---

### 4.2 Record Vitals

**ACTOR:** Doctor, Nurse (future role)  
**PRECONDITIONS:**
- Encounter started (status = InProgress)

**STEPS:**
1. Navigate to Vitals tab in encounter form
2. Enter measurements:
   - Height (cm or ft/in, converts to cm)
   - Weight (kg or lbs, converts to kg)
   - Blood Pressure (Systolic/Diastolic in mmHg)
   - Temperature (°C or °F, converts to °C)
   - Pulse (bpm)
   - SpO2 (%)
   - Respiratory Rate (breaths/min)
3. System calculates derived values:
   - BMI = Weight(kg) / (Height(m))²
   - BMI Category (Underweight, Normal, Overweight, Obese)
4. System flags abnormal values:
   - BP > 140/90 → "Hypertension risk"
   - Temp > 38°C → "Fever"
   - SpO2 < 95% → "Low oxygen"
5. Click "Save Vitals"
6. Vitals saved to Encounter
7. Show success message: "Vitals recorded"

**POSTCONDITIONS:**
- Vitals recorded in encounter
- Abnormal flags visible
- BMI calculated

**EXCEPTIONS:**
- **Out-of-Range Values:** Warning "Value seems unusual, please verify"
  - Example: Height > 250cm, Weight > 300kg
- **Missing Required Fields:** Cannot proceed to next tab until height + weight entered

**UI MOCKUP:** `wireframes/record-vitals.png`

---

### 4.3 Document SOAP Notes

**ACTOR:** Doctor  
**PRECONDITIONS:**
- Encounter started (status = InProgress)

**STEPS:**
1. Navigate to SOAP Notes tab
2. Enter notes in four sections (rich text editor):
   - **Subjective:** Patient's complaints, symptoms, history
     - "Patient reports headache for 3 days, throbbing pain in temples"
   - **Objective:** Physical examination findings, vitals
     - "BP 130/85, Temp 37.2°C, No neck stiffness, pupils equal and reactive"
   - **Assessment:** Diagnosis, differential diagnosis
     - "Likely tension headache, rule out migraine"
   - **Plan:** Treatment plan, follow-up
     - "Prescribe ibuprofen 400mg TID x 5 days, advise rest, follow-up if worsens"
3. Rich text features:
   - Bold, italic, underline
   - Bullet points, numbered lists
   - Insert template snippets (common phrases)
4. Auto-save every 30 seconds (draft)
5. Click "Save SOAP Notes"
6. Notes saved to Encounter

**POSTCONDITIONS:**
- SOAP notes recorded
- Visible in encounter history

**EXCEPTIONS:**
- **Auto-save Failed:** Warning banner, retry on next change
- **Concurrent Edit:** Not applicable (one doctor, one encounter)

**UI MOCKUP:** `wireframes/soap-notes.png`

---

### 4.4 Add Diagnosis (ICD-10)

**ACTOR:** Doctor  
**PRECONDITIONS:**
- Encounter started

**STEPS:**
1. Navigate to Diagnosis tab
2. Click "Add Diagnosis"
3. Search for ICD-10 code or description:
   - Type "headache" → autocomplete suggests:
     - R51.9 Headache, unspecified
     - G43.909 Migraine, unspecified, not intractable, without status migrainosus
     - G44.209 Tension-type headache, unspecified, not intractable
4. Select from list OR enter custom description if code not found
5. Specify:
   - Type: Primary, Secondary, Comorbidity
   - Status: Active, Resolved
6. Click "Add"
7. Diagnosis added to list (can add multiple)
8. Click "Save Diagnoses"

**POSTCONDITIONS:**
- Diagnosis(es) recorded in encounter
- ICD-10 code stored for reporting

**EXCEPTIONS:**
- **Code Not Found:** Allow free text entry, flag for review
- **Duplicate Diagnosis:** Warning "This diagnosis is already added"

**UI MOCKUP:** `wireframes/add-diagnosis.png`

---

### 4.5 Write Prescription

**ACTOR:** Doctor  
**PRECONDITIONS:**
- Encounter started

**STEPS:**
1. Navigate to Prescriptions tab
2. Click "Add Prescription"
3. Search medication:
   - Type "amoxicillin" → autocomplete suggests:
     - Amoxicillin 250mg Capsule
     - Amoxicillin 500mg Capsule
     - Amoxicillin 125mg/5ml Suspension
4. Select medication from list
5. Enter prescription details:
   - Dosage: 500mg
   - Frequency: 3 times daily (dropdown: 1x, 2x, 3x, 4x, every 4h, every 6h, as needed)
   - Duration: 7 days
   - Route: Oral (dropdown: Oral, Topical, Injectable, etc.)
   - Instructions: "Take with food, complete full course"
6. Quantity calculated automatically (Dosage × Frequency × Duration)
7. Click "Add"
8. Prescription added to list (can add multiple)
9. Click "Save Prescriptions"
10. Optional: Click "Print Prescription" → Generate PDF with:
    - Clinic letterhead
    - Doctor name, license number, signature
    - Patient name, age, date
    - Prescription details (Rx symbol, medication, dosage, instructions)

**POSTCONDITIONS:**
- Prescription(s) recorded in encounter
- Prescription printable as PDF

**EXCEPTIONS:**
- **Medication Not Found:** Allow free text entry, flag for review
- **Drug Interaction Warning:** (Future) Show alert if multiple prescriptions interact

**UI MOCKUP:** `wireframes/write-prescription.png`

---

### 4.6 Complete Encounter

**ACTOR:** Doctor  
**PRECONDITIONS:**
- Encounter started
- At minimum: Vitals OR SOAP notes OR Diagnosis entered

**STEPS:**
1. Review all tabs (Vitals, SOAP, Diagnosis, Prescriptions)
2. Click "Complete Encounter" button
3. Confirm: "Are you sure you want to complete this encounter? It will be locked for editing."
4. Click "Confirm"
5. System validates minimum data present
6. System updates Encounter:
   - Status = Completed
   - CompletedAt = Current timestamp
7. System updates QueueEntry (if from queue):
   - QueueStatus = Completed
   - CompletedAt = Current timestamp
8. System generates invoice automatically (if billing enabled):
   - Add consultation service
   - Add prescribed medications (if dispensed in-house)
9. Show success message: "Encounter completed successfully"
10. Navigate to Invoice review (optional) OR back to Queue

**POSTCONDITIONS:**
- Encounter status = Completed, locked from editing
- Queue entry completed (if applicable)
- Invoice generated (draft status)
- Doctor can call next patient

**EXCEPTIONS:**
- **Minimum Data Not Met:** "Please enter at least vitals or SOAP notes before completing"
- **Prescription Not Saved:** Warning "Unsaved prescription detected. Save or discard?"

**UI MOCKUP:** `wireframes/complete-encounter.png`

---

### 4.7 Amend Encounter (Add Addendum)

**ACTOR:** Doctor  
**PRECONDITIONS:**
- Encounter status = Completed
- User is the encounter's doctor OR has admin role

**STEPS:**
1. Open completed encounter from patient history
2. Encounter displayed as read-only
3. Click "Add Addendum" button
4. Enter addendum text (rich text editor):
   - "Addendum [Date/Time]: Patient called to report improvement after 2 days. No further action needed."
5. Click "Save Addendum"
6. Addendum saved with timestamp and author
7. Original encounter data unchanged

**POSTCONDITIONS:**
- Addendum recorded in audit log
- Visible in encounter history as separate entry below original notes

**EXCEPTIONS:**
- **Cannot Edit Original:** By design, original data immutable

**UI MOCKUP:** `wireframes/add-addendum.png`

---

## 5. Billing Workflows

### 5.1 Generate Invoice (Auto from Encounter)

**ACTOR:** System (automatic) OR Receptionist (manual review)  
**PRECONDITIONS:**
- Encounter completed

**STEPS:**
1. System triggered by encounter completion
2. System creates Invoice:
   - Patient ID
   - EncounterID (if from encounter)
   - InvoiceDate = Current date
   - DueDate = Current date (immediate payment)
   - Status = Draft
3. System adds line items:
   - Consultation service (from EncounterType → Service mapping)
     - Example: "General Consultation" - 100 USD
   - Prescribed medications (if dispensed in-house)
     - Example: "Amoxicillin 500mg x 21 caps" - 15 USD
   - Additional procedures (if any)
4. System calculates:
   - Subtotal = Sum of line items
   - Tax = Subtotal × TaxRate (configurable, default 0%)
   - Discount = 0 (manual adjustment)
   - Total = Subtotal + Tax - Discount
5. Invoice saved in database
6. Receptionist notified (if enabled): "Invoice ready for review"

**POSTCONDITIONS:**
- Invoice created with status = Draft
- Ready for receptionist to review/issue

**EXCEPTIONS:**
- **Service Not Configured:** Warning "No service defined for [EncounterType]. Please add manually."
- **Price Missing:** Use default price = 0, flag for review

**UI MOCKUP:** `wireframes/auto-invoice.png`

---

### 5.2 Review and Issue Invoice

**ACTOR:** Receptionist  
**PRECONDITIONS:**
- Invoice exists with status = Draft

**STEPS:**
1. Navigate to Billing → Pending Invoices
2. Select invoice from list
3. Review invoice details:
   - Patient info
   - Line items (Service, Quantity, Price)
   - Subtotal, Tax, Discount, Total
4. Edit if needed:
   - Add/remove line items
   - Adjust quantities
   - Apply discount (percentage or fixed amount)
   - Change prices (admin role required)
5. Click "Issue Invoice"
6. System updates Invoice:
   - Status = Issued
   - IssuedAt = Current timestamp
   - IssuedBy = Current user
7. Show success message: "Invoice issued - Total: [Amount]"
8. Optional: Print invoice (PDF)
9. Navigate to Payment recording OR back to patient profile

**POSTCONDITIONS:**
- Invoice status = Issued
- Patient owes payment
- Invoice visible in patient's billing history

**EXCEPTIONS:**
- **Total = 0:** Warning "Invoice total is zero. Issue anyway?"
- **Already Paid:** Cannot issue if status = Paid

**UI MOCKUP:** `wireframes/issue-invoice.png`

---

### 5.3 Record Payment

**ACTOR:** Receptionist  
**PRECONDITIONS:**
- Invoice exists with status = Issued OR PartiallyPaid

**STEPS:**
1. From invoice detail view → Click "Record Payment"
2. Enter payment details:
   - Amount (default: remaining balance)
   - Payment Method (dropdown: Cash, Card, Bank Transfer, Insurance, Other)
   - Reference (optional: transaction ID, check number, insurance claim number)
   - Payment Date (default: today)
3. System validates:
   - Amount > 0
   - Amount ≤ Remaining Balance (allow overpayment with warning)
4. Click "Record Payment"
5. System creates Payment record:
   - InvoiceID
   - Amount
   - PaymentMethod
   - Reference
   - PaymentDate
   - RecordedBy = Current user
6. System updates Invoice:
   - TotalPaid = Sum of all payments
   - RemainingBalance = Total - TotalPaid
   - Status = Paid (if RemainingBalance = 0) OR PartiallyPaid (if RemainingBalance > 0)
7. Record audit log
8. Show success message: "Payment recorded - Amount: [Amount], Balance: [Remaining]"
9. Optional: Print receipt (PDF)

**POSTCONDITIONS:**
- Payment recorded
- Invoice balance updated
- Receipt printable

**EXCEPTIONS:**
- **Overpayment:** Warning "Payment exceeds balance. Record as [Balance] + [Overpayment] refund?"
- **Already Fully Paid:** "Invoice is already fully paid. Record refund instead?"

**UI MOCKUP:** `wireframes/record-payment.png`

---

### 5.4 Record Refund

**ACTOR:** Receptionist (Admin approval required for refunds > threshold)  
**PRECONDITIONS:**
- Invoice exists with TotalPaid > 0

**STEPS:**
1. From invoice detail view → Click "Record Refund"
2. Enter refund details:
   - Amount (max: TotalPaid)
   - Reason (dropdown: Overpayment, Cancelled Service, Billing Error, Other)
   - Refund Method (same as payment methods)
   - Reference (optional)
   - Refund Date (default: today)
3. If refund > threshold (e.g., 100 USD):
   - Require admin approval
   - Prompt: "Refund requires admin approval. Proceed?"
   - Admin notified (if enabled)
4. Click "Record Refund"
5. System creates Payment record with negative amount:
   - Amount = -RefundAmount
6. System updates Invoice balance
7. Show success message: "Refund recorded - Amount: [Amount]"

**POSTCONDITIONS:**
- Refund recorded as negative payment
- Invoice balance adjusted
- Audit log includes refund reason

**EXCEPTIONS:**
- **Refund > TotalPaid:** "Cannot refund more than total paid"
- **Approval Denied:** "Refund not approved by admin"

**UI MOCKUP:** `wireframes/record-refund.png`

---

## 6. Administrative Workflows

### 6.1 Create Branch

**ACTOR:** Admin  
**PRECONDITIONS:**
- User has Admin role

**STEPS:**
1. Navigate to Administration → Branches
2. Click "Add Branch"
3. Enter branch details:
   - Name (required)
   - Address (Line 1, Line 2, City, State, Postal Code, Country)
   - Phone
   - Email
   - IsActive (default: true)
4. Click "Save Branch"
5. System validates:
   - Name not empty, unique
   - Phone/Email format valid
6. System saves branch
7. Show success message: "Branch created successfully"

**POSTCONDITIONS:**
- Branch exists in database
- Available in branch dropdown for appointments, queue
- Receptionists can be assigned to this branch

**EXCEPTIONS:**
- **Duplicate Name:** "Branch with this name already exists"

**UI MOCKUP:** `wireframes/create-branch.png`

---

### 6.2 Create User

**ACTOR:** Admin  
**PRECONDITIONS:**
- User has Admin role

**STEPS:**
1. Navigate to Administration → Users
2. Click "Add User"
3. Enter user details:
   - Username (required, unique)
   - Email (required, unique)
   - Full Name
   - Role (dropdown: Doctor, Receptionist, Admin)
   - Password (temporary, user changes on first login)
   - Assigned Branches (multi-select, required for Receptionist)
4. Click "Create User"
5. System validates:
   - Username, email unique
   - Password meets policy (min 8 chars, 1 uppercase, 1 number, 1 special)
   - At least one branch selected for Receptionist
6. System creates user with ASP.NET Identity
7. Assign role
8. Show success message: "User created - Username: [Username]"
9. Optional: Send email with temporary password

**POSTCONDITIONS:**
- User can log in
- Role permissions applied
- Branch assignments active

**EXCEPTIONS:**
- **Duplicate Username/Email:** "Username/Email already exists"
- **Weak Password:** "Password does not meet policy requirements"

**UI MOCKUP:** `wireframes/create-user.png`

---

### 6.3 Configure Service Catalog

**ACTOR:** Admin  
**PRECONDITIONS:**
- User has Admin role

**STEPS:**
1. Navigate to Administration → Services
2. Click "Add Service"
3. Enter service details:
   - Code (required, unique, e.g., "CONS-GEN")
   - Name (required, e.g., "General Consultation")
   - Description (optional)
   - Category (dropdown: Consultation, Procedure, Lab, Imaging, Pharmacy, Other)
   - Default Price (required)
   - IsActive (default: true)
4. Click "Save Service"
5. System validates:
   - Code unique
   - Price ≥ 0
6. System saves service
7. Show success message: "Service added successfully"

**POSTCONDITIONS:**
- Service available in invoice line item dropdown
- Service can be mapped to encounter types

**EXCEPTIONS:**
- **Duplicate Code:** "Service code already exists"
- **Negative Price:** "Price cannot be negative"

**UI MOCKUP:** `wireframes/configure-services.png`

---

## 7. Reporting Workflows

### 7.1 Generate Daily Revenue Report

**ACTOR:** Admin, Receptionist  
**PRECONDITIONS:**
- User has appropriate role

**STEPS:**
1. Navigate to Reports → Financial → Daily Revenue
2. Select date range (default: today)
3. Select branch (default: all branches OR user's branch for Receptionist)
4. Click "Generate Report"
5. System queries payments in date range
6. System aggregates:
   - Total Revenue by Payment Method (Cash, Card, etc.)
   - Total Revenue by Service Category
   - Number of Invoices Issued
   - Number of Payments Recorded
   - Outstanding Balance (all unpaid invoices)
7. Display report on screen:
   - Summary cards (Total Revenue, Total Cash, Total Card)
   - Charts (pie chart: revenue by method, bar chart: revenue by category)
   - Table: Invoice details (Invoice#, Patient, Amount, Payment Method)
8. Options:
   - Export to Excel (XLSX)
   - Export to PDF
   - Print

**POSTCONDITIONS:**
- Report generated and displayed
- Exportable to Excel/PDF

**EXCEPTIONS:**
- **No Data:** "No payments found in selected date range"
- **Large Dataset:** Warning if >10,000 records, suggest narrowing range

**UI MOCKUP:** `wireframes/daily-revenue-report.png`

---

### 7.2 Generate Operational Report

**ACTOR:** Admin, Doctor  
**PRECONDITIONS:**
- User has appropriate role

**STEPS:**
1. Navigate to Reports → Operational → Patient Activity
2. Select date range (default: last 7 days)
3. Select branch (default: all)
4. Click "Generate Report"
5. System queries encounters, appointments, queue entries in date range
6. System calculates:
   - Total Patients Seen (unique patients with encounters)
   - Total Appointments (Scheduled, Completed, NoShow, Cancelled)
   - Total Walk-Ins (queue entries without appointment)
   - Average Wait Time (QueuedAt → CalledAt)
   - No-Show Rate (NoShow / Total Scheduled × 100)
   - Top 10 Diagnoses (ICD-10 codes)
7. Display report with charts:
   - Line chart: Patients per day
   - Pie chart: Appointment status breakdown
   - Bar chart: Top diagnoses
8. Export options (Excel, PDF)

**POSTCONDITIONS:**
- Report generated and displayed
- Exportable

**EXCEPTIONS:**
- **No Data:** "No patient activity in selected date range"

**UI MOCKUP:** `wireframes/operational-report.png`

---

## Workflow State Machines

### Appointment Status Lifecycle

```
Scheduled → Confirmed → CheckedIn → Completed
   ↓           ↓           ↓
Cancelled    Cancelled   NoShow
```

### Queue Entry Status Lifecycle

```
Waiting → InProgress → Completed
   ↓
Cancelled (Patient Left)
```

### Encounter Status Lifecycle

```
InProgress → Completed
   ↓
Cancelled (rare, e.g., patient left before completion)
```

### Invoice Status Lifecycle

```
Draft → Issued → PartiallyPaid → Paid
   ↓       ↓
Cancelled Cancelled
```

---

## Cross-References

- **Entity Definitions:** See `05-domain-model.md`
- **API Endpoints:** See `11-backend-api-specification.md`
- **UI Components:** See `10-frontend-specification.md`
- **Test Cases:** See `12-testing-strategy.md`

---

## Notes

- **Workflow Versioning:** If workflows change post-launch, update this document and create migration guide
- **User Training:** These workflows form the basis for user documentation and training materials
- **Automation Opportunities:** Identify repetitive steps for future automation (e.g., auto-send SMS reminders)

---

*This document is the authoritative source for how ClinicCare should function. Implementation must match these workflows exactly unless changes are explicitly approved and documented.*
