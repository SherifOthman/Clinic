# 17. Risk Register

**Document Status:** ✅ Complete  
**Last Updated:** 2026-10-21  
**Owner:** Planning Phase  
**Related Documents:** [14-implementation-roadmap.md](./14-implementation-roadmap.md), [13-deployment-recovery.md](./13-deployment-recovery.md)

---

## 1. Overview

### 1.1 Purpose
Identify, assess, and track risks to ClinicCare project success, with mitigation strategies and contingency plans.

### 1.2 Risk Categories
- **Technical:** Architecture, performance, security
- **Schedule:** Timeline, dependencies, resource availability
- **Quality:** Bugs, test coverage, data integrity
- **Operational:** Deployment, backup, user adoption
- **External:** Regulatory, vendor dependencies

### 1.3 Risk Assessment Matrix

| Probability | Impact | Priority |
|-------------|--------|----------|
| **High (70-100%)** | Critical | 🔴 P1 - Immediate action |
| **High (70-100%)** | High | 🔴 P1 - Immediate action |
| **High (70-100%)** | Medium | 🟠 P2 - Plan mitigation |
| **Medium (30-70%)** | Critical | 🔴 P1 - Immediate action |
| **Medium (30-70%)** | High | 🟠 P2 - Plan mitigation |
| **Medium (30-70%)** | Medium | 🟡 P3 - Monitor |
| **Low (<30%)** | Critical | 🟠 P2 - Plan mitigation |
| **Low (<30%)** | High | 🟡 P3 - Monitor |
| **Low (<30%)** | Medium/Low | 🟢 P4 - Accept |

---

## 2. Technical Risks

### RISK-001: Database Corruption (SQLite)
**Category:** Technical  
**Probability:** Low (20%)  
**Impact:** Critical (data loss, system unavailable)  
**Priority:** 🟠 P2

**Description:**  
SQLite database file corruption due to disk failure, improper shutdown, or file system issues could result in data loss or system unavailability.

**Symptoms:**
- "Database disk image is malformed" error
- Application crashes on startup
- Unable to read patient/invoice data

**Impact Analysis:**
- Complete loss of patient data if backups fail
- Clinic operations halted until recovery
- Potential HIPAA violation if data unrecoverable

**Mitigation Strategies:**
1. **Prevention:**
   - Enable Write-Ahead Logging (WAL mode) for better concurrency and crash resistance
   - Automated daily backups (retention: 30 days)
   - Weekly backup verification (integrity check)
   - Use reliable storage (SSD preferred over HDD)
   - Implement graceful shutdown (SIGTERM handler)

2. **Detection:**
   - Weekly automated integrity checks (`PRAGMA integrity_check`)
   - Health check endpoint monitors database connectivity
   - Backup success/failure alerts

3. **Response:**
   - Restore from most recent backup (RPO: 24 hours max)
   - SQLite recovery commands (`sqlite3 .recover`)
   - Escalation path: notify admin, attempt recovery, restore from backup

**Contingency Plan:**
- If corruption detected: Stop application → Attempt SQLite recovery → If fails, restore from backup
- If backup also corrupted: Restore from weekly backup (RPO: 7 days)
- Document data loss window, inform users to re-enter data

**Status:** 🟢 Mitigated (backup strategy in place)  
**Owner:** DevOps  
**Review Date:** After v1.0 deployment

---

### RISK-002: Performance Degradation at Scale
**Category:** Technical  
**Probability:** Medium (50%)  
**Impact:** High (user frustration, workflow slowdown)  
**Priority:** 🟠 P2

**Description:**  
As patient database grows (>10,000 patients, >100,000 encounters), queries may slow down, impacting user experience.

**Symptoms:**
- API response time >1s (target: <500ms)
- Patient search takes >3s
- Queue dashboard refresh slow

**Impact Analysis:**
- User frustration, decreased productivity
- Potential workflow bottlenecks (queue delays)
- May require urgent optimization or PostgreSQL migration

**Mitigation Strategies:**
1. **Prevention:**
   - Database indexes on critical fields (phone, national ID, appointment date)
   - Query optimization (avoid N+1 queries, use pagination)
   - Caching for reference data (branches, ICD-10 codes)
   - Load testing after Sprint 4 (simulate 10K patients, 100 concurrent users)

2. **Detection:**
   - Performance monitoring (API response times logged)
   - Automated alerts if p95 response time >1s
   - User feedback (task completion time surveys)

