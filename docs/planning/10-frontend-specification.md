# 10. Frontend Specification

**Document Status:** ✅ Complete  
**Last Updated:** 2026-10-21  
**Owner:** Planning Phase  
**Related Documents:** [04-workflows.md](./04-workflows.md), [05-domain-model.md](./05-domain-model.md), [09-security-authorization.md](./09-security-authorization.md), [11-backend-api-specification.md](./11-backend-api-specification.md)

---

## 1. Overview

### 1.1 Purpose
Define the user interface architecture, component structure, design system, and interaction patterns for ClinicCare's React-based frontend application.

### 1.2 Technology Stack
**Framework & Libraries:**
- React 19.0+ with TypeScript 5.8+
- React Router 7.1+ for routing
- TanStack Query v5 for server state management
- Zustand 5.0+ for client state management
- React Hook Form 7.54+ with Zod validation
- Axios 1.7+ for HTTP client

**UI Components:**
- Shadcn/ui component library
- Radix UI primitives
- Tailwind CSS 4.0+ for styling
- Lucide React for icons
- date-fns 4.1+ for date handling

**Development Tools:**
- Vite 6.0+ as build tool
- ESLint + Prettier for code quality
- Vitest for unit testing
- Playwright for e2e testing

### 1.3 Design Principles
1. **Accessibility First**: WCAG 2.1 AA compliance minimum
2. **RTL Support**: Bidirectional text (Arabic/Hebrew support from care_clinic)
3. **Responsive Design**: Desktop-first, tablet support (no mobile app v1.0)
4. **Progressive Enhancement**: Core functionality works without JavaScript disabled
5. **Performance**: <3s initial load, <100ms interaction response

---

## 2. Application Architecture

### 2.1 Project Structure
```
frontend/
├── src/
│   ├── components/          # Reusable UI components
│   │   ├── ui/              # Shadcn/ui primitives
│   │   ├── forms/           # Form components with validation
│   │   ├── layouts/         # Page layouts (DashboardLayout, etc.)
│   │   └── common/          # Shared components (DataTable, SearchBox)
│   ├── features/            # Feature-based modules
│   │   ├── auth/            # Authentication
│   │   ├── patients/        # Patient management
│   │   ├── appointments/    # Scheduling
│   │   ├── queue/           # Queue management
│   │   ├── consultation/    # Clinical consultation
│   │   ├── billing/         # Billing & invoicing
│   │   └── cash-drawer/     # Cash drawer management
│   ├── hooks/               # Custom React hooks
│   ├── lib/                 # Utilities & helpers
│   │   ├── api/             # API client & endpoints
│   │   ├── validation/      # Zod schemas
│   │   └── utils/           # Helper functions
│   ├── stores/              # Zustand stores
│   ├── types/               # TypeScript types
│   ├── App.tsx              # Root component
│   └── main.tsx             # Entry point
├── public/                  # Static assets
└── tests/                   # Test files
```

### 2.2 Routing Structure
```typescript
// Route hierarchy
/login                          // Public: Login page
/dashboard                      // Private: Dashboard home
/patients
  /                             // Patient list
  /new                          // Create patient
  /:id                          // Patient profile
  /:id/edit                     // Edit patient
  /:id/history                  // Visit history
/appointments
  /                             // Calendar view
  /schedule                     // Schedule appointment
  /:id                          // Appointment details
/queue
  /                             // Queue dashboard
  /:branchId                    // Branch-specific queue
/consultation
  /:encounterId                 // Active consultation
/billing
  /invoices                     // Invoice list
  /invoices/:id                 // Invoice details
  /payments                     // Payment history
  /cash-drawer                  // Cash drawer management
/settings
  /profile                      // User profile
  /branch                       // Branch settings (Admin only)
  /users                        // User management (Admin only)
```

### 2.3 State Management Strategy

**Server State (TanStack Query):**
- Patient data
- Appointments
- Queue status
- Invoices & payments
- Medical records
- Settings & lookups

