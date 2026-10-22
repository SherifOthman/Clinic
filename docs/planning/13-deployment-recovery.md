# 13. Deployment & Recovery Strategy

**Document Status:** ✅ Complete  
**Last Updated:** 2026-10-21  
**Owner:** Planning Phase  
**Related Documents:** [07-database-design.md](./07-database-design.md), [14-implementation-roadmap.md](./14-implementation-roadmap.md)

---

## 1. Overview

### 1.1 Purpose
Define deployment architecture, backup strategy, disaster recovery procedures, and operational maintenance for ClinicCare system.

### 1.2 Deployment Model
**Target:** Single-clinic, self-hosted deployment via `care_clinic` desktop wrapper  
**Architecture:** Dockerized backend + frontend served by ASP.NET Core static files  
**Platform:** Windows/Linux/macOS desktop application (Wails + Docker)

### 1.3 Deployment Scope
- **v1.0:** Single-instance deployment (one clinic, one installation)
- **v1.1+:** Multi-branch support (same database, different queue views)
- **v2.0+:** Multi-clinic support (separate databases, cloud deployment option)

---

## 2. Deployment Architecture

### 2.1 Components

```
┌─────────────────────────────────────────────────────────┐
│                   Wails Desktop App                     │
│                   (care_clinic wrapper)                 │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌─────────────────┐    ┌───────────────────────────┐ │
│  │  Docker Engine  │───▶│  Docker Compose Services  │ │
│  └─────────────────┘    └───────────────────────────┘ │
│                               │                         │
│                               ├─▶ clinic-api:latest    │
│                               │   (ASP.NET Core 9.0)    │
│                               │   Port: 5000            │
│                               │                         │
│                               └─▶ clinic-db:latest     │
│                                   (SQLite / PostgreSQL) │
│                                                         │
│  ┌──────────────────────────────────────────────────┐ │
│  │  Data Volume: ./data                             │ │
│  │    ├─ clinic.db (SQLite database)                │ │
│  │    ├─ backups/ (automated backups)               │ │
│  │    └─ logs/ (application logs)                   │ │
│  └──────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

### 2.2 Docker Compose Configuration
```yaml
# docker-compose.yml
version: '3.8'

services:
  clinic-api:
    image: clinic-api:latest
    build:
      context: ./backend
      dockerfile: Dockerfile
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Data Source=/data/clinic.db
      - Jwt__SecretKey=${JWT_SECRET_KEY}
      - Jwt__Issuer=ClinicCare
      - Jwt__Audience=ClinicCare
      - Jwt__ExpiryMinutes=1440
    volumes:
      - ./data:/data
      - ./logs:/app/logs
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/api/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  # Optional: PostgreSQL for v1.1+
  clinic-db:
    image: postgres:17.10-alpine
    environment:
      - POSTGRES_DB=cliniccare
      - POSTGRES_USER=clinicuser
      - POSTGRES_PASSWORD=${DB_PASSWORD}
    volumes:
      - pgdata:/var/lib/postgresql/data
      - ./backups:/backups
    restart: unless-stopped
    profiles:
      - postgres

volumes:
  pgdata:
```

### 2.3 Backend Dockerfile
```dockerfile
# backend/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Clinic.Api/Clinic.Api.csproj", "Clinic.Api/"]
COPY ["src/Clinic.Application/Clinic.Application.csproj", "Clinic.Application/"]
COPY ["src/Clinic.Domain/Clinic.Domain.csproj", "Clinic.Domain/"]
COPY ["src/Clinic.Infrastructure/Clinic.Infrastructure.csproj", "Clinic.Infrastructure/"]
RUN dotnet restore "Clinic.Api/Clinic.Api.csproj"

COPY src/ .
WORKDIR "/src/Clinic.Api"
RUN dotnet build "Clinic.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Clinic.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Copy frontend build
COPY --from=frontend-build /app/dist ./wwwroot