3. **Response:**
   - Profile slow queries (EF Core query logging)
   - Add missing indexes
   - Optimize queries (projection, AsNoTracking)
   - If SQLite limits reached, migrate to PostgreSQL

**Contingency Plan:**
- If performance unacceptable: Accelerate PostgreSQL migration (planned v1.1)
- Temporary workaround: Increase pagination limits, disable real-time updates

**Status:** 🟡 Monitoring (load testing planned Sprint 4)  
**Owner:** Backend Developer  
**Review Date:** Week 8 (post-Sprint 4)

---

### RISK-003: Security Vulnerability Exploitation
**Category:** Technical  
**Probability:** Medium (40%)  
**Impact:** Critical (data breach, HIPAA violation)  
**Priority:** 🔴 P1

**Description:**  
Unpatched security vulnerabilities in dependencies (NuGet packages, npm packages) or application code could be exploited, leading to unauthorized access or data breach.

**Symptoms:**
- Dependabot alerts (high/critical vulnerabilities)
- OWASP scan failures
- Unauthorized access attempts in logs

**Impact Analysis:**
- Patient data breach (HIPAA violation, legal liability)
- Ransomware attack (data encrypted, operations halted)
- Reputational damage, loss of trust

**Mitigation Strategies:**
1. **Prevention:**
   - Dependency scanning (Dependabot, Snyk)
   - OWASP Top 10 security checks in CI/CD
   - Code review with security checklist
   - Input validation (FluentValidation)
   - Parameterized queries (EF Core, no raw SQL)
   - JWT token expiry (24 hours)
   - Rate limiting (100 req/min per IP)
   - HTTPS enforced in production

2. **Detection:**
   - Automated security scans on every commit
   - Failed login attempts logged and monitored
   - Audit trail for sensitive operations

3. **Response:**
   - Patch high/critical vulnerabilities within 48 hours
   - Rotate JWT secret keys if breach suspected
   - Incident response plan: isolate system, assess impact, notify users

**Contingency Plan:**
- If breach detected: Disconnect from network → Assess scope → Restore from clean backup → Change all passwords → Notify affected users

**Status:** 🟡 Active monitoring (Dependabot enabled)  
**Owner:** Security Lead / Backend Developer  
**Review Date:** Continuous (weekly dependency check)

---

### RISK-004: Integration Issues Between Frontend and Backend
**Category:** Technical  
**Probability:** High (60%)  
**Impact:** Medium (delays, rework)  
**Priority:** 🟠 P2

**Description:**  
Mismatched API contracts, incorrect data formats, or missing fields between frontend and backend could cause integration failures during development.

**Symptoms:**
- Frontend errors: "Cannot read property X of undefined"
- API returns 400 Bad Request unexpectedly
- Data not displaying correctly in UI

**Impact Analysis:**
- Development delays (1-2 days per issue)
- Increased rework, frustration
- Potential missed sprint goals

**Mitigation Strategies:**
1. **Prevention:**
   - API specification written before implementation (11-backend-api-specification.md)
   - Shared TypeScript types generated from API (OpenAPI code generation)
   - Integration tests run on every commit
   - Contract testing (Pact or similar)

2. **Detection:**
   - Integration tests fail in CI/CD
   - Frontend mock data validated against API spec
   - Swagger documentation reviewed

3. **Response:**
   - Fix API contract mismatch immediately (blocker priority)
   - Update API spec and regenerate types
   - Add test case to prevent regression

**Contingency Plan:**
- If critical integration issue blocks sprint: Timebox fix to 4 hours, escalate if not resolved, adjust sprint scope

**Status:** 🟢 Mitigated (API spec complete, integration tests planned)  
**Owner:** Full-stack Developer  
**Review Date:** Weekly (during sprint)

---

### RISK-005: Floating-Point Errors in Financial Calculations
**Category:** Technical  
**Probability:** Low (15%)  
**Impact:** Critical (incorrect billing, financial loss)  
**Priority:** 🟠 P2

**Description:**  
Using `float` or `double` types for money calculations could introduce rounding errors, leading to incorrect invoice totals or cash drawer balances.

**Symptoms:**
- Invoice total off by $0.01
- Cash drawer discrepancy unexplained
- Payment balance due incorrect after multiple payments

**Impact Analysis:**
- Financial errors (overpayment, underpayment)
- Cash drawer reconciliation failures
- Loss of user trust, potential legal issues