**Client State (Zustand):**
```typescript
// stores/auth.ts
interface AuthStore {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  login: (credentials: Credentials) => Promise<void>;
  logout: () => void;
}

// stores/ui.ts
interface UIStore {
  sidebarOpen: boolean;
  currentBranch: Branch | null;
  theme: 'light' | 'dark';
  locale: 'en' | 'ar' | 'he';
  toggleSidebar: () => void;
  setBranch: (branch: Branch) => void;
}

// stores/consultation.ts
interface ConsultationStore {
  activeEncounter: Encounter | null;
  draftNotes: string;
  pendingOrders: Order[];
  setActiveEncounter: (encounter: Encounter) => void;
  saveDraftNotes: (notes: string) => void;
  addOrder: (order: Order) => void;
}
```

---

## 3. Core Components

### 3.1 Layout Components

#### DashboardLayout
```typescript
// components/layouts/DashboardLayout.tsx
interface DashboardLayoutProps {
  children: React.ReactNode;
}

// Features:
// - Top navigation bar with user menu
// - Collapsible sidebar with navigation
// - Breadcrumb navigation
// - Branch selector (if user has multi-branch access)
// - Notification center
// - Quick action buttons
```

**Navigation Structure:**
- Dashboard (Home icon)
- Patients (Users icon)
- Appointments (Calendar icon)
- Queue (List icon)
- Billing (CreditCard icon)
- Reports (BarChart icon) - P2
- Settings (Settings icon)

#### ConsultationLayout
```typescript
// features/consultation/ConsultationLayout.tsx
// Specialized layout for active consultation
// - Patient info header (pinned)
// - Tabbed interface: Notes, Orders, Vitals, History
// - Quick actions sidebar
// - Timer display (consultation duration)
// - Save/Complete buttons
```

### 3.2 Form Components

#### PatientForm
```typescript
// features/patients/PatientForm.tsx
interface PatientFormData {
  // Demographics (Section 5.1 domain model)
  firstName: string;
  lastName: string;
  dateOfBirth: Date;
  gender: 'male' | 'female' | 'other';
  nationalId?: string;
  phone: string;
  email?: string;
  address?: string;
  emergencyContact?: {
    name: string;
    phone: string;
    relationship: string;
  };
  // Medical info
  bloodGroup?: string;
  allergies?: string[];
  chronicConditions?: string[];
}

// Validation: Zod schema
const patientSchema = z.object({
  firstName: z.string().min(1, "Required").max(100),
  lastName: z.string().min(1, "Required").max(100),
  dateOfBirth: z.date().max(new Date(), "Cannot be future date"),
  gender: z.enum(['male', 'female', 'other']),
  nationalId: z.string().optional().refine(validateNationalId),
  phone: z.string().regex(/^\+?[0-9]{10,15}$/, "Invalid phone"),
  email: z.string().email().optional().or(z.literal('')),
  // ... rest of validation
});
```

#### AppointmentForm
```typescript
// features/appointments/AppointmentForm.tsx
interface AppointmentFormData {
  patientId: string;
  branchId: string;
  scheduledAt: Date;
  type: 'walkin' | 'scheduled';
  reason?: string;
  notes?: string;
}

// Features:
// - Patient search with autocomplete
// - Branch selector (filtered by user access)
// - Date/time picker with availability check
// - Conflict detection (existing appointments)
// - Queue position preview
```

#### InvoiceForm
```typescript
// features/billing/InvoiceForm.tsx
interface InvoiceFormData {
  encounterId?: string;
  patientId: string;
  items: InvoiceItem[];
  discount?: {
    type: 'percentage' | 'fixed';
    value: number;
  };
  notes?: string;
}

interface InvoiceItem {
  description: string;
  quantity: number;
  unitPrice: number;
  taxRate: number;
}

// Features:
// - Dynamic item rows (add/remove)
// - Service/product lookup
// - Real-time total calculation
// - Discount application
// - Tax calculation per item
// - Print preview
```

### 3.3 Data Display Components

#### DataTable
```typescript
// components/common/DataTable.tsx
interface DataTableProps<T> {
  columns: ColumnDef<T>[];
  data: T[];
  loading?: boolean;
  pagination?: {
    page: number;
    pageSize: number;
    total: number;
    onPageChange: (page: number) => void;
  };
  sorting?: {
    field: string;
    direction: 'asc' | 'desc';
    onSortChange: (field: string) => void;
  };
  filters?: FilterConfig[];
  actions?: RowAction<T>[];
}

// Based on TanStack Table v8
// Features:
// - Server-side pagination
// - Multi-column sorting
// - Column filtering
// - Row selection
// - Bulk actions
// - Export to CSV/PDF
```