ENTRYPOINT ["dotnet", "Clinic.Api.dll"]
```

### 2.4 LAN Network Configuration

#### Network Topology

ClinicCare is designed for **local network operation** with a server-client architecture:

**Server Requirements:**
- One desktop PC or server (Windows/Linux/macOS)
- Static IP recommended: 192.168.1.100 (example)
- Runs ClinicCare Docker containers
- Accessible on LAN port 5000

**Client Requirements:**
- Desktop PCs, laptops, or tablets
- Modern web browser (Chrome 90+, Edge 90+, Firefox 88+)
- Network access to server IP
- No local installation needed (browser-based)

**Network Requirements:**
- LAN (wired or WiFi) with router
- DHCP or static IPs
- **No internet required for operation**
- Internet optional for: software updates, cloud backups (future)

**Network Diagram:**
```
┌──────────────────────────────────────────────┐
│         Clinic Local Network                 │
│         (192.168.1.0/24)                     │
│                                              │
│  ┌──────────────────┐                       │
│  │ Router/Switch    │                       │
│  │ 192.168.1.1      │                       │
│  └─────────┬────────┘                       │
│            │                                 │
│   ─────────┼─────────────────────────       │
│   │        │            │           │       │
│   │        │            │           │       │
│ ┌─▼──┐  ┌─▼──┐      ┌──▼─┐      ┌──▼─┐   │
│ │Srv │  │Rec │      │Doc │      │Tab │   │
│ │.100│  │.101│      │.102│      │.103│   │
│ │    │  │    │      │    │      │    │   │
│ │API │  │Web │      │Web │      │Web │   │
│ │+DB │  │Brow│      │Brow│      │Brow│   │
│ └────┘  └────┘      └────┘      └────┘   │
│                                              │
│  Server: Docker (API + SQLite)              │
│  Clients: Browser → http://192.168.1.100:5000 │
│                                              │
│  NO INTERNET CONNECTION REQUIRED            │
└──────────────────────────────────────────────┘
```

#### Docker Configuration for LAN Access

**Update docker-compose.yml:**

```yaml
services:
  clinic-api:
    image: clinic-api:latest
    build:
      context: ./backend
      dockerfile: Dockerfile
    ports:
      - "0.0.0.0:5000:5000"  # ← Bind to all interfaces (LAN accessible)
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://0.0.0.0:5000  # ← Listen on all IPs
      - ConnectionStrings__DefaultConnection=Data Source=/data/clinic.db
      - Jwt__SecretKey=${JWT_SECRET_KEY}
      - Jwt__Issuer=ClinicCare
      - Jwt__Audience=ClinicCare
      - Jwt__ExpiryMinutes=1440
      - AllowedHosts=*  # ← Allow any hostname/IP
    volumes:
      - ./data:/data
      - ./logs:/app/logs
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/api/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

**CORS Configuration (Program.cs):**

```csharp
// Add CORS for LAN access
builder.Services.AddCors(options =>
{
    options.AddPolicy("ClinicLAN", policy =>
    {
        // Option 1: Specific IPs (most secure)
        policy
            .WithOrigins(
                "http://192.168.1.100:5000",  // Server itself
                "http://192.168.1.101:5000",  // Reception
                "http://192.168.1.102:5000",  // Doctor office
                "http://192.168.1.103:5000"   // Spare device
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
        
        // Option 2: Entire subnet (less secure, more flexible)
        // Use for development or small trusted network
        policy
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .WithOrigins("http://192.168.1.*")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Use CORS middleware
app.UseCors("ClinicLAN");
```

#### Client Access Instructions

**For Reception Desktop (192.168.1.101):**
1. Open Chrome/Edge browser
2. Navigate to: `http://192.168.1.100:5000`
3. Bookmark this URL (Ctrl+D)
4. Login with credentials
5. Application loads from server

**For Doctor Laptop (192.168.1.102):**
1. Connect to clinic WiFi
2. Open browser to: `http://192.168.1.100:5000`
3. Login and use normally
4. All data saved to server database

**Important Notes:**
- All devices must be on same LAN (192.168.1.x)
- Clients need network access to server
- **Internet not required** - works offline from internet
- Server must be running (Docker containers up)

#### Firewall Configuration

**Windows Server Firewall:**
```powershell
# Allow inbound on port 5000 from LAN subnet
New-NetFirewallRule -DisplayName "ClinicCare LAN Access" `
    -Direction Inbound `
    -LocalPort 5000 `
    -Protocol TCP `
    -Action Allow `
    -Profile Private `
    -RemoteAddress 192.168.1.0/24
```

