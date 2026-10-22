# 08 - Financial Workflows & Cash Management

**Status:** 🟢 DRAFT  
**Last Updated:** 2026-10-21  
**Dependencies:** 03-product-scope.md, 04-workflows.md, 05-domain-model.md, 02-reference-research.md  
**Feeds Into:** 05-domain-model.md (updates), 07-database-design.md, 12-testing-strategy.md

---

## Purpose

Define comprehensive financial workflows for ClinicCare including cash drawer management, expense tracking, shift reconciliation, refunds, and financial reporting. Based on research from OpenMRS Cashier module, OpenEMR ar_session, and CARE billing patterns.

---

## Evidence Base

This document is informed by actual implementations in:
- **OpenMRS Cashier Module:** CashPoint + Timesheet entities, shift-based reconciliation
- **OpenEMR:** `ar_session` table with open/closed status, deposit tracking
- **CARE:** Account entity with cached totals, PaymentReconciliation with tendered/returned amounts
- **Validation:** See `00-validation-findings.md` Section "Cash Drawer & Expense Management"

---

## Required Additional Entities

Based on comparative research, the **4-entity billing model is insufficient**. We need **7 entities** for complete financial management:

### Existing (from 05-domain-model.md):
1. **Service** - Catalog of billable services
2. **Invoice** - Patient bills
3. **InvoiceLineItem** - Individual charges
4. **Payment** - Payments and refunds

### Required Additions:
5. **CashDrawer** (OpenMRS: Timesheet, OpenEMR: ar_session) - Shift/drawer management
6. **Expense** - Clinic operating expenses
7. **CashMovement** (optional for v1.0) - Granular cash tracking

**Rationale:** Without these, cannot reconcile cash drawer or track clinic expenses. Evidence: every researched system (OpenMRS, OpenEMR) has shift/session management.

---

## Financial Workflow Categories

1. **Cash Drawer Management:** Opening, operating, closing, reconciliation
2. **Invoice & Payment:** Service delivery → billing → payment collection
3. **Refund Processing:** Full and partial refunds with approval
4. **Expense Management:** Recording clinic expenses
5. **Daily Reconciliation:** End-of-day cash counting and variance resolution
6. **Financial Reporting:** Daily/weekly/monthly reports by branch

---

## 1. Cash Drawer Management Workflows

### 1.1 Open Cash Drawer (Start of Shift)

**ACTOR:** Receptionist  
**FREQUENCY:** Once per shift (typically once per day per branch)  
**PRECONDITIONS:**
- User has Receptionist or Admin role
- Branch assigned to user
- No open drawer for this user at this branch today