#### PatientCard
```typescript
// features/patients/PatientCard.tsx
// Displays patient summary in lists
// - Avatar with initials
// - Name, age, gender
// - National ID
// - Last visit date
// - Quick actions (View, Edit, Schedule)
// - Status indicators (Active encounter, Unpaid invoices)
```

#### QueueCard
```typescript
// features/queue/QueueCard.tsx
// Displays queue token in queue view
// - Token number (large display)
// - Patient name
// - Appointment time
// - Wait duration
// - Status badge (waiting, called, in-progress)
// - Actions (Call Next, Mark No-Show, Skip)
```

### 3.4 Search & Filter Components

#### PatientSearch
```typescript
// features/patients/PatientSearch.tsx
// Global patient search (in navbar)
// - Autocomplete with debounce (300ms)
// - Search by: name, phone, national ID
// - Recent patients dropdown
// - Quick create patient button
// - Navigate to patient profile on select

// API: GET /api/patients/search?q={query}&limit=10
```

#### DateRangePicker
```typescript
// components/common/DateRangePicker.tsx
// Reusable date range selector
// - Preset ranges (Today, This Week, This Month, Custom)
// - Calendar popup
// - Quick navigation (arrows for prev/next period)
// - Used in: reports, billing history, appointment calendar
```

---

## 4. Feature Modules

### 4.1 Authentication Module

**Components:**
- `LoginForm` - Email/password authentication
- `PrivateRoute` - Protected route wrapper
- `PermissionGuard` - Role-based component visibility

**Hooks:**
```typescript
// hooks/useAuth.ts
const { user, isAuthenticated, login, logout } = useAuth();

// hooks/usePermissions.ts
const { can, hasRole } = usePermissions();
// Usage: can('create:patient'), hasRole('Admin')
```

**Flows:**
1. Login → Store JWT → Fetch user profile → Redirect to dashboard
2. Token refresh → Automatic silent refresh before expiry
3. Logout → Clear token → Clear cache → Redirect to login
4. Session timeout → Show warning modal → Auto-logout after 15 minutes idle

### 4.2 Patient Management Module

**Pages:**
- `PatientListPage` - Searchable, paginated patient list
- `PatientProfilePage` - Patient details with tabs (Info, History, Documents)
- `PatientFormPage` - Create/edit patient

**Key Components:**
- `PatientForm` - Demographics form with validation
- `PatientHistory` - Timeline of visits and encounters
- `PatientDocuments` - Uploaded files (P2 feature)

**Hooks:**
```typescript
const { data: patients, isLoading } = usePatients({
  page: 1,
  search: 'john',
  sortBy: 'lastName'
});

const { mutate: createPatient } = useCreatePatient();
const { data: patient } = usePatient(patientId);
```

### 4.3 Appointment & Queue Module

**Pages:**
- `AppointmentCalendarPage` - Weekly calendar view
- `QueueDashboardPage` - Real-time queue status per branch
- `ScheduleAppointmentPage` - Appointment booking form

**Key Components:**
- `AppointmentCalendar` - Full-calendar with drag-drop (P2)
- `QueueBoard` - Kanban-style queue visualization
- `QueueControls` - Call next, skip, mark no-show

**Real-time Updates:**
```typescript
// WebSocket or polling for queue updates
useQueueSubscription(branchId, {
  onTokenCalled: (token) => showNotification(`Token ${token.number} called`),
  onTokenCompleted: () => refreshQueue(),
});
```

**Flows:**
1. Schedule appointment → Check availability → Create appointment → Add to queue
2. Walk-in patient → Quick register → Add to queue
3. Call next → Update queue status → Notify patient (future P2)
4. Complete consultation → Mark token done → Move to next

### 4.4 Consultation Module

**Pages:**
- `ConsultationPage` - Active consultation workspace

