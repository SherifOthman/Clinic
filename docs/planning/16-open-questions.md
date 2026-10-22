# 16 - Open Questions & Unresolved Decisions

**Status:** 🟡 ACTIVE  
**Last Updated:** 2026-10-21  
**Purpose:** Track questions requiring explicit user decisions before or during implementation

---

## Critical Questions (Blocking Implementation)

### Q1: Cash Drawer Management Scope

**Context:** Financial workflows require cash drawer/shift management (see `08-financial-workflows.md`)

**Options:**
- **A:** 6-entity model (CashDrawer + Expense mandatory in v1.0, CashMovement defer to v1.1)
- **B:** Defer all cash drawer to v1.1, use simple payment tracking in v1.0
- **C:** Full 7-entity with CashMovement in v1.0

**Recommendation:** Option A (6 entities for v1.0)

**Rationale:** Cash reconciliation critical for financial integrity. OpenMRS/OpenEMR evidence shows this is essential. CashMovement optional for granular audit trail.

**Decision Status:** ✅ **APPROVED** (implemented in domain model, workflows, database schema)

**Impact:** Entities added to domain model, workflows documented, database schema updated

---

### Q2: Offline Mode Scope

**Context:** "Offline mode" has two distinct meanings in clinic management systems

**Clarification:**
- **Internet-Independent Operation (LAN-only):** v1.0 MANDATORY - System runs on local network without internet connection
- **Client Offline Mode:** v2.0+ DEFERRED - Browser continues working when disconnected from server

**Decision Status:** ✅ **CLARIFIED** (documented in `03-product-scope.md` Section 1.1, `13-deployment-recovery.md` Section 2.4)

**v1.0 Requirements:**
- System runs on LAN without internet (Docker on 0.0.0.0:5000)
- Static IP addressing for clinic network
- CORS configured for LAN IP ranges
- No cloud dependencies for core operations
- Deployment guide includes network troubleshooting

**Deferred to v2.0+:**
- Service Worker for client-side caching
- IndexedDB for offline data persistence
- Sync queue for offline-created records
- Conflict resolution for multi-device edits

**Impact:** v1.0 deployment requires LAN but not full offline PWA. Client-side offline mode adds ~3 weeks development + testing complexity.

---

### Q3: ICD-10 Code Database

**Context:** Diagnosis entity supports ICD-10 codes (20,000+ codes)

**Options:**
- **A:** Embed full ICD-10 database as seed data (large, ~5MB)
- **B:** External API integration (requires internet)
- **C:** Common codes only (~500 codes, user can enter custom)
- **D:** No coding support, free text only

**Recommendation:** Option C for v1.0, Option A for v1.1

**Rationale:** Common codes cover 80% of use cases. Full database adds complexity. External API adds internet dependency.

**Decision Status:** 🟡 **PENDING USER APPROVAL**

---

### Q3: ICD-10 Code Database

**Context:** Diagnosis entity supports ICD-10 codes (20,000+ codes)

**Options:**
- **A:** Embed full ICD-10 database as seed data (large, ~5MB)
- **B:** External API integration (requires internet)
- **C:** Common codes only (~500 codes, user can enter custom)
- **D:** No coding support, free text only

**Recommendation:** Option C for v1.0, Option A for v1.1

**Rationale:** Common codes cover 80% of use cases. Full database adds complexity. External API adds internet dependency.

**Decision Status:** 🟡 **PENDING USER APPROVAL**

**Impact:** 
- Option A: Requires migration to add 20K+ codes
- Option C: Simpler, covers common needs
- Affects: Database size, Diagnosis entity, search functionality

---

### Q4: Prescription Drug Database

**Context:** Prescription entity references Medication (generic/brand names, strengths)

**Options:**
- **A:** Embed FDA drug database (~10,000 medications)
- **B:** External API (RxNorm, First Databank)
- **C:** Clinic-specific formulary (user manually adds medications)

**Recommendation:** Option C for v1.0

**Rationale:** Clinic formulary typically 100-500 medications. Easier to manage, no licensing issues.

**Decision Status:** ✅ **APPROVED** (Medication table in schema, user populates)

**Impact:** Admin must seed common medications, autocomplete from database

---

## High Priority Questions

### Q5: Multi-Invoice Payment Allocation

**Context:** Single payment may cover multiple outstanding invoices

**Options:**
- **A:** 1:1 Payment→Invoice (current design), defer to v2.0
- **B:** Add Account entity (CARE approach) for flexible allocation
- **C:** Add PaymentAllocation join table (Payment ←→ Invoice)