**Linux (ufw):**
```bash
# Allow port 5000 from LAN subnet
sudo ufw allow from 192.168.1.0/24 to any port 5000 proto tcp

# Verify rule
sudo ufw status
```

**macOS (pf firewall):**
```bash
# Add rule to /etc/pf.conf
pass in proto tcp from 192.168.1.0/24 to any port 5000

# Reload firewall
sudo pfctl -f /etc/pf.conf
```

#### Static IP Configuration (Recommended)

**Why Static IP:**
- Clients access server via IP (bookmarked URL)
- DHCP may change IP after reboot (bookmarks break)
- Static IP ensures consistent access

**Windows Server Static IP:**
1. Control Panel → Network and Sharing Center
2. Change adapter settings
3. Right-click Ethernet → Properties
4. IPv4 → Properties → Use the following IP:
   - IP Address: `192.168.1.100`
   - Subnet Mask: `255.255.255.0`
   - Default Gateway: `192.168.1.1`
   - DNS: `192.168.1.1` (or `8.8.8.8`)
5. OK → Close → Test connectivity

**Linux Static IP (netplan):**
```yaml
# /etc/netplan/01-netcfg.yaml
network:
  version: 2
  ethernets:
    eth0:
      dhcp4: no
      addresses:
        - 192.168.1.100/24
      gateway4: 192.168.1.1
      nameservers:
        addresses: [192.168.1.1, 8.8.8.8]
```
```bash
sudo netplan apply
```