**Mitigation Strategies:**
1. **Prevention:**
   - Use `decimal` type for ALL money values (C#, database)
   - Unit tests with edge cases ($0.01, $999,999.99, $0.33 × 3)
   - Code review checklist: verify decimal usage
   - Extensive financial calculation tests (TEST-014-MONEY, TEST-015-MONEY, TEST-018-MONEY)

2. **Detection:**
   - Unit tests catch rounding errors
   - Manual QA: verify invoice calculations with sample data

3. **Response:**
   - If error found: Fix immediately (blocker)
   - Audit existing invoices/payments for errors
   - Correct affected records

**Contingency Plan:**
- If production financial error: Halt billing operations → Fix bug → Audit all affected transactions → Manually correct discrepancies

**Status:** 🟢 Mitigated (decimal type enforced, tests planned)  
**Owner:** Backend Developer  
**Review Date:** Sprint 4 (before billing release)

---

## 3. Schedule Risks

### RISK-006: Scope Creep (Mid-Sprint Feature Requests)
**Category:** Schedule  
**Probability:** High (70%)  
**Impact:** High (missed deadlines, incomplete features)  
**Priority:** 🔴 P1

**Description:**  
Users or stakeholders request new features or changes mid-sprint, disrupting planned work and delaying delivery.

**Symptoms:**
- "Can we add this quick feature?"
- Sprint goals not met
- Developer context switching

**Impact Analysis:**
- Sprint deliverables incomplete
- Timeline delays (v1.0 pushed from Week 8 to Week 10+)
- Developer burnout, decreased morale

**Mitigation Strategies:**
1. **Prevention:**
   - Strict change control process
   - Sprint scope locked after sprint planning
   - Product backlog for new requests
   - Weekly stakeholder demos to gather feedback early

2. **Detection:**
   - Feature requests logged in backlog (not inserted into sprint)
   - Velocity tracking (if velocity drops, investigate scope creep)

3. **Response:**
   - Politely decline mid-sprint requests, add to backlog
   - If truly urgent (P0 blocker), negotiate scope reduction (remove lower-priority task)

**Contingency Plan:**
- If scope creep impacts timeline: Re-prioritize features, defer P1/P2 features to v1.1, deliver minimum viable v1.0

**Status:** 🟡 Active risk (ongoing)  
**Owner:** Project Manager / Product Owner  
**Review Date:** Weekly sprint planning

---

### RISK-007: Developer Unavailability (Illness, Vacation)
**Category:** Schedule  
**Probability:** Medium (50%)  
**Impact:** High (timeline delays, knowledge loss)  
**Priority:** 🟠 P2

**Description:**  
Single developer (or small team) unavailable due to illness, vacation, or unexpected personal issues could halt development.

**Symptoms:**
- Developer out of office
- Sprint tasks not progressing
- Knowledge gaps when developer returns

**Impact Analysis:**
- Timeline delays (1 week absence = 1 week delay)
- Knowledge loss if no documentation
- Pressure to work during illness/vacation

**Mitigation Strategies:**
1. **Prevention:**
   - Comprehensive documentation (all planning docs complete)
   - Code comments for complex logic
   - Pair programming on critical features
   - Plan vacations around sprint boundaries

2. **Detection:**
   - Developer communicates absence in advance
   - Daily standup reveals blocked tasks

3. **Response:**
   - Adjust sprint scope if absence mid-sprint
   - Onboard backup developer if available
   - Document progress before absence

**Contingency Plan:**
- If unexpected absence >1 week: Pause sprint, adjust timeline, communicate delays to stakeholders

**Status:** 🟡 Monitoring (document, document, document)  
**Owner:** Team Lead  
**Review Date:** Monthly

---

### RISK-008: Technical Debt Accumulation
**Category:** Schedule, Quality  
**Probability:** High (60%)  
**Impact:** Medium (future slowdown, bugs)  
**Priority:** 🟠 P2

**Description:**  
Pressure to deliver features quickly leads to shortcuts (skipped tests, incomplete error handling, hardcoded values), accumulating technical debt that slows future development.

**Symptoms:**
- Decreasing test coverage
- "TODO" comments proliferating
- Increasing bug count
- Slower velocity in later sprints

**Impact Analysis:**
- Future feature development slower
- More bugs in production
- Eventual major refactor needed (costly)

**Mitigation Strategies:**
1. **Prevention:**
   - Allocate 20% of each sprint to refactoring/debt reduction
   - Definition of Done includes tests and documentation
   - Code review enforces quality standards
   - No "temporary hacks" without tracking ticket

2. **Detection:**
   - Code coverage trends downward
   - Velocity decreases over time
   - Increasing "TODO" count in codebase

3. **Response:**
   - Dedicated "tech debt sprint" after v1.0 release
   - Pay down debt incrementally (boy scout rule: leave code better than found)

**Contingency Plan:**
- If debt becomes unmanageable: Schedule debt sprint (1 week), communicate delay, refactor critical areas

**Status:** 🟡 Active (enforce Definition of Done)  
**Owner:** Tech Lead  
**Review Date:** After each sprint (velocity review)

---

## 4. Quality Risks

### RISK-009: Insufficient Test Coverage
**Category:** Quality  
**Probability:** Medium (40%)  
**Impact:** High (production bugs, data corruption)  
**Priority:** 🟠 P2

**Description:**  
Time pressure or incomplete test strategy leads to low test coverage, allowing bugs to reach production.

**Symptoms:**
- Coverage <70% (below target)
- Critical paths not tested
- Bugs found in production that tests should have caught

**Impact Analysis:**
- Production bugs impact user experience
- Data corruption (invoices, payments)
- Emergency hotfixes required, disrupting schedule

**Mitigation Strategies:**
1. **Prevention:**
   - Enforce coverage thresholds in CI/CD (block merge if <80% backend, <70% frontend)
   - Test-first development for critical features (TDD)
   - Code review checks for test coverage

2. **Detection:**
   - Coverage reports generated on every commit
   - Critical path tests identified and tracked

3. **Response:**
   - If coverage drops below threshold: Write tests before merging PR
   - Add tests retroactively for uncovered code

**Contingency Plan:**
- If critical bug in production: Hotfix within 24 hours, write regression test, deploy

**Status:** 🟢 Mitigated (coverage thresholds enforced)  
**Owner:** QA Lead / Developer  
**Review Date:** Continuous (every PR)

---

### RISK-010: Data Integrity Issues (Orphaned Records, Inconsistent State)
**Category:** Quality  
**Probability:** Medium (35%)  
**Impact:** Critical (data corruption, incorrect billing)  
**Priority:** 🔴 P1

**Description:**  
Bugs in business logic or database constraints could lead to orphaned records (e.g., QueueToken without Appointment), inconsistent state (e.g., encounter completed but queue token still in-progress), or incorrect data.

**Symptoms:**
- Orphaned queue tokens
- Invoice total ≠ sum of line items
- Encounter in-progress but patient not in queue

**Impact Analysis:**
- Data corruption requires manual correction
- Incorrect billing (financial loss or overcharge)
- User confusion, loss of trust

**Mitigation Strategies:**
1. **Prevention:**
   - Database foreign key constraints enforce relationships
   - Transaction boundaries ensure atomic operations
   - Unit tests for state transitions (e.g., queue token status)
   - Data validation in application layer (FluentValidation)

2. **Detection:**
   - Data consistency checks (scheduled job, weekly)
   - Unit tests for invariants (e.g., "completed encounter has invoice")
   - Audit logs track changes

3. **Response:**
   - Fix bug immediately if data corruption detected
   - Write migration script to correct existing data
   - Add test to prevent regression

**Contingency Plan:**
- If widespread data corruption: Restore from backup (last clean state), replay transactions if possible, manual correction if needed

**Status:** 🟡 Monitoring (foreign keys enforced, tests planned)  
**Owner:** Backend Developer  
**Review Date:** Sprint 4 (billing features)

---

## 5. Operational Risks

### RISK-011: Backup Failure (Backups Not Created or Corrupted)
**Category:** Operational  
**Probability:** Low (25%)  
**Impact:** Critical (data loss if disaster occurs)  
**Priority:** 🟠 P2

**Description:**  
Automated backup script fails silently, or backups are created but corrupted, leaving no valid restore point.

**Symptoms:**
- Backup files not appearing in /data/backups
- Backup verification fails (integrity check)
- Restore test fails

**Impact Analysis:**
- No valid backup during disaster (RPO = unknown)
- Data loss if production database fails
- HIPAA violation (backup required)

**Mitigation Strategies:**
1. **Prevention:**
   - Automated daily backups (cron job)
   - Backup success/failure logged
   - Weekly backup verification (restore test)
   - External backup copy (USB drive, weekly)

2. **Detection:**
   - Backup monitoring script checks for new backups daily
   - Alert if no backup in 24 hours
   - Verification script tests restore process

3. **Response:**
   - If backup fails: Investigate immediately, manually trigger backup
   - If backup corrupted: Attempt recovery, use previous backup

**Contingency Plan:**
- If all backups fail during disaster: Data loss accepted (RPO = last successful backup), re-enter recent data manually

**Status:** 🟡 Monitoring (backup script written, verification planned)  
**Owner:** DevOps  
**Review Date:** Weekly (backup verification)

---

### RISK-012: User Adoption Failure (Users Resist New System)
**Category:** Operational  
**Probability:** Medium (40%)  
**Impact:** High (project failure, wasted investment)  
**Priority:** 🟠 P2

**Description:**  
Users (doctors, receptionists) resist adopting new system, preferring manual processes or previous system, leading to low usage and project failure.

**Symptoms:**
- Low system usage (<50% of staff)
- Users continue manual records (paper, Excel)
- Negative feedback, complaints

**Impact Analysis:**
- Project ROI not realized
- Wasted development effort
- Potential abandonment of system

**Mitigation Strategies:**
1. **Prevention:**
   - Involve users early (feedback during planning, demos)
   - User training sessions before launch
   - User guide (step-by-step workflows)
   - Address pain points from previous system
   - Incremental rollout (pilot group first)

2. **Detection:**
   - Usage analytics (logins per day, feature usage)
   - User surveys (satisfaction, ease of use)
   - Support ticket volume

3. **Response:**
   - Identify adoption blockers (usability issues, missing features)
   - Provide additional training, support
   - Iterate based on user feedback

**Contingency Plan:**
- If adoption <50% after 1 month: Conduct user interviews, identify blockers, prioritize fixes, extend pilot period

**Status:** 🟡 Active risk (user feedback loop planned)  
**Owner:** Product Owner  
**Review Date:** Post-launch (Week 9)

---

### RISK-013: Deployment Failure (Application Won't Start in Production)
**Category:** Operational  
**Probability:** Low (20%)  
**Impact:** High (system unavailable, clinic operations halted)  
**Priority:** 🟠 P2

**Description:**  
Deployment to production fails due to configuration issues, missing dependencies, or environment differences between dev and production.

**Symptoms:**
- Docker container crashes on startup
- Health check fails (HTTP 503)
- Database connection errors

**Impact Analysis:**
- System unavailable, clinic cannot operate
- Rollback required, delays deployment
- User frustration, loss of confidence

**Mitigation Strategies:**
1. **Prevention:**
   - Test deployment process in staging environment
   - Docker ensures environment consistency
   - Health checks validate deployment success
   - Deployment checklist (13-deployment-recovery.md)

2. **Detection:**
   - Health check fails post-deployment
   - Error logs show startup failure

3. **Response:**
   - Rollback to previous version immediately
   - Investigate failure, fix issue
   - Re-deploy with fix

**Contingency Plan:**
- If deployment fails: Rollback to previous version (docker-compose down; docker-compose up with old tag), investigate, fix, retry

**Status:** 🟢 Mitigated (deployment process documented)  
**Owner:** DevOps  
**Review Date:** Before v1.0 release

---

## 6. External Risks

### RISK-014: Regulatory Compliance (HIPAA, Data Privacy)
**Category:** External  
**Probability:** Low (20%)  
**Impact:** Critical (legal liability, fines)  
**Priority:** 🟠 P2

**Description:**  
ClinicCare may not meet HIPAA or local data privacy regulations, leading to non-compliance, fines, or legal action.

**Symptoms:**
- Audit finds non-compliant practices
- Data breach reported to authorities
- Lack of audit trail for sensitive operations

**Impact Analysis:**
- Legal fines ($100-$50,000 per violation)
- Reputational damage
- System shutdown until compliance achieved

**Mitigation Strategies:**
1. **Prevention:**
   - Audit trail for all sensitive operations (create, update, delete patients/invoices)
   - Access control (role-based, branch-level)
   - Data encryption (JWT tokens, passwords hashed with BCrypt)
   - HTTPS enforced in production
   - Backup encryption (future P2)
   - User access logs (who accessed what, when)

2. **Detection:**
   - Compliance checklist reviewed
   - Security audit (external consultant, future)

3. **Response:**
   - If non-compliant: Address issues immediately, document remediation

**Contingency Plan:**
- If audit finds violations: Halt operations, fix issues, re-audit before resuming

**Status:** 🟡 Monitoring (security features implemented, audit pending)  
**Owner:** Legal / Security Lead  
**Review Date:** Before v1.0 release (compliance review)

---

### RISK-015: Third-Party Dependency Failure (Docker, ASP.NET Core)
**Category:** External  
**Probability:** Low (10%)  
**Impact:** High (system unavailable, forced upgrade)  
**Priority:** 🟡 P3

**Description:**  
Critical bug in third-party dependency (Docker, ASP.NET Core, React) forces urgent upgrade or workaround.

**Symptoms:**
- Security advisory for critical vulnerability
- Bug in Docker breaks deployment
- ASP.NET Core bug causes crashes

**Impact Analysis:**
- System unavailable until fix/upgrade
- Development delays (testing new version)
- Potential breaking changes in upgrade

**Mitigation Strategies:**
1. **Prevention:**
   - Pin dependency versions (no auto-update)
   - Monitor release notes, security advisories
   - Test upgrades in staging before production

2. **Detection:**
   - Dependabot alerts
   - Community forums, GitHub issues

3. **Response:**
   - Evaluate impact of upgrade
   - Test in staging
   - Schedule upgrade during maintenance window

**Contingency Plan:**
- If critical vulnerability: Emergency upgrade within 48 hours, test thoroughly, deploy hotfix

**Status:** 🟢 Low priority (stable dependencies chosen)  
**Owner:** DevOps  
**Review Date:** Quarterly (dependency review)

---

## 7. Risk Monitoring & Review

### 7.1 Risk Review Cadence
- **Weekly:** Review active risks (P1, P2) in team meeting
- **Sprint End:** Update risk status, identify new risks
- **Monthly:** Full risk register review, adjust priorities

### 7.2 Risk Status Tracking
- 🟢 **Mitigated:** Risk reduced to acceptable level
- 🟡 **Monitoring:** Active monitoring, mitigation in progress
- 🔴 **Active:** High priority, requires immediate action
- ⚫ **Closed:** Risk no longer applicable, mitigation successful

### 7.3 Risk Dashboard (Example)
```
╔══════════════════════════════════════════════════════════╗
║                   Risk Dashboard                         ║
╠══════════════════════════════════════════════════════════╣
║ Total Risks: 15                                          ║
║ Active Risks (P1): 2  🔴                                 ║
║ Monitoring (P2): 8    🟡                                 ║
║ Low Priority (P3): 3  🟢                                 ║
║ Closed: 2             ⚫                                  ║
║                                                          ║
║ Top 3 Risks:                                             ║
║ 1. RISK-006: Scope creep (P1) 🔴                        ║
║ 2. RISK-003: Security vulnerability (P1) 🔴             ║
║ 3. RISK-010: Data integrity issues (P1) 🟡              ║
║                                                          ║
║ Overdue Risk Reviews: 0                                  ║
╚══════════════════════════════════════════════════════════╝
```

---

## 8. Open Questions

### Q1: Risk Contingency Budget
**Question:** Should we allocate contingency budget (time/money) for risk mitigation?  
**Context:** 10-20% contingency common in projects  
**Recommendation:** Allocate 2-week buffer after v1.0 (Week 10) for unforeseen issues  
**Decision:** [PENDING]

### Q2: Risk Owner Responsibilities
**Question:** Should risk owners have formal accountability (e.g., weekly status updates)?  
**Recommendation:** Yes, risk owners report status in sprint retrospectives  
**Decision:** [PENDING]

### Q3: External Risk Assessment
**Question:** Should we hire external consultant for security/compliance audit?  
**Recommendation:** Yes, before v1.0 release (budget permitting)  
**Decision:** [PENDING]

---

## 9. Related Documents

- [14-implementation-roadmap.md](./14-implementation-roadmap.md) - Schedule risks mapped to sprints
- [13-deployment-recovery.md](./13-deployment-recovery.md) - Operational risk mitigation (backup, recovery)
- [12-testing-strategy.md](./12-testing-strategy.md) - Quality risk mitigation (test coverage)
- [09-security-authorization.md](./09-security-authorization.md) - Security risk mitigation (RBAC, HIPAA)

---

## 10. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-10-21 | Planning Phase | Initial risk register (15 risks identified) |

---

**Document Confidence:** HIGH  
**Evidence Base:** Industry best practices (risk management), project context (small clinic, single developer)  
**Assumptions:** Risks assessed based on typical software projects, small team context  
**Next Review:** Weekly (active risks), after v1.0 release (full review)