**Key Components:**
- `ConsultationHeader` - Patient info, timer, actions
- `VitalsForm` - Quick vitals entry (BP, temp, pulse, weight)
- `ClinicalNotesEditor` - Rich text editor for SOAP notes
- `OrderSection` - Medication/lab/imaging orders
- `DiagnosisSection` - ICD-10 code search and selection

**Workspace Layout:**
```
┌─────────────────────────────────────────────────────┐
│ Patient Header | Timer: 00:15:23 | [Save] [Complete]│
├─────────────────────────────────────────────────────┤
│ Tabs: [Vitals] [Notes] [Orders] [Diagnosis] [Hist] │
├─────────────────────────────────────────────────────┤
│                                                     │
│  Tab Content Area                                   │
│                                                     │
│                                                     │
└─────────────────────────────────────────────────────┘
```

**Autosave:**
- Debounced autosave (5 seconds after typing stops)
- Draft stored in Zustand + localStorage
- Restore draft on page reload
- Clear draft on consultation complete

### 4.5 Billing Module

**Pages:**
- `InvoiceListPage` - All invoices with filters
- `InvoiceDetailPage` - Invoice view with payment history
- `CreateInvoicePage` - Invoice creation form
- `PaymentHistoryPage` - Payment transactions
- `CashDrawerPage` - Cash drawer management

**Key Components:**
- `InvoiceForm` - Line items, discounts, taxes
- `PaymentForm` - Payment recording (cash, card, insurance)
- `CashDrawerControl` - Open/close drawer, reconciliation
- `InvoicePrintView` - Print-optimized invoice template

**Cash Drawer Workflow:**
```typescript
// 1. Open Drawer (start of shift)
const { mutate: openDrawer } = useOpenCashDrawer();
openDrawer({ branchId, openingBalance, openedBy });

// 2. Record Payments
const { mutate: recordPayment } = useRecordPayment();
recordPayment({ invoiceId, amount, method: 'cash', cashDrawerId });

// 3. Record Expense
const { mutate: recordExpense } = useRecordExpense();
recordExpense({ description, amount, category, cashDrawerId });

// 4. Close Drawer (end of shift)
const { mutate: closeDrawer } = useCloseCashDrawer();
closeDrawer({ cashDrawerId, closingBalance, closedBy });
// System calculates expected vs actual, flags discrepancies
```

### 4.6 Settings Module

**Pages:**
- `ProfileSettingsPage` - User profile, password change
- `BranchSettingsPage` - Branch configuration (Admin only)
- `UserManagementPage` - User CRUD (Admin only)
- `SystemSettingsPage` - General settings (Admin only)

**Key Components:**
- `UserForm` - User creation with role assignment
- `BranchForm` - Branch details, working hours
- `RolePermissionMatrix` - Visual permission editor (Admin only)

---

## 5. API Integration

### 5.1 API Client Configuration
```typescript
// lib/api/client.ts
import axios from 'axios';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor: Add JWT token
apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().token;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response interceptor: Handle 401 (token expired)
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      useAuthStore.getState().logout();
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);
```

### 5.2 Query Hooks Pattern
```typescript
// features/patients/api.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

// Query keys factory
export const patientKeys = {
  all: ['patients'] as const,
  lists: () => [...patientKeys.all, 'list'] as const,
  list: (filters: PatientFilters) => [...patientKeys.lists(), filters] as const,
  details: () => [...patientKeys.all, 'detail'] as const,
  detail: (id: string) => [...patientKeys.details(), id] as const,
};

// Fetch patients list
export const usePatients = (filters: PatientFilters) => {
  return useQuery({
    queryKey: patientKeys.list(filters),
    queryFn: () => apiClient.get('/api/patients', { params: filters }),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

// Fetch single patient
export const usePatient = (id: string) => {
  return useQuery({
    queryKey: patientKeys.detail(id),
    queryFn: () => apiClient.get(`/api/patients/${id}`),
    enabled: !!id,
  });
};

// Create patient mutation
export const useCreatePatient = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreatePatientDTO) => apiClient.post('/api/patients', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: patientKeys.lists() });
    },
  });
};

// Update patient mutation
export const useUpdatePatient = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdatePatientDTO }) =>
      apiClient.put(`/api/patients/${id}`, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: patientKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: patientKeys.lists() });
    },
  });
};
```