**STEPS:**
1. Navigate to Cash Management → Open Drawer
2. Select branch (defaults to user's assigned branch)
3. Enter opening cash amount (starting float)
   - Example: 500 USD (standard starting cash)
4. System validates:
   - No drawer already open for this user+branch
   - Opening amount ≥ 0
5. Click "Open Drawer"
6. System creates CashDrawer record:
   - UserId = Current user
   - BranchId = Selected branch
   - OpenedAt = Current timestamp
   - OpeningBalance = Entered amount
   - Status = Open
   - ExpectedBalance = OpeningBalance (initially)
   - ActualBalance = null (not yet counted)
7. System records opening as CashMovement:
   - Type = DrawerOpen
   - Amount = Opening balance
   - CashDrawerId = New drawer ID
8. Show success: "Cash drawer opened - Starting balance: $500.00"
9. Print drawer opening receipt (optional)

**POSTCONDITIONS:**
- CashDrawer exists with Status = Open
- User can now create invoices and record payments
- Only one open drawer per user per branch allowed

**BUSINESS RULES:**
- Opening balance typically 200-1000 USD (configurable per branch)
- User must close previous day's drawer before opening new one
- Only one open drawer per user+branch+day
- Opening balance recorded in audit log

**EXCEPTIONS:**
- **Drawer Already Open:** "You have an open drawer. Close it before opening a new one."
- **Previous Drawer Not Closed:** Warning "Previous drawer not closed/reconciled. Close it first?"

**UI MOCKUP:** `wireframes/open-cash-drawer.png`

**Evidence:** OpenMRS Timesheet clock-in, OpenEMR ar_session open

---

### 1.2 Record Cash Payment (During Shift)

**ACTOR:** Receptionist  
**PRECONDITIONS:**
- Cash drawer open (Status = Open)
- Invoice issued (Status = Issued OR PartiallyPaid)

**STEPS:**
1. From invoice detail view → Click "Record Payment"
2. Payment Method dropdown → Select "Cash"
3. Enter payment details:
   - Amount Tendered: 100 USD (what patient gave)
   - System auto-calculates:
     - Amount Due: 85 USD (remaining balance)
     - Change to Give: 15 USD
4. Confirm amounts displayed clearly
5. Click "Record Payment"
6. System creates Payment record:
   - InvoiceId = Current invoice
   - CashDrawerId = User's open drawer
   - Amount = 85 USD (applied to invoice)
   - AmountTendered = 100 USD
   - AmountReturned = 15 USD (change given)
   - PaymentMethod = Cash
   - PaymentDate = Now
   - RecordedBy = Current user
7. System updates Invoice:
   - TotalPaid += 85
   - RemainingBalance -= 85
   - Status = Paid (if balance = 0) OR PartiallyPaid
8. System updates CashDrawer:
   - ExpectedBalance += 85 (net cash IN)
9. System records CashMovement:
   - Type = PaymentIn
   - Amount = 85 USD (net)
   - Reference = Invoice number
10. Print receipt with change amount highlighted

**POSTCONDITIONS:**
- Payment recorded and linked to drawer
- Invoice balance updated
- Expected drawer balance increased
- Patient receives receipt with change amount

**BUSINESS RULES:**
- AmountTendered ≥ AmountDue (patient can't give less)
- AmountReturned = AmountTendered - AmountDue (exact change)
- If AmountTendered = AmountDue, no change needed (AmountReturned = 0)
- Drawer balance updated in real-time
- Cannot record cash payment without open drawer

**EXCEPTIONS:**
- **No Open Drawer:** "Please open cash drawer first"
- **Insufficient Tendered:** "Customer must tender at least $85.00"
- **Large Bill:** Warning if tendered > balance × 5 (e.g., $500 for $10 bill)

**UI MOCKUP:** `wireframes/record-cash-payment.png`

**Evidence:** CARE PaymentReconciliation tendered/returned amounts

---

### 1.3 Cash Refund (During Shift)

**ACTOR:** Receptionist (Admin approval if > threshold)  
**PRECONDITIONS:**
- Cash drawer open
- Invoice has TotalPaid > 0
- Refund reason documented

**STEPS:**
1. From invoice → Click "Refund"
2. Enter refund details:
   - Amount: 50 USD (max: TotalPaid)
   - Reason: Overpayment / Cancelled Service / Billing Error / Other
   - Notes: Optional explanation
3. If refund > threshold (e.g., 100 USD):
   - Require admin approval
   - Send notification to admin
   - Admin reviews → Approve or Reject
4. If approved (or below threshold):
   - Click "Process Refund"
5. System creates Payment record:
   - InvoiceId = Current invoice
   - CashDrawerId = User's open drawer
   - Amount = -50 USD (negative)
   - IsRefund = true
   - RefundReason = Selected reason
   - PaymentMethod = Cash
6. System updates Invoice:
   - TotalPaid -= 50
   - RemainingBalance += 50
   - Status = PartiallyPaid (if balance > 0) OR Issued
7. System updates CashDrawer:
   - ExpectedBalance -= 50 (cash OUT)
8. System records CashMovement:
   - Type = Refund
   - Amount = -50
   - Reference = Invoice number
9. Print refund receipt

**POSTCONDITIONS:**
- Refund recorded as negative payment
- Drawer balance decreased
- Invoice balance adjusted
- Audit trail complete

**BUSINESS RULES:**
- Refund amount ≤ TotalPaid
- Refund reason required
- Admin approval if amount > threshold (configurable, default 100 USD)
- Cannot refund more than paid
- Refunds reduce drawer balance immediately

**EXCEPTIONS:**
- **Refund Exceeds Paid:** "Cannot refund more than $85.00 (total paid)"
- **Approval Denied:** "Refund request denied by admin"
- **Insufficient Cash in Drawer:** Warning if refund > drawer balance

**UI MOCKUP:** `wireframes/process-refund.png`

**Evidence:** OpenMRS refund approval workflow

---

### 1.4 Record Expense (During Shift)

**ACTOR:** Receptionist, Doctor (with permission)  
**PRECONDITIONS:**
- Cash drawer open (for cash expenses)
- Expense category configured

**STEPS:**
1. Navigate to Cash Management → Record Expense
2. Enter expense details:
   - Category: Dropdown (Supplies, Utilities, Rent, Salaries, Maintenance, Other)
   - Description: "Office supplies - printer paper"
   - Amount: 25 USD
   - Payment Method: Cash / Card / Bank Transfer
   - Receipt Number: Optional
   - Paid Date: Default today
3. If Payment Method = Cash:
   - Verify drawer has sufficient balance
   - Confirm drawer deduction
4. Upload receipt image (optional)
5. Click "Record Expense"
6. System creates Expense record:
   - BranchId = Current branch
   - Category = Selected category
   - Description = Entered text
   - Amount = 25 USD
   - PaymentMethod = Cash
   - PaidBy = Current user
   - PaidAt = Selected date
   - CashDrawerId = User's open drawer (if cash)
   - ReceiptPath = Uploaded file path
7. If cash expense:
   - System updates CashDrawer:
     - ExpectedBalance -= 25
   - System records CashMovement:
     - Type = Expense
     - Amount = -25
     - Reference = Expense description
8. Show success: "Expense recorded - $25.00 deducted from drawer"

**POSTCONDITIONS:**
- Expense recorded and categorized
- Cash drawer balance decreased (if cash)
- Receipt stored (if uploaded)
- Expense appears in reports

**BUSINESS RULES:**
- Expense categories configurable in SystemConfiguration
- Cash expenses require open drawer
- Large expenses (> 500 USD) may require admin approval (configurable)
- Receipt upload encouraged but not mandatory
- Expenses reduce drawer balance immediately

**EXCEPTIONS:**
- **Insufficient Cash:** "Drawer balance ($50) insufficient for expense ($75)"
- **No Open Drawer (for cash):** "Please open drawer or use non-cash payment method"
- **Approval Required:** "Expense exceeds limit. Admin approval required."

**UI MOCKUP:** `wireframes/record-expense.png`

**Evidence:** Common requirement in clinic management (not explicitly in CARE)

---

### 1.5 Close Cash Drawer (End of Shift)

**ACTOR:** Receptionist  
**FREQUENCY:** Once per shift (typically end of day)  
**PRECONDITIONS:**
- Drawer open (Status = Open)
- All payments for the day recorded
- Ready to count physical cash

**STEPS:**
1. Navigate to Cash Management → Close Drawer
2. System displays current shift summary:
   - Opening Balance: $500.00
   - Cash Payments Received: $850.00
   - Refunds Given: -$50.00
   - Expenses Paid: -$25.00
   - **Expected Balance: $1,275.00**
3. Physical cash count:
   - Count actual cash in drawer
   - Enter breakdown by denomination (optional):
     - $100 bills: × 10 = $1,000
     - $50 bills: × 4 = $200
     - $20 bills: × 3 = $60
     - $10 bills: × 1 = $10
     - $5 bills: × 1 = $5
     - Coins: $0.50
     - **Total Counted: $1,275.50**
4. System calculates variance:
   - Variance = ActualBalance - ExpectedBalance
   - Variance = $1,275.50 - $1,275.00 = **+$0.50 (overage)**
5. Enter variance explanation (if any):
   - "Rounding difference from change given"
6. Review reconciliation report
7. Click "Close Drawer"
8. System updates CashDrawer:
   - ClosedAt = Current timestamp
   - ClosedBy = Current user
   - ActualBalance = $1,275.50
   - Variance = +$0.50
   - VarianceReason = Entered explanation
   - Status = Closed
9. System records CashMovement:
   - Type = DrawerClose
   - Amount = ActualBalance
10. Print shift summary report
11. Optional: Record bank deposit
    - Amount to Deposit: $1,000 (keep $275 for tomorrow's float)
    - Deposit Date: Tomorrow
    - Bank Reference: TBD

**POSTCONDITIONS:**
- Drawer closed and reconciled
- Variance documented
- Shift summary printed
- Cash counted and secured
- Drawer ready for next day

**BUSINESS RULES:**
- Variance tolerance: ±$5.00 (configurable)
- Large variance (> $10) requires manager review
- Drawer cannot be reopened once closed (create new drawer next day)
- All payments must be recorded before closing
- Bank deposit optional but recommended

**EXCEPTIONS:**
- **Large Variance:** Warning "Variance ($15) exceeds tolerance. Manager review required."
- **Pending Invoices:** Warning "3 invoices unpaid. Confirm before closing?"
- **Missing Explanation:** "Large variance requires explanation"

**UI MOCKUP:** `wireframes/close-cash-drawer.png`

**Evidence:** OpenMRS Timesheet close-out, OpenEMR ar_session closed=1

---

### 1.6 Manager Review Variance

**ACTOR:** Doctor, Admin  
**PRECONDITIONS:**
- Drawer closed with variance > tolerance
- Manager review required flag set

**STEPS:**
1. Navigate to Cash Management → Pending Reviews
2. Select drawer with variance
3. Review shift summary:
   - Expected: $1,275.00
   - Actual: $1,260.00
   - Variance: **-$15.00 (shortage)**
   - Explanation: "Unsure - no errors found"
4. Review all transactions for the shift:
   - List of invoices, payments, refunds, expenses
   - Check for data entry errors
5. Manager actions:
   - **Approve:** Accept variance, drawer reconciliation complete
   - **Reject:** Flag for investigation, cashier notified
   - **Adjust:** Correct error (e.g., duplicate entry), recalculate
6. Add manager notes: "Approved - within acceptable range for high volume day"
7. Click action button
8. System updates CashDrawer:
   - ManagerReviewedBy = Current user
   - ManagerReviewedAt = Now
   - ManagerNotes = Entered notes
   - Status = Reconciled (if approved) OR Investigation (if rejected)

**POSTCONDITIONS:**
- Variance reviewed and documented
- Cashier notified of outcome
- Drawer finalized or flagged for investigation

**BUSINESS RULES:**
- Manager role required for variance > tolerance
- Rejection triggers investigation workflow
- Approved variances recorded in reports
- Frequent variances may trigger retraining

**EXCEPTIONS:**
- **Large Shortage:** Automatic escalation to admin
- **Pattern Detected:** "Cashier has 3 recent variances. Schedule review?"

**UI MOCKUP:** `wireframes/manager-review-variance.png`

**Evidence:** Common practice in retail/clinic cash management

---

## 2. Invoice & Payment Workflows

### 2.1 Generate Invoice from Encounter

*See `04-workflows.md` Section 5.1 (Auto from Encounter)*

**Key Additions for Cash Drawer Integration:**
- Invoice creation does NOT affect drawer balance
- Only when payment recorded does drawer balance change
- Link invoice to branch's active drawer (if any)

### 2.2 Record Non-Cash Payment

**ACTOR:** Receptionist  
**PRECONDITIONS:**
- Invoice issued

**STEPS:**
1. From invoice → Click "Record Payment"
2. Payment Method: **Card** / Bank Transfer / Insurance / Other
3. Enter payment details:
   - Amount: Full or partial
   - Reference: Last 4 digits of card, transaction ID, etc.
   - Notes: Optional
4. Click "Record Payment"
5. System creates Payment record:
   - InvoiceId = Current invoice
   - CashDrawerId = **null** (non-cash, not drawer-linked)
   - Amount = Entered amount
   - PaymentMethod = Selected method
   - Reference = Entered reference
6. System updates Invoice balances
7. **No drawer balance change** (non-cash)
8. Print receipt

**POSTCONDITIONS:**
- Payment recorded
- Drawer balance unchanged
- Invoice balance updated

**BUSINESS RULES:**
- Non-cash payments not linked to drawer
- Card/bank payments recorded but don't affect cash reconciliation
- Still counted in revenue reports

**Evidence:** Standard practice

---

## 3. Daily Reconciliation Workflow

### 3.1 End-of-Day Reconciliation Report

**ACTOR:** Admin, Doctor  
**FREQUENCY:** Daily  
**PRECONDITIONS:**
- All drawers for the day closed
- All payments recorded

**STEPS:**
1. Navigate to Reports → Financial → Daily Reconciliation
2. Select date (default: today)
3. Select branch (or all branches)
4. System generates report:

**Revenue Summary:**
- Total Invoices Issued: 25 invoices, $3,500.00
- Total Payments Received: $3,200.00
  - Cash: $2,100.00
  - Card: $950.00
  - Bank Transfer: $150.00
- Outstanding Balance: $300.00
- Refunds Given: $50.00

**Cash Drawer Summary (per drawer/branch):**
| Branch | Cashier | Opening | Cash In | Cash Out | Expected | Actual | Variance | Status |
|--------|---------|---------|---------|----------|----------|--------|----------|--------|
| Main | Alice | $500 | $1,850 | -$75 | $2,275 | $2,280 | +$5 | Reconciled |
| North | Bob | $300 | $250 | -$20 | $530 | $525 | -$5 | Reconciled |
| **Total** | - | **$800** | **$2,100** | **-$95** | **$2,805** | **$2,805** | **$0** | ✅ |

**Expense Summary:**
- Supplies: $35.00
- Utilities: $0.00
- Maintenance: $60.00
- **Total Expenses: $95.00**

**Net Cash Position:**
- Cash Revenue: $2,100.00
- Less Refunds: -$50.00
- Less Expenses: -$95.00
- **Net Cash Flow: $1,955.00**
- Opening Cash: $800.00
- **Ending Cash: $2,755.00** (Expected)
- **Actual Cash: $2,805.00**
- **Variance: +$50.00** (investigated and approved)

5. Export to PDF / Excel
6. Archive report

**POSTCONDITIONS:**
- Daily reconciliation complete
- Variances explained or escalated
- Report archived for audit

**BUSINESS RULES:**
- Report generated only after all drawers closed
- Variance rollup across all branches
- Archived for 7 years (compliance)

**UI MOCKUP:** `wireframes/daily-reconciliation-report.png`

**Evidence:** Standard accounting practice

---

## 4. Financial Reports

### 4.1 Shift Summary Report

**Content:**
- Cashier name, branch, shift time (open → close)
- Opening balance
- Transactions summary (invoices, payments, refunds, expenses)
- Expected vs actual balance
- Variance and explanation
- Manager approval status

**Evidence:** OpenMRS Shift Summary report

### 4.2 Revenue by Payment Method

**Content:**
- Date range
- Total revenue by method:
  - Cash: $X
  - Card: $Y
  - Bank Transfer: $Z
  - Insurance: $W
  - Other: $V
- Breakdown by branch
- Trend chart (daily/weekly/monthly)

**Evidence:** OpenMRS Payments by Payment Mode report

### 4.3 Expense Report

**Content:**
- Date range
- Expenses by category
- Total per category
- Breakdown by branch
- Largest expenses (top 10)
- Expense trend

**Evidence:** Standard accounting report

### 4.4 Outstanding Invoices

**Content:**
- List of unpaid/partially paid invoices
- Patient name, invoice number, date, amount due
- Aging buckets (0-30 days, 31-60, 61-90, 90+)
- Total outstanding by branch

**Evidence:** CARE Account total_balance

---

## Entity Definitions (Additions to 05-domain-model.md)

### 5.1 CashDrawer Entity

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Track cash drawer shifts with opening/closing balances and reconciliation

**Properties:**
| Property | Type | Nullable | Description |
|----------|------|----------|-------------|
| BranchId | Guid | No | Foreign key to Branch |
| UserId | string | No | Foreign key to ApplicationUser (cashier) |
| OpenedAt | DateTime | No | Shift start timestamp |
| ClosedAt | DateTime | Yes | Shift end timestamp (null if still open) |
| OpeningBalance | decimal | No | Starting cash amount |
| ExpectedBalance | decimal | No | Calculated: Opening + CashIn - CashOut |
| ActualBalance | decimal | Yes | Counted cash at close (null if not yet closed) |
| Variance | decimal | Yes | ActualBalance - ExpectedBalance |
| VarianceReason | string | Yes | Explanation for variance |
| Status | CashDrawerStatus | No | Enum: Open, Closed, Reconciled, Investigation |
| ManagerReviewedBy | string | Yes | FK to ApplicationUser (manager who reviewed) |
| ManagerReviewedAt | DateTime | Yes | Manager review timestamp |
| ManagerNotes | string | Yes | Manager comments on variance |
| BankDepositAmount | decimal | Yes | Amount deposited to bank |
| BankDepositDate | DateOnly | Yes | Date of bank deposit |
| BankDepositReference | string | Yes | Bank deposit receipt number |

**Relationships:**
- **Branch:** Many-to-One with Branch
- **User:** Many-to-One with ApplicationUser (cashier)
- **ManagerReviewer:** Many-to-One with ApplicationUser (manager)
- **Payments:** One-to-Many with Payment (cash payments only)
- **Expenses:** One-to-Many with Expense (cash expenses only)
- **CashMovements:** One-to-Many with CashMovement (optional)

**Indexes:**
- BranchId, OpenedAt (for daily drawers)
- UserId, Status (for active drawer lookup)
- Status (for pending reviews)

**Business Rules:**
- Only one open drawer per UserId + BranchId at a time
- OpeningBalance ≥ 0
- ExpectedBalance calculated from transactions
- ActualBalance set only when closing
- Variance = ActualBalance - ExpectedBalance
- Large variance (> threshold) requires manager review
- Status transitions: Open → Closed → Reconciled (or Investigation)
- Cannot modify drawer after Status = Reconciled

**Evidence:** OpenMRS Timesheet, OpenEMR ar_session

**Enums:**
```csharp
public enum CashDrawerStatus
{
    Open = 1,
    Closed = 2,
    Reconciled = 3,
    Investigation = 4
}
```

---

### 5.2 Expense Entity

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Track clinic operating expenses

**Properties:**
| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| BranchId | Guid | No | - | Foreign key to Branch |
| CashDrawerId | Guid | Yes | - | FK to CashDrawer (if paid from drawer) |
| Category | ExpenseCategory | No | - | Enum: Supplies, Utilities, Rent, Salaries, Maintenance, Other |
| Description | string | No | 500 | What was purchased/paid for |
| Amount | decimal | No | - | Expense amount |
| PaymentMethod | PaymentMethod | No | - | Cash, Card, BankTransfer, etc. |
| ReceiptNumber | string | Yes | 100 | Receipt or invoice number |
| ReceiptPath | string | Yes | 500 | Path to uploaded receipt image |
| PaidBy | string | No | - | FK to ApplicationUser |
| PaidAt | DateTime | No | - | Payment date/time |
| Notes | string | Yes | 1000 | Additional notes |

**Relationships:**
- **Branch:** Many-to-One with Branch
- **CashDrawer:** Many-to-One with CashDrawer (optional, cash only)
- **PaidByUser:** Many-to-One with ApplicationUser

**Indexes:**
- BranchId, PaidAt (for date range queries)
- Category (for category reports)
- CashDrawerId (for drawer reconciliation)

**Business Rules:**
- Amount > 0
- Category required
- Description required, 1-500 characters
- PaymentMethod = Cash → CashDrawerId required
- Receipt upload encouraged but optional
- Large expenses (> threshold) may require approval
- Cannot delete (soft delete only)

**Enums:**
```csharp
public enum ExpenseCategory
{
    Supplies = 1,        // Medical/office supplies
    Utilities = 2,       // Electricity, water, internet
    Rent = 3,            // Facility rent
    Salaries = 4,        // Staff salaries (if tracked)
    Maintenance = 5,     // Repairs, equipment maintenance
    Marketing = 6,       // Advertising, promotions
    Insurance = 7,       // Liability, malpractice insurance
    Taxes = 8,           // Business taxes
    Professional = 9,    // Legal, accounting fees
    Other = 99           // Miscellaneous
}
```

**Evidence:** Standard business expense tracking

---

### 5.3 CashMovement Entity (Optional for v1.0)

**Namespace:** Domain.Entities  
**Inherits From:** AuditableEntity  
**Purpose:** Granular cash flow tracking (audit trail for drawer transactions)

**Properties:**
| Property | Type | Nullable | Description |
|----------|------|----------|-------------|
| CashDrawerId | Guid | No | FK to CashDrawer |
| Type | CashMovementType | No | Enum: DrawerOpen, PaymentIn, Refund, Expense, DrawerClose, BankDeposit, Adjustment |
| Amount | decimal | No | Movement amount (positive or negative) |
| BalanceBefore | decimal | No | Drawer balance before this movement |
| BalanceAfter | decimal | No | Drawer balance after this movement |
| Reference | string | Yes | Invoice number, expense description, etc. |
| Notes | string | Yes | Additional details |

**Relationships:**
- **CashDrawer:** Many-to-One with CashDrawer

**Indexes:**
- CashDrawerId, CreatedDate (for chronological audit trail)

**Business Rules:**
- Amount can be positive (cash in) or negative (cash out)
- BalanceBefore + Amount = BalanceAfter
- Immutable once created (audit trail)
- Automatic creation on payment/expense/refund

**Enums:**
```csharp
public enum CashMovementType
{
    DrawerOpen = 1,
    PaymentIn = 2,
    Refund = 3,
    Expense = 4,
    DrawerClose = 5,
    BankDeposit = 6,
    Adjustment = 7  // Manual correction
}
```

**Decision:** Optional for v1.0. CashDrawer.ExpectedBalance calculation can be derived from Payment and Expense queries. Add CashMovement in v1.1 if granular audit trail needed.

**Evidence:** Derived from accounting best practices

---

## Updated Payment Entity (Changes to 05-domain-model.md)

Add to Payment entity:

**New Properties:**
| Property | Type | Nullable | Description |
|----------|------|----------|-------------|
| CashDrawerId | Guid | Yes | FK to CashDrawer (cash payments only) |
| AmountTendered | decimal | Yes | Amount customer gave (cash only) |
| AmountReturned | decimal | Yes | Change given to customer (cash only) |

**Business Rules (additions):**
- PaymentMethod = Cash → CashDrawerId required
- PaymentMethod = Cash → AmountTendered ≥ Amount
- AmountReturned = AmountTendered - Amount (for cash)
- Non-cash payments: CashDrawerId = null

**Evidence:** CARE PaymentReconciliation tendered_amount, returned_amount

---

## Updated Invoice Entity (Changes to 05-domain-model.md)

No structural changes needed. Invoice already has:
- TotalAmount, TotalPaid, RemainingBalance (calculations)
- Status (Draft, Issued, PartiallyPaid, Paid, Cancelled)

Payment refunds already supported via negative Payment records.

---

## Calculation Formulas

### Cash Drawer Expected Balance
```
ExpectedBalance = OpeningBalance 
                + SUM(Payments WHERE Method=Cash AND Amount>0)
                - SUM(Payments WHERE Method=Cash AND Amount<0 [refunds])
                - SUM(Expenses WHERE Method=Cash)
```

### Cash Drawer Variance
```
Variance = ActualBalance - ExpectedBalance
```

### Daily Revenue
```
Total Revenue = SUM(Payments WHERE PaymentDate=Today AND Amount>0)
```

### Net Cash Flow
```
Net Cash = Cash Revenue - Cash Refunds - Cash Expenses
```

---

## Validation Rules

### At Drawer Open:
- ✅ No open drawer for this user+branch
- ✅ OpeningBalance ≥ 0
- ✅ Opening balance reasonable (100-2000 USD)

### At Payment Recording:
- ✅ Invoice exists and Status = Issued OR PartiallyPaid
- ✅ Payment amount > 0
- ✅ Payment amount ≤ Remaining balance (overpayment warning)
- ✅ If cash: drawer is open
- ✅ If cash: AmountTendered ≥ Amount

### At Refund:
- ✅ Refund amount > 0
- ✅ Refund amount ≤ TotalPaid
- ✅ Refund reason required
- ✅ If > threshold: admin approval required
- ✅ If cash: drawer is open
- ✅ If cash: drawer has sufficient balance (warning)

### At Expense:
- ✅ Amount > 0
- ✅ Category required
- ✅ Description required
- ✅ If cash: drawer is open
- ✅ If cash: drawer has sufficient balance (warning if balance < amount)

### At Drawer Close:
- ✅ ActualBalance entered
- ✅ Variance calculated
- ✅ If variance > tolerance: explanation required
- ✅ All invoices for shift finalized (optional check)

---

## Security & Authorization

### Permissions Required:
- **Open/Close Drawer:** Receptionist, Admin
- **Record Payment:** Receptionist, Admin
- **Record Refund:** Receptionist (< threshold), Admin (any amount)
- **Approve Refund:** Admin only
- **Record Expense:** Receptionist, Doctor, Admin
- **Review Variance:** Doctor, Admin only
- **View Financial Reports:** Doctor, Admin only
- **Modify Closed Drawer:** Admin only (audit log recorded)

---

## Testing Criteria

### Unit Tests:
- Drawer balance calculations
- Variance calculation
- Payment tender/change calculation
- Refund validation
- Expense category enforcement

### Integration Tests:
- Open drawer → record payments → close drawer → variance = 0
- Open drawer → record payment + refund → close → variance matches
- Multiple drawers (different branches) independent
- Drawer open twice → validation error
- Close drawer → reopen (new drawer)

### End-to-End Tests:
- Full shift: open → 10 payments → 2 refunds → 3 expenses → close → reconcile
- Variance > tolerance → manager review → approval
- Large refund → approval workflow
- Daily reconciliation report accuracy

---

## Migration from 4-Entity to 7-Entity Model

**Existing invoices/payments preserved:**
- Payments without CashDrawerId → historical, non-drawer payments
- New payments (post-migration) → require drawer if cash

**Steps:**
1. Add CashDrawer, Expense, CashMovement tables
2. Add CashDrawerId, AmountTendered, AmountReturned columns to Payment
3. Migrate existing cash payments (set CashDrawerId = null for historical)
4. Enforce drawer requirement going forward

---

## Cross-References

- **Workflows:** See `04-workflows.md` Section 5 (Billing) - update with drawer steps
- **Domain Model:** See `05-domain-model.md` Section 5 (Billing) - add 3 entities
- **Database Design:** See `07-database-design.md` - DDL for new tables
- **API Spec:** See `11-backend-api-specification.md` - endpoints for drawer management
- **Testing:** See `12-testing-strategy.md` - financial workflow test cases

---

## Open Questions

See `16-open-questions.md`:

**Q1:** Variance tolerance threshold? (Proposed: ±$5 for small clinics, ±$10 for high volume)

**Q2:** Manager approval threshold for refunds? (Proposed: $100)

**Q3:** Expense approval threshold? (Proposed: $500)

**Q4:** Include CashMovement entity in v1.0 or defer to v1.1? (Recommendation: Defer - ExpectedBalance calculable from Payment+Expense queries)

**Q5:** Bank deposit tracking detailed or simple? (Proposed: Simple - just amount and date on CashDrawer)

---

## Conclusion

**Billing Model Updated:** 4 entities → **7 entities** (6 mandatory for v1.0, 1 optional)

**Mandatory for v1.0:**
1. Service
2. Invoice
3. InvoiceLineItem
4. Payment (updated with drawer fields)
5. **CashDrawer** (NEW)
6. **Expense** (NEW)

**Optional (defer to v1.1):**
7. CashMovement (granular audit trail)

**Evidence-Based:** All patterns derived from OpenMRS Cashier module, OpenEMR ar_session, and CARE PaymentReconciliation.

**Next Steps:**
1. Update `05-domain-model.md` Section 5 with CashDrawer and Expense entities
2. Update `04-workflows.md` Section 5 to reference drawer operations
3. Create `07-database-design.md` with SQL DDL for all 7 entities
4. Create `11-backend-api-specification.md` with drawer management endpoints

---

*This document defines the complete financial management system for ClinicCare. All workflows are evidence-based and testable. Implementation must match these specifications exactly unless changes are explicitly approved and documented.*