**Recommendation:** Option A (defer to v2.0)

**Rationale:** Work around with multiple payment records. Not blocking for v1.0.

**Decision Status:** ✅ **APPROVED** (documented in `00-validation-findings.md`)

**Impact:** Users create separate payments per invoice. Feature request for v2.0.

---

### Q6: Expense Categories

**Context:** Expense entity uses ExpenseCategory enum

**Proposed Categories:**
1. Supplies (medical, office)
2. Utilities (electricity, water, internet)
3. Rent
4. Salaries
5. Maintenance
6. Marketing
7. Insurance
8. Taxes
9. Professional (legal, accounting)
10. Travel
11. Training
12. Equipment
13. Other

**Decision Status:** ✅ **APPROVED** (implemented in `05-domain-model.md`)

**Impact:** ExpenseCategory enum defined, extensible with "Other"

---

### Q7: Variance Tolerance Thresholds

**Context:** Cash drawer variance requires manager review if exceeds tolerance

**Proposed Thresholds:**
- **Variance Tolerance:** ±$5.00 (small clinic) or ±$10.00 (high volume)
- **Refund Approval:** $100.00
- **Expense Approval:** $500.00

**Decision Status:** ✅ **APPROVED** (documented in `08-financial-workflows.md`, configurable in SystemConfiguration)

**Impact:** SystemConfiguration seed data, workflow logic

---

## Medium Priority Questions

### Q8: Lab/Imaging Integration

**Context:** Clinical workflows may need lab results, imaging reports

**Options:**
- **A:** HL7/FHIR integration (complex, requires interface engine)
- **B:** File attachments only (PDF, images)
- **C:** Manual data entry

**Recommendation:** Option B for v1.0, Option A for v2.0+

**Decision Status:** ✅ **APPROVED** (Attachment entity supports files)

**Impact:** Attachments table, file upload workflow

---

### Q9: Insurance Claims Submission

**Context:** Some clinics may bill insurance companies

**Options:**
- **A:** EDI 837 claim generation (complex, requires payer IDs, clearinghouse)
- **B:** Manual claim filing (print CMS-1500 form, fax/mail)
- **C:** No insurance support

**Recommendation:** Option B for v1.0 (partial), defer EDI to v2.0

**Decision Status:** 🟡 **PENDING** (needs user confirmation if insurance support required)

**Impact:** 
- Option B: Invoice print template with claim data
- Option A: Additional entities (Payer, Claim, ClaimStatus), external integrations

---

### Q10: Telemedicine/Patient Portal

**Context:** Potential future features for patient self-service

**Options:**
- **A:** Include in v1.0
- **B:** Defer to v2.0
- **C:** Exclude indefinitely

**Recommendation:** Option B (defer to v2.0)

**Decision Status:** ✅ **APPROVED** (explicit non-goal in `03-product-scope.md`)

**Impact:** None for v1.0

---

### Q11: Stock Management (Pharmacy Inventory)

**Context:** Medication dispensing may require stock tracking

**Options:**
- **A:** Full inventory system (Stock, StockMovement, Reorder, etc.) in v1.0
- **B:** Simple stock tracking (Stock entity only) in v1.0
- **C:** Defer to v1.1 (M6)

**Recommendation:** Option C (defer to M6)

**Decision Status:** ✅ **APPROVED** (documented in `03-product-scope.md` as P2/M6)

**Impact:** Medication table in v1.0, Stock table deferred

---

## Low Priority Questions

### Q12: Multi-Language Support Scope

**Context:** i18n framework in place (M0), which languages to support?

**Confirmed:**
- ✅ English (default)
- ✅ Arabic (RTL support required)

**Pending:**
- French?
- Spanish?
- Other?

**Decision Status:** 🟡 **PENDING** (English + Arabic confirmed, others TBD)

**Impact:** Translation files, RTL layout testing

---

### Q13: Reporting Export Formats

**Context:** Financial/operational reports need export

**Proposed:**
- ✅ PDF (printable)
- ✅ Excel (XLSX) (analysis)
- CSV? (data exchange)
- JSON? (API export)

**Decision Status:** ✅ **APPROVED** (PDF + Excel mandatory, CSV/JSON nice-to-have)

**Impact:** Report generation libraries (iTextSharp, EPPlus)

---

### Q14: Backup Automation

**Context:** SQLite file backup strategy