### 5.3 Error Handling
```typescript
// lib/api/errors.ts
export class ApiError extends Error {
  constructor(
    public statusCode: number,
    public message: string,
    public errors?: Record<string, string[]>
  ) {
    super(message);
  }
}

// Global error handler
export const handleApiError = (error: unknown): ApiError => {
  if (axios.isAxiosError(error) && error.response) {
    const { status, data } = error.response;
    return new ApiError(
      status,
      data.message || 'An error occurred',
      data.errors
    );
  }
  return new ApiError(500, 'Network error');
};

// Usage in components
const { mutate, error } = useCreatePatient();
if (error) {
  // Display error.message in toast
  // Display error.errors in form field errors
}
```

---

## 6. Design System

### 6.1 Color Palette
```css
/* Tailwind config - colors.ts */
:root {
  /* Brand colors */
  --primary: 212 100% 48%;        /* Blue #0066CC */
  --primary-foreground: 0 0% 100%;
  --secondary: 210 40% 96%;       /* Light blue-gray */
  --secondary-foreground: 222 47% 11%;
  
  /* Semantic colors */
  --success: 142 71% 45%;         /* Green */
  --warning: 38 92% 50%;          /* Orange */
  --error: 0 84% 60%;             /* Red */
  --info: 199 89% 48%;            /* Cyan */
  
  /* Neutral colors */
  --background: 0 0% 100%;
  --foreground: 222 47% 11%;
  --muted: 210 40% 96%;
  --muted-foreground: 215 16% 47%;
  --border: 214 32% 91%;
  
  /* Status colors */
  --status-active: 142 71% 45%;   /* Green - active encounter */
  --status-waiting: 38 92% 50%;   /* Orange - waiting in queue */
  --status-completed: 210 40% 60%; /* Gray - completed */
  --status-cancelled: 0 0% 45%;   /* Dark gray */
  --status-pending: 199 89% 48%;  /* Blue - pending payment */
}
```

### 6.2 Typography
```css
/* Font families */
font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
font-family-mono: 'Fira Code', Consolas, monospace;

/* Type scale */
text-xs: 0.75rem;    /* 12px - labels, badges */
text-sm: 0.875rem;   /* 14px - body text, form inputs */
text-base: 1rem;     /* 16px - default body */
text-lg: 1.125rem;   /* 18px - card titles */
text-xl: 1.25rem;    /* 20px - section headers */
text-2xl: 1.5rem;    /* 24px - page titles */
text-3xl: 1.875rem;  /* 30px - dashboard hero */

/* Font weights */
font-normal: 400;
font-medium: 500;
font-semibold: 600;
font-bold: 700;
```

### 6.3 Spacing & Layout
```css
/* Spacing scale (Tailwind default) */
0: 0px;
1: 0.25rem;   /* 4px */
2: 0.5rem;    /* 8px */
3: 0.75rem;   /* 12px */
4: 1rem;      /* 16px */
6: 1.5rem;    /* 24px */
8: 2rem;      /* 32px */
12: 3rem;     /* 48px */

/* Layout constraints */
max-w-xs: 20rem;      /* 320px - forms, cards */
max-w-md: 28rem;      /* 448px - modals */
max-w-lg: 32rem;      /* 512px - large forms */
max-w-screen-xl: 1280px; /* Dashboard content */
```

### 6.4 Component Variants

**Button:**
```tsx
// Primary action
<Button variant="default">Save Patient</Button>

// Secondary action
<Button variant="outline">Cancel</Button>

// Destructive action
<Button variant="destructive">Delete</Button>

// Ghost (minimal)
<Button variant="ghost">View Details</Button>

// Sizes
<Button size="sm">Small</Button>
<Button size="default">Default</Button>
<Button size="lg">Large</Button>
```

**Badge:**
```tsx
// Status badges
<Badge variant="success">Paid</Badge>
<Badge variant="warning">Pending</Badge>
<Badge variant="destructive">Overdue</Badge>
<Badge variant="secondary">Cancelled</Badge>
```

**Card:**
```tsx
<Card>
  <CardHeader>
    <CardTitle>Patient Information</CardTitle>
    <CardDescription>Personal and contact details</CardDescription>
  </CardHeader>
  <CardContent>
    {/* Card body */}
  </CardContent>
  <CardFooter>
    <Button>Save Changes</Button>
  </CardFooter>
</Card>
```

