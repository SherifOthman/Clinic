# 02 - Reference System Research

**Status:** 🟡 IN PROGRESS  
**Last Updated:** 2026-10-21  
**Dependencies:** 01-repository-audit.md  
**Feeds Into:** 03-product-scope.md, 04-workflows.md, 05-domain-model.md

---

## Purpose

Comparative research of open-source healthcare systems to identify best practices, avoid common pitfalls, and make evidence-based design decisions for ClinicCare. This document synthesizes findings from multiple reference implementations beyond CARE.

---

## Research Sources

### Primary Reference: CARE v0.1.15
**Status:** ✅ COMPLETE  
**Location:** `d:\Clinic\reference\care_clinic\`, `d:\Clinic\reference\care\`, `d:\Clinic\reference\care_fe\`  
**Analysis:** See `01-repository-audit.md` for comprehensive 40+ entity domain model

**Key Findings:**
- **Architecture:** Django REST + React, FHIR-aligned entities, multi-tenant with Organization hierarchy
- **Strengths:** Comprehensive scheduling (7 entities), robust billing (5 entities), strong audit trails
- **Weaknesses:** High complexity, hospital-focused, overkill for small clinic
- **Adoption:** Selective - simplify scheduling to 2 entities, billing to 4 entities, flatten Organization to Branch

### Secondary References

### OpenEMR
**Status:** ✅ RESEARCHED (online documentation)  
**Repository:** https://github.com/openemr/openemr.git  
**Technology:** PHP + MySQL  
**Target:** Hospital/multi-practice EHR  
**Documentation:** https://www.open-emr.org/wiki/

**Research Method:** Online documentation and database schema documentation due to large repository size.

**Key Findings:**

**Payment Session Management (`ar_session` table):**
- Tracks payment sessions with open/closed status
- Fields: `session_id`, `payer_id`, `user_id`, `closed` (0=open, 1=closed)
- Payment details: `reference` (check/EOB number), `check_date`, `deposit_date`
- Amount tracking: `pay_total`, `global_amount`
- Payment type and method from configurable lists
- Location tracking: optional `FacilityLocation` FK
- Audit: `created_time`, `modified_time`

**Key Insight:** OpenEMR uses session-based payment tracking, similar to shift/drawer concept. Session remains open while cashier is working, then closed at end of shift. This is the **ar_session** pattern.

**Payment Activity (`ar_activity` table):**
- Links payments to specific encounters/invoices
- Payer type code (0=patient, 1=primary insurance, 2=secondary, etc.)
- Supports partial payments and payment allocation

**Billing Features:**
- Fee sheets for service capture
- Batch payment processing
- EOB (Explanation of Benefits) posting
- Patient statements and invoicing
- Financial reports by payment mode
- ICD-10/CPT code integration

**Adopted Patterns:**
- ✅ Payment sessions (open/closed) → CashDrawer entity
- ✅ Session-based payment grouping
- ✅ Payment method configuration via lists
- ✅ Deposit date tracking
- ❌ Insurance-focused (too complex for cash-only clinic)

**Evidence:** [OpenEMR ar_session schema](https://www.open-emr.org/documentation/8.4.1/b10/tables/ar_session.html), [Accounting & Receivables Wiki](https://www.open-emr.org/wiki/index.php/Accounting_&_Receivables)

#### OpenMRS
**Status:** ✅ RESEARCHED (GitHub repository + documentation)  
**Repository:** https://github.com/openmrs/openmrs-module-cashier, https://github.com/openmrs/openmrs-module-billing  
**Technology:** Java + Hibernate + MySQL  
**Target:** Developing countries, primary care, HIV/TB focus

**Research Method:** GitHub repository analysis and official Atlassian wiki documentation.

**Key Findings:**

**Cashier Module (openmrs-module-cashier / openmrs-module-billing):**

**Cash Point Management:**
- **CashPoint** entity: Represents physical cashier stations/locations
- Fields: `name`, `description`, `location` (FK to OpenMRS Location)
- Multiple cash points per facility supported
- Location-based transaction tracking

**Cashier Timesheet/Shift Management:**
- **Timesheet** entity: Tracks cashier work shifts
- Clock in/out functionality
- Fields: `cashier` (user), `cashPoint`, `clockIn`, `clockOut`, `status`
- Configurable validation: require active timesheet before creating bills
- Auto-close timesheets at end of day
- Shift-based financial reporting

**Bill Management:**
- **Bill** entity with line items
- Status workflow: Pending → Paid / Exempted / Refunded
- Bill adjustments with full audit trail
- Discounts (line-item and whole-bill)
- Refunds with approval workflow (request → approve → complete)

**Payment Processing:**
- **Payment** entity linked to Bill and Timesheet
- Payment modes: Cash, Insurance, Mobile Money, Credit/Debit, Custom
- Payment attributes (e.g., phone number for mobile money, reference for bank transfer)
- Partial payment support
- Automatic change calculation
- Per-payment cashier attribution

**Receipt Generation:**
- Configurable Jasper Report templates
- Sequential or custom receipt numbering
- Reprint capabilities with privilege controls

**Financial Reports:**
- Shift summary (per cashier, per timesheet)
- Daily shift summaries (all cashiers for a day)
- Department collections
- Department revenue
- Payments by payment mode

**Item & Pricing:**
- **BillableService** entity (services a facility can charge for)
- **ItemPrice** entity (prices by payment mode and service/stock item)
- Integration with OpenMRS Stock Management module
- Price history tracking

**Billing Exemptions:**
- Rule-based exemptions (JavaScript evaluation)
- Age-based and order-based criteria
- Per-concept exemption configuration

**Automatic Billing from Orders:**
- Test orders → billed via BillableService (concept-based pricing)
- Drug orders → billed via StockItem (stock management integration)
- Extensible strategy pattern for custom order types

**Adopted Patterns:**
- ✅ CashPoint entity → Map to Branch-specific cash drawers
- ✅ Cashier Timesheet/Shift → CashDrawer open/close workflow
- ✅ Bill adjustments and audit trails
- ✅ Refund request/approval workflow
- ✅ Shift-based reporting
- ✅ Receipt numbering with sequence blocks
- ❌ Stock Management integration (defer to M6)
- ❌ Order-based auto-billing (not applicable to M1-M4)

**Key Insight:** OpenMRS provides the **most comprehensive cash management** of all systems researched. The Timesheet model is exactly what ClinicCare needs for shift/drawer reconciliation.

**Evidence:** [GitHub openmrs-module-billing](https://github.com/openmrs/openmrs-module-billing/blob/main/README.md), [OpenHMIS Cashier Module Wiki](https://openmrs.atlassian.net/wiki/spaces/docs/pages/25462620/OpenHMIS+Cashier+Module)

#### GNU Health
**Status:** ✅ RESEARCHED (documentation)  
**Repository:** https://www.gnuhealth.org/ (Savannah)  
**Technology:** Python + Tryton ERP  
**Target:** Primary care, public health, developing countries

**Research Method:** Official GNU Health documentation and Tryton module documentation.

**Key Findings:**

**Integrated Tryton ERP:**
- GNU Health built on Tryton business platform
- Uses Tryton's standard accounting modules:
  - `trytond-account` - Chart of accounts, general ledger
  - `trytond-account-invoice` - Invoice management
  - `trytond-account-invoice-line-standalone` - Standalone line items
  - `trytond-project-invoice` - Service-based invoicing

**Invoice Management:**
- Invoice types: Invoice, Supplier Invoice, Credit Note, Supplier Credit Note
- Status workflow: Draft → Validated → Posted → Paid → Canceled
- Line items with products/services, quantities, prices
- Taxation support

**Products & Services:**
- Product entity as base for invoicing
- List price, cost price, UOM (unit of measure)
- Medication products with dose units (tablet, capsule, mL, vial)
- Services linkable to laboratory requests, procedures

**Financial Management:**
- Full double-entry accounting (Tryton standard)
- Chart of accounts integration
- Payment terms configuration
- Supplier and customer management

**Limitations:**
- No explicit "cash drawer" or "shift" entity in documentation
- Relies on Tryton's general accounting (not clinic-specific)
- Financial features are ERP-grade (overkill for small clinic)

**Adopted Patterns:**
- ✅ Product-based pricing (Service entity)
- ✅ Invoice status workflow
- ✅ Credit Note for refunds
- ❌ Full double-entry accounting (too complex)
- ❌ No cash drawer/shift management

**Assessment:** GNU Health is powerful but generic. OpenMRS Cashier module provides more clinic-specific cash management.

**Evidence:** [GNU Health Products Management](https://docs.gnuhealth.org/his/userguide/healthcenter/products.html), [GNU Health Laboratory Billing](https://docs.gnuhealth.org/his/userguide/modules/laboratory.html), [Tryton Account Invoice](https://pypi.org/project/trytond-account-invoice/)

#### TPT Doctor
**Status:** ❌ NOT RELEVANT  
**Research Result:** Web search did not locate a specific open-source EMR/clinic system called "TPT Doctor"

**Search Performed:** Multiple queries for "TPT Doctor EMR", "TPT Doctor clinic management", "TPT Doctor healthcare software"

**Findings:** No authoritative repository or documentation found. May be:
- Commercial/proprietary system (not open source)
- Regional/localized system with limited documentation
- Deprecated or renamed project
- Misidentified in original directive

**Decision:** Skip TPT Doctor research. CARE, OpenEMR, OpenMRS, and GNU Health provide comprehensive coverage of open-source healthcare system patterns.

---

## Comparative Analysis Framework

### Evaluation Dimensions

For each reference system, assess:

1. **Patient Management**
   - Demographics storage (concrete vs JSON)
   - Identifier schemes (passport, national ID, medical record number)
   - Contact information (addresses, phones, emergency contacts)
   - Multi-branch access patterns

2. **Scheduling & Queue**
   - Appointment booking flow
   - Walk-in patient handling
   - Queue/waitlist management
   - Resource allocation (doctor, room, equipment)
   - Conflict detection

3. **Clinical Documentation**
   - Encounter/visit structure
   - SOAP notes or equivalent
   - Vital signs capture
   - Diagnosis coding (ICD-10/SNOMED)
   - Treatment plan recording

4. **Billing & Finance**
   - Service catalog/price list
   - Invoice generation
   - Payment recording (cash, card, insurance)
   - Insurance claim processing
   - Receipt printing

5. **Inventory & Pharmacy**
   - Medication master data
   - Stock tracking
   - Prescription writing
   - Dispense recording
   - Reorder alerts

6. **Security & Audit**
   - Authentication (local, SSO, MFA)
   - Authorization (RBAC, ABAC)
   - Audit logging (who, what, when)
   - Data encryption
   - HIPAA/GDPR compliance patterns

7. **Reporting & Analytics**
   - Daily patient counts
   - Revenue reports
   - Diagnosis distribution
   - Appointment no-show rates
   - Inventory consumption

---

## Key Findings (In Progress)

### Patient Identifier Patterns

**CARE Approach:** Config-driven PatientIdentifierConfig + PatientIdentifier
- **Pros:** Flexible, supports multiple ID types per patient
- **Cons:** Additional join for queries, config management overhead
- **Evidence:** `reference/care/emr/models/patient.py` lines 50-80

**ClinicCare Proposal:** Enum-based PatientIdentifierType
- Simpler for small clinic (passport, national ID, MRN)
- Direct foreign key, easier queries
- See ADR-003 in `06-architecture-decisions.md`

**OpenEMR Approach:** TBD (pending clone completion)

**OpenMRS Approach:** TBD (not started)

### Scheduling Architecture

**CARE Approach:** 7-entity system
- Schedule → Availability → AvailabilityException → TokenSlot → TokenBooking
- Token → TokenQueue → TokenCategory → TokenSubQueue
- **Assessment:** Hospital-grade, too complex for single-doctor clinic

**ClinicCare Proposal:** 2-entity system
- Appointment (pre-booked)
- QueueEntry (walk-ins + waiting patients)
- See ADR-004 in `06-architecture-decisions.md`

**OpenEMR Approach:** TBD

**OpenMRS Approach:** TBD

### Billing Models

**CARE Approach:** 5-entity FHIR-aligned
- Account → ChargeItemDefinition → ChargeItem → Invoice → PaymentReconciliation
- **Assessment:** Insurance-focused, too complex for cash/card clinic

**ClinicCare Proposal:** 4-entity simplified
- Service → Invoice → InvoiceLineItem → Payment
- See ADR-005 in `06-architecture-decisions.md`

**OpenEMR Approach:** TBD

**OpenMRS Approach:** TBD

---

## Research Timeline

| System | Clone | Investigation | Documentation | Status |
|--------|-------|---------------|---------------|--------|
| CARE v0.1.15 | ✅ | ✅ | ✅ | COMPLETE |
| care_clinic | ✅ | ✅ | ✅ | COMPLETE |
| OpenEMR | ⚠️ Online docs | ✅ | ✅ | COMPLETE |
| OpenMRS | ⚠️ Online docs | ✅ | ✅ | COMPLETE |
| GNU Health | ⚠️ Online docs | ✅ | ✅ | COMPLETE |
| TPT Doctor | ❌ Not found | ❌ | ❌ | NOT RELEVANT |

**Note:** OpenEMR, OpenMRS, and GNU Health researched via online documentation and schema references due to repository size/availability. CARE researched via actual source code at pinned commit.

---

## Blocking Issues

### ✅ RESOLVED: All Reference Research Complete

**OpenEMR:** Researched via online documentation (ar_session table schema, accounting wiki)  
**OpenMRS:** Researched via GitHub repository and Atlassian wiki (Cashier/Billing modules)  
**GNU Health:** Researched via official documentation (Tryton integration)  
**TPT Doctor:** Not found; determined to be not relevant

**Conclusion:** Sufficient comparative research completed. Four major open-source healthcare systems analyzed (CARE, OpenEMR, OpenMRS, GNU Health).

---

## Next Actions

1. **✅ COMPLETE:** OpenEMR research via online documentation
2. **✅ COMPLETE:** OpenMRS Cashier/Billing module research  
3. **✅ COMPLETE:** GNU Health financial integration research
4. **✅ COMPLETE:** All comparative research
5. **NEXT:** Apply findings to ClinicCare domain model (add CashDrawer/Expense entities)
6. **NEXT:** Create financial workflows document with cash reconciliation

---

## Decision Impact

Research findings directly inform:
- **03-product-scope.md:** Feature prioritization based on common patterns
- **04-workflows.md:** Proven workflow designs from successful systems
- **05-domain-model.md:** Entity relationships validated across multiple implementations
- **06-architecture-decisions.md:** Evidence for ADR proposals

---

## Evidence Trail

- CARE domain model: `reference/care/emr/models/*.py` (40+ entity files)
- care_clinic architecture: `reference/care_clinic/README.md`, `reference/care_clinic/docs/README.md`
- OpenEMR: Pending clone completion
- OpenMRS: Not started
- GNU Health: Not started
- TPT Doctor: Not located

---

## Notes

- **CARE is sufficient for initial planning:** 40+ entities, comprehensive workflows, FHIR alignment provides solid foundation
- **Additional research is confirmatory:** Looking for validation/contradictions, not starting from scratch
- **Pragmatic scope:** Focus on systems actually used in similar contexts (small clinic, single doctor)
- **Time-boxed:** Don't block planning indefinitely on complete comparative analysis

---

*This document will be updated as additional reference systems are researched.*
