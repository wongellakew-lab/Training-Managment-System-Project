import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { roleGuard } from './guards/role.guard';
import { loginRedirectGuard } from './guards/login-redirect.guard';
import { rootRedirectGuard } from './guards/root-redirect.guard';

export const routes: Routes = [
  // Root — smart redirect based on auth state and role
  {
    path: '',
    pathMatch: 'full',
    canActivate: [rootRedirectGuard],
    loadComponent: () =>
      import('./features/login/login.component').then((m) => m.LoginComponent)
  },

  // Login — redirect away if already authenticated
  {
    path: 'login',
    canActivate: [loginRedirectGuard],
    loadComponent: () =>
      import('./features/login/login.component').then((m) => m.LoginComponent)
  },

  {
    path: 'unauthorized',
    loadComponent: () =>
      import('./features/unauthorized/unauthorized.component').then((m) => m.UnauthorizedComponent)
  },

  // ─── Admin Routes ─────────────────────────────────────────────────────────
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin'] },
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },
      {
        path: 'dashboard',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./features/admin-dashboard/admin-dashboard.component').then((m) => m.AdminDashboardComponent)
      },
      {
        path: 'courses',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./features/courses/courses.component').then((m) => m.CoursesComponent)
      },
      {
        path: 'courses/:id',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./features/course-detail/course-detail.component').then((m) => m.CourseDetailComponent)
      },
      {
        path: 'students',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./features/students/students.component').then((m) => m.StudentsComponent)
      },
      {
        path: 'instructors',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./features/instructors/instructors.component').then((m) => m.InstructorsComponent)
      },
      {
        path: 'enrollments',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./features/enrollments/enrollments.component').then((m) => m.EnrollmentsComponent)
      },
      {
        path: 'calendar',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./features/calendar/calendar.component').then((m) => m.CalendarComponent)
      },
      {
        path: 'financials',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./features/financials/financials.component').then((m) => m.FinancialsComponent)
      }
    ]
  },

  // ─── Instructor Routes ────────────────────────────────────────────────────
  {
    path: 'instructor',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Instructor', 'Admin'] },
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },
      {
        path: 'dashboard',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Instructor', 'Admin'] },
        loadComponent: () =>
          import('./features/instructor-dashboard/instructor-dashboard.component').then((m) => m.InstructorDashboardComponent)
      },
      {
        path: 'courses',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Instructor', 'Admin'] },
        loadComponent: () =>
          import('./features/courses/courses.component').then((m) => m.CoursesComponent)
      },
      {
        path: 'courses/:id',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Instructor', 'Admin'] },
        loadComponent: () =>
          import('./features/course-detail/course-detail.component').then((m) => m.CourseDetailComponent)
      }
    ]
  },

  // ─── Student Routes ───────────────────────────────────────────────────────
  {
    path: 'student',
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },
      {
        path: 'dashboard',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Student', 'Admin'] },
        loadComponent: () =>
          import('./features/student-dashboard/student-dashboard.component').then((m) => m.StudentDashboardComponent)
      },
      {
        path: 'courses',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Student', 'Admin'] },
        loadComponent: () =>
          import('./features/courses/courses.component').then((m) => m.CoursesComponent)
      },
      {
        path: 'courses/:id',
        canActivate: [authGuard],
        loadComponent: () =>
          import('./features/course-detail/course-detail.component').then((m) => m.CourseDetailComponent)
      },
      {
        path: 'enrollments',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Student', 'Admin'] },
        loadComponent: () =>
          import('./features/enrollments/enrollments.component').then((m) => m.EnrollmentsComponent)
      },
      {
        path: 'grades',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Student', 'Admin'] },
        loadComponent: () =>
          import('./features/student-dashboard/student-dashboard.component').then((m) => m.StudentDashboardComponent)
      },
      {
        path: 'certificates',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Student', 'Admin'] },
        loadComponent: () =>
          import('./features/certificate-view/certificate-view.component').then((m) => m.CertificateViewComponent)
      }
    ]
  },

  // ─── Catch-all: redirect to root (which triggers role-based redirect) ─────
  {
    path: '**',
    canActivate: [rootRedirectGuard],
    loadComponent: () =>
      import('./features/login/login.component').then((m) => m.LoginComponent)
  }
];