---

## 7. Accessibility

### 7.1 WCAG 2.1 AA Requirements

**Perceivable:**
- ✅ Color contrast ratio ≥4.5:1 for normal text, ≥3:1 for large text
- ✅ Alt text for all images and icons
- ✅ Form labels explicitly associated with inputs
- ✅ Error messages clearly identify problematic fields

**Operable:**
- ✅ Full keyboard navigation (Tab, Shift+Tab, Enter, Escape, Arrow keys)
- ✅ Focus indicators visible (2px solid outline)
- ✅ No keyboard traps
- ✅ Skip links for main content

**Understandable:**
- ✅ Consistent navigation across pages
- ✅ Clear error messages with recovery suggestions
- ✅ Form validation on submit, inline errors on blur
- ✅ Confirmation dialogs for destructive actions

**Robust:**
- ✅ Semantic HTML (nav, main, article, section, header, footer)
- ✅ ARIA landmarks and labels
- ✅ Valid HTML (no duplicate IDs)
- ✅ Screen reader tested (NVDA, JAWS)

### 7.2 Keyboard Shortcuts

| Shortcut | Action | Scope |
|----------|--------|-------|
| `Ctrl+K` | Global patient search | Global |
| `Ctrl+N` | New patient | Patient list |
| `Ctrl+Shift+A` | Schedule appointment | Global |
| `Ctrl+Shift+Q` | Open queue dashboard | Global |
| `Ctrl+S` | Save current form/note | Forms, consultation |
| `Escape` | Close modal/dialog | Modals |
| `?` | Show keyboard shortcuts help | Global |

### 7.3 Screen Reader Landmarks
```html
<!-- Dashboard structure -->
<body>
  <header role="banner" aria-label="Site header">
    <nav aria-label="Primary navigation">...</nav>
  </header>
  
  <main role="main" aria-label="Main content">
    <section aria-labelledby="page-title">
      <h1 id="page-title">Patient List</h1>
      ...
    </section>
  </main>
  
  <aside role="complementary" aria-label="Quick actions">...</aside>
  
  <footer role="contentinfo" aria-label="Site footer">...</footer>
</body>
```

---

## 8. Internationalization (i18n)

### 8.1 Supported Languages (v1.0)
- English (en) - Default
- Arabic (ar) - RTL support
- Hebrew (he) - RTL support

### 8.2 Implementation
```typescript
// lib/i18n.ts
import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

i18n
  .use(initReactI18next)
  .init({
    resources: {
      en: { translation: enTranslations },
      ar: { translation: arTranslations },
      he: { translation: heTranslations },
    },
    lng: 'en',
    fallbackLng: 'en',
    interpolation: {
      escapeValue: false, // React already escapes
    },
  });

// Usage in components
import { useTranslation } from 'react-i18next';

const { t, i18n } = useTranslation();
<h1>{t('patients.title')}</h1>
<Button onClick={() => i18n.changeLanguage('ar')}>العربية</Button>
```

### 8.3 RTL Support
```tsx
// RTL direction change
useEffect(() => {
  document.dir = i18n.dir(); // 'ltr' or 'rtl'
}, [i18n.language]);

// Tailwind RTL utilities
<div className="ms-4"> {/* margin-inline-start (left in LTR, right in RTL) */}
<div className="me-4"> {/* margin-inline-end */}
<div className="ps-4"> {/* padding-inline-start */}
```

---

## 9. Performance Optimization

### 9.1 Code Splitting
```typescript
// Route-based code splitting
const PatientListPage = lazy(() => import('./features/patients/PatientListPage'));
const AppointmentCalendarPage = lazy(() => import('./features/appointments/AppointmentCalendarPage'));

<Suspense fallback={<PageLoader />}>
  <Routes>
    <Route path="/patients" element={<PatientListPage />} />
    <Route path="/appointments" element={<AppointmentCalendarPage />} />
  </Routes>
</Suspense>
```

### 9.2 Image Optimization
- Use WebP format with PNG fallback
- Lazy load images below fold (`loading="lazy"`)
- Responsive images with `srcset`
- Avatar placeholders (initials) before image load