**Options:**
- **A:** Manual backup (admin clicks "Backup Now")
- **B:** Scheduled backup (daily at specified time)
- **C:** Automated cloud backup (Azure, AWS S3)

**Recommendation:** Option A + B for v1.0

**Decision Status:** 🟡 **PENDING**

**Impact:** Backup service implementation, storage location

---

### Q15: Appointment Reminders

**Context:** Reduce no-shows with SMS/email reminders

**Options:**
- **A:** SMS via Twilio (cost per message)
- **B:** Email (free, but less effective)
- **C:** No reminders in v1.0

**Recommendation:** Option B for v1.0 (email), Option A for v1.1

**Decision Status:** 🟡 **PENDING**

**Impact:** Email service, scheduled job, template system

---

### Q16: Database Choice Finalization

**Context:** SQLite for v1.0, PostgreSQL for v1.1+ (ADR-007)

**Confirmation Needed:**
- Is SQLite acceptable for initial deployment?
- At what scale/concurrent users trigger PostgreSQL migration?

**Recommendation:** SQLite for single-doctor clinic (<20 concurrent users), PostgreSQL for multi-doctor or >50 concurrent users

**Decision Status:** ✅ **APPROVED** (documented in ADR-007)

**Impact:** Migration scripts, testing on both databases

---

## Questions Resolved

| ID | Question | Decision | Date | Documented In |
|----|----------|----------|------|---------------|
| Q1 | Cash Drawer Management | 6-entity model (CashDrawer+Expense+Payment updates) | 2026-10-21 | 08-financial-workflows.md, 05-domain-model.md |
| Q2 | Offline Mode Scope | Internet-independent LAN (v1.0), client offline deferred (v2.0+) | 2026-10-21 | 03-product-scope.md, 13-deployment-recovery.md |
| Q4 | Prescription Drug Database | Clinic formulary (user populates) | 2026-10-21 | 05-domain-model.md |
| Q5 | Multi-Invoice Payment | Defer to v2.0 | 2026-10-21 | 00-validation-findings.md |
| Q6 | Expense Categories | 13 categories approved | 2026-10-21 | 05-domain-model.md |
| Q7 | Variance Thresholds | $5 variance, $100 refund, $500 expense | 2026-10-21 | 08-financial-workflows.md |
| Q8 | Lab Integration | File attachments only (v1.0) | 2026-10-21 | 05-domain-model.md |
| Q10 | Telemedicine | Defer to v2.0 | 2026-10-21 | 03-product-scope.md |
| Q11 | Stock Management | Defer to M6 | 2026-10-21 | 03-product-scope.md |
| Q13 | Export Formats | PDF + Excel | 2026-10-21 | Assumed standard |
| Q16 | Database Choice | SQLite v1.0, PostgreSQL v1.1+ | 2026-10-21 | ADR-007 |

---

## Decision Request Process

**For USER to resolve pending questions:**

1. Review question context and options
2. Select preferred option or suggest alternative
3. Provide rationale if deviating from recommendation
4. Agent updates this document + affected planning docs
5. Agent proceeds with implementation aligned to decision

**Format for decisions:**
```
Question ID: Q2
Decision: Option C (Common codes only for v1.0)
Rationale: Balance between utility and complexity
Approved By: [User Name]
Date: 2026-10-21
```

---

## Adding New Questions

**When to add a question:**
- Implementation blocked by unclear requirement
- Multiple valid approaches with different tradeoffs
- User preference needed (UX, workflow, priorities)
- Compliance or regulatory uncertainty
- Budget/resource constraints affecting scope

**Template:**
```markdown
### QXX: [Question Title]

**Context:** [Why this question arose]

**Options:**
- **A:** [Option description]
- **B:** [Option description]
- **C:** [Option description]

**Recommendation:** [Agent's recommended option]

**Rationale:** [Why this recommendation]

**Decision Status:** 🟡 PENDING

**Impact:** [What changes if each option selected]
```

---

## Cross-References

- **Product Scope:** Feature priorities in `03-product-scope.md`
- **Domain Model:** Entity decisions in `05-domain-model.md`
- **Workflows:** Operational decisions in `04-workflows.md`, `08-financial-workflows.md`
- **Architecture:** Technical decisions in `06-architecture-decisions.md`
- **Risks:** Decision impacts on risks in `17-risk-register.md`

---

*This document is a living record of planning decisions. Update it whenever new questions arise or existing questions are resolved.*