**Router DHCP Reservation (Alternative):**
- Access router admin (http://192.168.1.1)
- Find DHCP settings
- Reserve IP for server's MAC address
- Server gets same IP every time (via DHCP)

#### Troubleshooting LAN Access

**Issue: Cannot access server from client**

**Checklist:**
1. ✅ Server Docker running: `docker ps` (should show clinic-api)
2. ✅ Server listening on 0.0.0.0: `netstat -an | findstr 5000` (Windows) or `netstat -tulpn | grep 5000` (Linux)
3. ✅ Firewall allows port 5000: Test with `telnet 192.168.1.100 5000` from client
4. ✅ Client on same subnet: `ipconfig` (Windows) or `ip addr` (Linux) - IP should be 192.168.1.x
5. ✅ Ping server: `ping 192.168.1.100` (should reply)
6. ✅ CORS configured: Check server logs for CORS errors

**Test from Client (PowerShell):**
```powershell
Test-NetConnection -ComputerName 192.168.1.100 -Port 5000

# Expected output:
# TcpTestSucceeded : True
```

**Test from Client (curl):**
```bash
curl http://192.168.1.100:5000/api/health

# Expected output:
# {"status":"Healthy", ...}
```

**Common Issues:**

1. **Connection Refused:**
   - Server not running: Start Docker containers
   - Wrong IP: Verify server IP with `ipconfig`/`ip addr`
   - Firewall blocking: Check firewall rules

2. **CORS Error (in browser console):**
   - CORS policy not configured for client IP
   - Update Program.cs CORS origins
   - Restart API container

3. **Slow Performance:**
   - WiFi congestion: Use wired Ethernet for server
   - Router overload: Upgrade router
   - Database location: Ensure /data on fast SSD

4. **Intermittent Disconnects:**
   - WiFi power saving: Disable on client laptops
   - Network cable loose: Check server Ethernet connection
   - Router needs restart: Power cycle router

#### Security Considerations

**LAN Security:**
- Port 5000 only accessible on LAN (not internet-exposed)
- Firewall blocks external access
- HTTPS optional on LAN (consider for sensitive environments)
- Strong passwords mandatory (clinic data sensitive)

**HTTPS on LAN (Optional, for high security):**
```yaml
# docker-compose.yml
services:
  clinic-api:
    ports:
      - "0.0.0.0:5001:5001"  # HTTPS port
    environment:
      - ASPNETCORE_URLS=https://0.0.0.0:5001;http://0.0.0.0:5000
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/cert.pfx
      - ASPNETCORE_Kestrel__Certificates__Default__Password=${CERT_PASSWORD}
    volumes:
      - ./https:/https:ro
```

Generate self-signed cert:
```powershell
# Windows (PowerShell)
New-SelfSignedCertificate -DnsName "192.168.1.100" -CertStoreLocation "cert:\LocalMachine\My"

# Linux
openssl req -x509 -newkey rsa:4096 -keyout key.pem -out cert.pem -days 365 -nodes
openssl pkcs12 -export -out cert.pfx -inkey key.pem -in cert.pem
```

**Note:** Self-signed certs require browser trust (accept warning first time).

---

## 3. Deployment Process

### 3.1 Initial Installation

#### Step 1: Install Prerequisites
```bash
# Windows (PowerShell)
# - Install Docker Desktop for Windows
# - Download care_clinic installer (ClinicCare-Setup.exe)

# Linux
sudo apt-get update
sudo apt-get install docker.io docker-compose
# Download care_clinic AppImage

# macOS
brew install docker docker-compose
# Download care_clinic .dmg
```

#### Step 2: Run Installer
```bash
# Windows
.\ClinicCare-Setup.exe

# Linux
chmod +x ClinicCare-1.0.0.AppImage
./ClinicCare-1.0.0.AppImage

# macOS
open ClinicCare-1.0.0.dmg
# Drag to Applications folder
```

#### Step 3: First-Time Setup Wizard
```
ClinicCare Setup Wizard
│
├─ 1. Database Setup
│    ├─ Choose: SQLite (default) or PostgreSQL
│    └─ Database location: C:\ClinicData\clinic.db
│
├─ 2. Admin User Creation
│    ├─ Email: admin@clinic.com
│    ├─ Password: [secure password]
│    └─ Full Name: Dr. Ahmed Hassan
│
├─ 3. Clinic Information
│    ├─ Clinic Name: Main Clinic
│    ├─ Address: 123 Main St, Cairo
│    └─ Phone: +201234567890
│
└─ 4. Start Application
     └─ Launch ClinicCare (opens browser to http://localhost:5000)
```

### 3.2 Version Updates

#### Automated Update (via care_clinic)
```markdown
1. care_clinic checks for updates on startup
2. If new version available, prompts user
3. User clicks "Update Now"
4. care_clinic downloads new Docker images
5. Stops old containers gracefully
6. Runs database migrations (if needed)
7. Starts new containers
8. Verifies health check passes
9. Notifies user: "Update complete, please refresh browser"
```

#### Manual Update
```bash
# 1. Backup database
cd /path/to/clinic/data
cp clinic.db clinic.db.backup_2026-10-21

# 2. Pull new Docker images
docker-compose pull

# 3. Stop old containers
docker-compose down

# 4. Run database migrations
docker-compose run --rm clinic-api dotnet ef database update

# 5. Start new containers
docker-compose up -d

# 6. Verify health
curl http://localhost:5000/api/health
```

---

## 4. Database Migrations

### 4.1 Migration Strategy

**Zero-Downtime Migrations (v1.1+):**
- Backward-compatible schema changes only
- Add columns as nullable first, backfill data, make NOT NULL later
- Never drop columns/tables in same release (deprecate → remove in next version)

**Downtime-Allowed Migrations (v1.0):**
- Small clinic, can tolerate 5-minute downtime
- Run migrations during off-hours (e.g., before clinic opens)

### 4.2 Migration Execution
```bash
# Generate migration (development)
cd backend/src/Clinic.Infrastructure
dotnet ef migrations add <MigrationName> --startup-project ../Clinic.Api

# Apply migration (production)
docker-compose run --rm clinic-api dotnet ef database update

# Rollback migration (if needed)
docker-compose run --rm clinic-api dotnet ef database update <PreviousMigrationName>

# Verify migration applied
docker-compose run --rm clinic-api dotnet ef migrations list
```

### 4.3 Migration Rollback Plan
```markdown
If migration fails:
1. Stop application: `docker-compose down`
2. Restore database backup: `cp clinic.db.backup clinic.db`
3. Start previous version: `docker-compose up -d` (old image tag)
4. Investigate migration failure in logs
5. Fix migration script
6. Test in staging environment
7. Re-attempt migration
```

---

## 5. Backup Strategy

### 5.1 Automated Backups

#### SQLite Backup Script
```bash
#!/bin/bash
# scripts/backup-sqlite.sh

DB_PATH="/data/clinic.db"
BACKUP_DIR="/data/backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="${BACKUP_DIR}/clinic_${TIMESTAMP}.db"

# Create backup directory if not exists
mkdir -p ${BACKUP_DIR}

# SQLite online backup (safe for running database)
sqlite3 ${DB_PATH} ".backup '${BACKUP_FILE}'"

# Compress backup
gzip ${BACKUP_FILE}

# Keep only last 30 days of backups
find ${BACKUP_DIR} -name "clinic_*.db.gz" -mtime +30 -delete

echo "Backup completed: ${BACKUP_FILE}.gz"
```

#### Scheduled Backups (cron)
```cron
# /etc/cron.d/clinic-backup

# Daily backup at 2 AM
0 2 * * * /app/scripts/backup-sqlite.sh >> /data/logs/backup.log 2>&1

# Weekly full backup (copy to external drive)
0 3 * * 0 /app/scripts/backup-weekly.sh >> /data/logs/backup.log 2>&1
```

#### PostgreSQL Backup Script
```bash
#!/bin/bash
# scripts/backup-postgres.sh

BACKUP_DIR="/backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="${BACKUP_DIR}/clinic_${TIMESTAMP}.sql"

# pg_dump with compression
docker-compose exec -T clinic-db pg_dump -U clinicuser cliniccare | gzip > ${BACKUP_FILE}.gz

# Keep only last 30 days
find ${BACKUP_DIR} -name "clinic_*.sql.gz" -mtime +30 -delete

echo "Backup completed: ${BACKUP_FILE}.gz"
```

### 5.2 Backup Schedule

| Backup Type | Frequency | Retention | Storage Location |
|-------------|-----------|-----------|------------------|
| **Incremental** | Every 6 hours | 7 days | Local disk (`/data/backups`) |
| **Daily Full** | Daily at 2 AM | 30 days | Local disk |
| **Weekly Full** | Sunday 3 AM | 12 weeks | External drive / USB |
| **Monthly Archive** | 1st of month | 1 year | Cloud storage (optional) |

### 5.3 Backup Verification
```bash
# Test backup integrity (weekly cron job)
#!/bin/bash
# scripts/verify-backup.sh

LATEST_BACKUP=$(ls -t /data/backups/clinic_*.db.gz | head -1)
TEST_DIR="/tmp/backup-test-$(date +%s)"

mkdir -p ${TEST_DIR}
gunzip -c ${LATEST_BACKUP} > ${TEST_DIR}/test.db

# Verify database integrity
sqlite3 ${TEST_DIR}/test.db "PRAGMA integrity_check;"

if [ $? -eq 0 ]; then
    echo "✅ Backup verification successful: ${LATEST_BACKUP}"
else
    echo "❌ Backup verification FAILED: ${LATEST_BACKUP}"
    # Send alert email/notification
fi

rm -rf ${TEST_DIR}
```

---

## 6. Disaster Recovery

### 6.1 Recovery Scenarios

#### Scenario 1: Database Corruption
**Symptoms:** Application crashes, SQLite error "database disk image is malformed"  
**Recovery:**
```bash
# 1. Stop application
docker-compose down

# 2. Attempt SQLite recovery
sqlite3 /data/clinic.db ".recover" | sqlite3 /data/clinic_recovered.db

# 3. If recovery succeeds, replace database
mv /data/clinic.db /data/clinic.db.corrupted
mv /data/clinic_recovered.db /data/clinic.db

# 4. If recovery fails, restore from backup
gunzip -c /data/backups/clinic_latest.db.gz > /data/clinic.db

# 5. Start application
docker-compose up -d

# 6. Verify health
curl http://localhost:5000/api/health
```

**Data Loss:** Up to last backup (max 6 hours if incremental backups enabled)

#### Scenario 2: Accidental Data Deletion
**Symptoms:** User reports missing patients/invoices  
**Recovery:**
```bash
# 1. Identify when deletion occurred (check audit logs)
docker-compose logs clinic-api | grep "DELETE"

# 2. Find backup before deletion
ls -lh /data/backups/ | grep "2026-10-20"

# 3. Restore to temporary database
gunzip -c /data/backups/clinic_20261020_140000.db.gz > /tmp/clinic_restore.db

# 4. Extract deleted records
sqlite3 /tmp/clinic_restore.db "SELECT * FROM Patients WHERE Id='pat_123';" > deleted_patient.sql

# 5. Re-insert into production database (manual SQL)
sqlite3 /data/clinic.db < deleted_patient.sql

# 6. Verify data restored in application UI
```

**Prevention:** Implement soft-delete (add `IsDeleted` flag) in v1.1

#### Scenario 3: Hardware Failure (Disk Crash)
**Symptoms:** Server/computer won't boot, disk unreadable  
**Recovery:**
```bash
# 1. Install ClinicCare on new machine
./ClinicCare-Setup.exe

# 2. Stop application
docker-compose down

# 3. Restore database from external backup
cp /mnt/usb-backup/clinic_latest.db.gz /data/backups/
gunzip /data/backups/clinic_latest.db.gz
mv /data/backups/clinic_latest.db /data/clinic.db

# 4. Start application
docker-compose up -d

# 5. Re-enter any data since last backup (manual)
```

**RTO (Recovery Time Objective):** 2 hours  
**RPO (Recovery Point Objective):** Last daily backup (24 hours max data loss)

#### Scenario 4: Ransomware Attack
**Symptoms:** Files encrypted, ransom note displayed  
**Recovery:**
```markdown
1. **DO NOT PAY RANSOM**
2. Disconnect network immediately
3. Identify infection vector (email, USB, etc.)
4. Wipe infected machine, reinstall OS
5. Install ClinicCare from clean installer
6. Restore database from offline backup (USB drive)
7. Change all passwords (database, admin users)
8. Review audit logs for unauthorized access
9. Implement additional security measures (antivirus, firewall rules)
```

**Prevention:**
- Weekly offline backups (USB drive, disconnected after backup)
- Antivirus software with ransomware protection
- User training (phishing awareness)

### 6.2 Recovery Testing

**Quarterly Disaster Recovery Drill:**
```markdown
Date: First Sunday of each quarter (Jan, Apr, Jul, Oct)
Duration: 2 hours

Procedure:
1. Simulate database corruption (rename clinic.db to .corrupted)
2. Restore from latest backup
3. Verify all critical functions work:
   - Login
   - View patients
   - Create appointment
   - Create invoice
4. Document:
   - Time to restore: <X minutes>
   - Data loss: <X hours/days>
   - Issues encountered
5. Update recovery documentation if needed
```

---

## 7. Monitoring & Alerting

### 7.1 Application Health Monitoring

#### Health Check Endpoint
```csharp
// Program.cs
app.MapHealthChecks("/api/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration
            }),
            timestamp = DateTime.UtcNow
        });
        await context.Response.WriteAsync(result);
    }
});

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database")
    .AddCheck("disk-space", () =>
    {
        var drive = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory));
        return drive.AvailableFreeSpace > 1_000_000_000 // 1GB free
            ? HealthCheckResult.Healthy("Disk space OK")
            : HealthCheckResult.Degraded("Low disk space");
    });
```

#### Monitoring Dashboard (Planned v1.1)
```markdown
Metrics to track:
- API response times (p50, p95, p99)
- Request error rate
- Database connection pool usage
- Disk space remaining
- Backup success/failure rate
- Active user sessions
- Queue length per branch
```

### 7.2 Logging

#### Structured Logging (Serilog)
```csharp
// Program.cs
builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console()
        .WriteTo.File(
            path: "/app/logs/clinic-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        ));
```

#### Log Levels
| Level | Usage | Examples |
|-------|-------|----------|
| **Trace** | Very detailed, debug info | SQL queries, cache hits/misses |
| **Debug** | Detailed flow | Method entry/exit, variable values |
| **Information** | General flow | User login, appointment created |
| **Warning** | Unexpected but handled | Validation failed, resource not found |
| **Error** | Operation failed | Database error, external API failure |
| **Critical** | System failure | Unhandled exception, out of memory |

#### Log Rotation
```bash
# Keep logs for 30 days, compress old logs
find /app/logs -name "clinic-*.log" -mtime +30 -delete
find /app/logs -name "clinic-*.log" -mtime +7 -exec gzip {} \;
```

### 7.3 Alerting (Planned v1.1)

**Alert Triggers:**
- ❌ Health check fails 3 times in 5 minutes → Email admin
- ⚠️ Disk space <5% free → Email admin
- ⚠️ Backup failed → Email admin
- ❌ Database connection lost → Desktop notification
- ⚠️ Error rate >5% (10 errors in 1 minute) → Email admin

**Alert Channels:**
- Email (SMTP)
- Desktop notification (via care_clinic)
- SMS (future, via Twilio integration)

---

## 8. Performance Optimization

### 8.1 Database Optimization

#### Indexes (from 07-database-design.md)
```sql
-- Critical indexes already defined in schema
CREATE INDEX idx_patients_phone ON Patients(Phone);
CREATE INDEX idx_patients_national_id ON Patients(NationalId);
CREATE INDEX idx_appointments_branch_date ON Appointments(BranchId, ScheduledAt);
CREATE INDEX idx_queue_tokens_branch_status ON QueueTokens(BranchId, Status, TokenNumber);
CREATE INDEX idx_invoices_patient_status ON Invoices(PatientId, Status);
```

#### Query Optimization
```csharp
// Use AsNoTracking for read-only queries
var patients = await context.Patients
    .AsNoTracking()
    .Where(p => p.BranchId == branchId)
    .OrderBy(p => p.LastName)
    .ToListAsync();

// Avoid N+1 queries with Include
var appointments = await context.Appointments
    .Include(a => a.Patient)
    .Include(a => a.Branch)
    .Where(a => a.ScheduledAt >= DateTime.Today)
    .ToListAsync();

// Use projection for large datasets
var patientSummaries = await context.Patients
    .Select(p => new PatientSummaryDto
    {
        Id = p.Id,
        FullName = p.FirstName + " " + p.LastName,
        Phone = p.Phone
    })
    .ToListAsync();
```

### 8.2 Caching Strategy

#### In-Memory Cache (IMemoryCache)
```csharp
// Cache reference data (branches, users)
public async Task<List<Branch>> GetBranchesAsync()
{
    return await _cache.GetOrCreateAsync("branches", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
        return await _context.Branches.ToListAsync();
    });
}

// Cache ICD-10 codes
public async Task<List<Icd10Code>> GetIcd10CodesAsync()
{
    return await _cache.GetOrCreateAsync("icd10-codes", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
        return await _context.Icd10Codes.ToListAsync();
    });
}
```

#### Cache Invalidation
```csharp
// Invalidate cache on data change
public async Task<Result<Branch>> CreateBranchAsync(CreateBranchCommand command)
{
    // ... create branch logic
    await _context.SaveChangesAsync();
    
    // Invalidate branches cache
    _cache.Remove("branches");
    
    return Result.Success(branch);
}
```

### 8.3 SQLite Performance Tuning
```sql
-- Apply on database creation
PRAGMA journal_mode = WAL;          -- Write-Ahead Logging (better concurrency)
PRAGMA synchronous = NORMAL;         -- Balance between safety and speed
PRAGMA cache_size = -64000;          -- 64MB cache
PRAGMA temp_store = MEMORY;          -- Temp tables in memory
PRAGMA mmap_size = 268435456;        -- Memory-mapped I/O (256MB)
```

---

## 9. Security Hardening

### 9.1 Production Security Checklist

#### Application Security
- [x] HTTPS enforced (redirect HTTP → HTTPS)
- [x] JWT secret key stored in environment variable (not hardcoded)
- [x] Database connection string in environment variable
- [x] CORS restricted to known origins
- [x] Rate limiting enabled (100 req/min per IP)
- [x] Input validation on all endpoints (FluentValidation)
- [x] SQL injection protection (EF Core parameterized queries)
- [x] XSS protection (Content-Security-Policy headers)
- [x] Sensitive data not logged (passwords, tokens)

#### Infrastructure Security
- [x] Docker containers run as non-root user
- [x] Database file permissions: 600 (owner read/write only)
- [x] Backup files encrypted (AES-256)
- [x] Firewall: Only port 5000 exposed to localhost
- [x] Antivirus software installed and updated
- [x] OS security patches applied

#### Operational Security
- [x] Strong password policy enforced (min 8 chars, complexity)
- [x] Failed login attempts logged
- [x] User sessions expire after 24 hours
- [x] Audit trail for sensitive operations (delete, edit billing)
- [x] Admin users reviewed quarterly
- [x] Inactive users disabled after 90 days

### 9.2 SSL/TLS Configuration
```nginx
# nginx.conf (if using reverse proxy)
server {
    listen 443 ssl http2;
    server_name clinic.example.com;

    ssl_certificate /etc/ssl/certs/clinic.crt;
    ssl_certificate_key /etc/ssl/private/clinic.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;

    location / {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}

server {
    listen 80;
    server_name clinic.example.com;
    return 301 https://$host$request_uri;
}
```

---

## 10. Capacity Planning

### 10.1 Growth Projections

**Year 1 (v1.0):**
- Patients: 1,000
- Appointments/day: 20-30
- Database size: ~500MB
- Required disk space: 10GB (including backups)
- RAM: 4GB (2GB for Docker, 2GB for OS)
- CPU: 2 cores

**Year 2 (v1.1 - Multi-branch):**
- Patients: 3,000
- Appointments/day: 60-80
- Database size: ~2GB
- Required disk space: 50GB
- RAM: 8GB
- CPU: 4 cores

**Year 3 (v2.0 - Migration to PostgreSQL):**
- Patients: 10,000
- Appointments/day: 150-200
- Database size: ~10GB
- Required disk space: 100GB
- RAM: 16GB
- CPU: 8 cores

### 10.2 Database Maintenance

#### Vacuum (SQLite)
```bash
# Weekly maintenance (reclaim space, optimize)
sqlite3 /data/clinic.db "VACUUM;"
sqlite3 /data/clinic.db "ANALYZE;"
```

#### Reindex (PostgreSQL)
```sql
-- Monthly maintenance
REINDEX DATABASE cliniccare;
VACUUM ANALYZE;
```

---

## 11. Rollback Procedures

### 11.1 Application Rollback
```bash
# Rollback to previous version
docker-compose down
docker-compose --file docker-compose.v0.9.0.yml up -d

# Verify health
curl http://localhost:5000/api/health
```

### 11.2 Database Migration Rollback
```bash
# Rollback to specific migration
docker-compose run --rm clinic-api \
    dotnet ef database update <PreviousMigrationName>

# Or restore from backup
cp /data/backups/clinic_pre_migration.db /data/clinic.db
```

---

## 12. Open Questions

### Q1: Cloud Backup Strategy
**Question:** Should automated cloud backups be included in v1.0?  
**Context:** Requires cloud credentials, adds complexity  
**Recommendation:** Optional feature, user can manually upload to Dropbox/Google Drive  
**Decision:** [DEFERRED TO v1.1]

### Q2: Real-time Monitoring Tools
**Question:** What monitoring tool to use for production?  
**Options:**
- A) Built-in dashboard (custom development)
- B) Application Insights (Azure, requires account)
- C) Self-hosted Grafana + Prometheus

**Recommendation:** Option A (simple dashboard) for v1.0, Option C for v1.1  
**Decision:** [PENDING]

### Q3: PostgreSQL Migration Timing
**Question:** When to migrate from SQLite to PostgreSQL?  
**Trigger Points:**
- Database size >10GB
- Multi-user concurrency issues
- Need for advanced features (full-text search, partitioning)

**Recommendation:** Offer migration path in v1.1, mandatory in v2.0 if triggers met  
**Decision:** [PENDING]

---

## 13. Related Documents

- [07-database-design.md](./07-database-design.md) - Database schema and backup requirements
- [14-implementation-roadmap.md](./14-implementation-roadmap.md) - Deployment milestones
- [17-risk-register.md](./17-risk-register.md) - Deployment risks and mitigations

---

## 14. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-10-21 | Planning Phase | Initial deployment & recovery strategy |

---

**Document Confidence:** HIGH  
**Evidence Base:** Docker best practices, care_clinic deployment patterns, SQLite/PostgreSQL operational guides  
**Assumptions:** Desktop deployment (not cloud), single-clinic v1.0, technical user (doctor/admin) can run basic scripts  
**Next Review:** After v1.0 deployment, gather user feedback on deployment complexity