### 9.3 Caching Strategy
```typescript
// TanStack Query cache times
const cacheConfig = {
  // Frequently changing data
  queue: { staleTime: 10 * 1000, cacheTime: 30 * 1000 },         // 10s stale, 30s cache
  appointments: { staleTime: 60 * 1000, cacheTime: 5 * 60 * 1000 }, // 1m stale, 5m cache
  
  // Relatively stable data
  patients: { staleTime: 5 * 60 * 1000, cacheTime: 10 * 60 * 1000 }, // 5m stale, 10m cache
  invoices: { staleTime: 5 * 60 * 1000, cacheTime: 10 * 60 * 1000 },
  
  // Static/reference data
  branches: { staleTime: 30 * 60 * 1000, cacheTime: 60 * 60 * 1000 }, // 30m stale, 1h cache
  users: { staleTime: 15 * 60 * 1000, cacheTime: 30 * 60 * 1000 },
  icd10Codes: { staleTime: 24 * 60 * 60 * 1000, cacheTime: Infinity }, // 1 day stale, infinite cache
};
```

### 9.4 Bundle Size Targets
- Initial bundle: <300KB gzipped
- Per-route chunks: <150KB gzipped
- Total JavaScript: <1MB gzipped
- Images/assets: <500KB total per page

### 9.5 Performance Metrics
```typescript
// Web Vitals monitoring (future P2)
import { getCLS, getFID, getFCP, getLCP, getTTFB } from 'web-vitals';

getCLS(console.log); // Cumulative Layout Shift
getFID(console.log); // First Input Delay
getFCP(console.log); // First Contentful Paint
getLCP(console.log); // Largest Contentful Paint
getTTFB(console.log); // Time to First Byte

// Targets:
// LCP: <2.5s (good), <4s (needs improvement)
// FID: <100ms (good), <300ms (needs improvement)
// CLS: <0.1 (good), <0.25 (needs improvement)
```

---

## 10. Testing Strategy

### 10.1 Unit Tests (Vitest)
```typescript
// features/patients/PatientForm.test.tsx
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { PatientForm } from './PatientForm';

describe('PatientForm', () => {
  it('validates required fields', async () => {
    render(<PatientForm />);
    const submitButton = screen.getByRole('button', { name: /save/i });
    await userEvent.click(submitButton);
    
    expect(screen.getByText(/first name is required/i)).toBeInTheDocument();
    expect(screen.getByText(/last name is required/i)).toBeInTheDocument();
  });

  it('submits valid patient data', async () => {
    const onSubmit = vi.fn();
    render(<PatientForm onSubmit={onSubmit} />);
    
    await userEvent.type(screen.getByLabelText(/first name/i), 'John');
    await userEvent.type(screen.getByLabelText(/last name/i), 'Doe');
    await userEvent.type(screen.getByLabelText(/phone/i), '+1234567890');
    // ... fill other fields
    
    await userEvent.click(screen.getByRole('button', { name: /save/i }));
    
    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({
        firstName: 'John',
        lastName: 'Doe',
        phone: '+1234567890',
      }));
    });
  });
});
```

### 10.2 Integration Tests (Playwright)
```typescript
// tests/e2e/patient-workflow.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Patient Management', () => {
  test('create and view patient', async ({ page }) => {
    // Login
    await page.goto('/login');
    await page.fill('input[name="email"]', 'admin@clinic.com');
    await page.fill('input[name="password"]', 'password');
    await page.click('button[type="submit"]');
    
    // Navigate to patients
    await page.click('a[href="/patients"]');
    await expect(page).toHaveURL('/patients');
    
    // Create new patient
    await page.click('button:has-text("New Patient")');
    await page.fill('input[name="firstName"]', 'Jane');
    await page.fill('input[name="lastName"]', 'Smith');
    await page.fill('input[name="dateOfBirth"]', '1990-01-15');
    await page.selectOption('select[name="gender"]', 'female');
    await page.fill('input[name="phone"]', '+1234567890');
    await page.click('button:has-text("Save")');
    
    // Verify patient appears in list
    await expect(page.locator('text=Jane Smith')).toBeVisible();
    
    // View patient profile
    await page.click('text=Jane Smith');
    await expect(page.locator('h1:has-text("Jane Smith")')).toBeVisible();
    await expect(page.locator('text=+1234567890')).toBeVisible();
  });
});
```

### 10.3 Accessibility Tests
```typescript
// tests/a11y/forms.test.tsx
import { render } from '@testing-library/react';
import { axe, toHaveNoViolations } from 'jest-axe';
import { PatientForm } from './PatientForm';

expect.extend(toHaveNoViolations);

describe('Accessibility', () => {
  it('PatientForm has no accessibility violations', async () => {
    const { container } = render(<PatientForm />);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
```

### 10.4 Visual Regression Tests (P2)
```typescript
// tests/visual/components.spec.ts
import { test, expect } from '@playwright/test';

test('PatientCard visual snapshot', async ({ page }) => {
  await page.goto('/storybook/?path=/story/patients-patientcard--default');
  await expect(page).toHaveScreenshot('patient-card-default.png');
});
```

---

## 11. Development Workflow

### 11.1 Local Development
```bash
# Start dev server
npm run dev

# Run tests in watch mode
npm run test:watch

# Run linter
npm run lint

# Type check
npm run type-check

# Format code
npm run format
```

### 11.2 Environment Variables
```env
# .env.development
VITE_API_BASE_URL=http://localhost:5000
VITE_WS_URL=ws://localhost:5000
VITE_ENABLE_MOCK_API=false

# .env.production
VITE_API_BASE_URL=/api
VITE_WS_URL=wss://clinic.example.com
```

### 11.3 Build & Deployment
```bash
# Production build
npm run build

# Preview production build
npm run preview

# Analyze bundle size
npm run analyze

# Output:
# dist/
#   ├── index.html
#   ├── assets/
#   │   ├── index-[hash].js
#   │   ├── vendor-[hash].js
#   │   └── *.css
#   └── favicon.ico
```

---

## 12. Open Questions

### Q1: Real-time Updates for Queue
**Question:** Should queue updates use WebSockets or polling?  
**Options:**
- A) WebSocket connection for real-time updates (requires WebSocket server)
- B) Polling every 5-10 seconds (simpler, works with current REST API)
- C) Server-Sent Events (SSE) for one-way updates

**Recommendation:** Option B (polling) for v1.0, upgrade to WebSocket in v1.1 if needed.  
**Decision:** [PENDING]

### Q2: Offline Support
**Question:** Should the app work offline with local storage?  
**Context:** Desktop app (via care_clinic wrapper), likely has stable internet  
**Recommendation:** No offline support in v1.0 (adds complexity). Revisit if users request.  
**Decision:** [PENDING]

### Q3: Print Templates
**Question:** Where should print templates (invoice, prescription) be managed?  
**Options:**
- A) Hardcoded React components
- B) Server-side HTML templates (fetched via API)
- C) WYSIWYG template editor (admin feature)

**Recommendation:** Option A for v1.0 (simpler), Option C for v2.0 (customization).  
**Decision:** [PENDING]

### Q4: Multi-language Clinical Terms
**Question:** Should clinical terms (diagnoses, medications) be translated?  
**Context:** ICD-10 codes, medication names often standardized in English  
**Recommendation:** UI translated, clinical terms keep English with local display name field.  
**Decision:** [PENDING]

---

## 13. Related Documents

- [04-workflows.md](./04-workflows.md) - User workflows informing UI flows
- [05-domain-model.md](./05-domain-model.md) - Entities mapped to TypeScript types
- [09-security-authorization.md](./09-security-authorization.md) - Role-based UI permissions
- [11-backend-api-specification.md](./11-backend-api-specification.md) - API contracts consumed by frontend
- [12-testing-strategy.md](./12-testing-strategy.md) - Frontend testing requirements

---

## 14. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-10-21 | Planning Phase | Initial frontend specification |

---

**Document Confidence:** HIGH  
**Evidence Base:** Care_clinic v0.1.15 React structure, Shadcn/ui patterns, CARE frontend conventions  
**Assumptions:** Desktop-first design, no mobile app v1.0, single-tab usage (no concurrent edits)  
**Next Review:** After backend API specification complete, validate API contracts match frontend needs
