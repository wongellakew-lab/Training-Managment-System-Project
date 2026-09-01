import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { roleGuard } from './guards/role.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'admin/dashboard'
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'unauthorized',
    loadComponent: () =>
      import('./features/unauthorized/unauthorized.component').then((m) => m.UnauthorizedComponent)
  },

  // Eduva Global & Admin Routes
  {
    path: 'dashboard',
    redirectTo: 'admin/dashboard',
    pathMatch: 'full'
  },
  {
    path: 'admin',
    pathMatch: 'full',
    redirectTo: 'admin/dashboard'
  },
  {
    path: 'admin/dashboard',
    loadComponent: () =>
      import('./features/admin-dashboard/admin-dashboard.component').then((m) => m.AdminDashboardComponent)
  },
  {
    path: 'calendar',
    loadComponent: () =>
      import('./features/calendar/calendar.component').then((m) => m.CalendarComponent)
  },
  {
    path: 'enrollments',
    loadComponent: () =>
      import('./features/enrollments/enrollments.component').then((m) => m.EnrollmentsComponent)
  },
  {
    path: 'courses',
    loadComponent: () =>
      import('./features/courses/courses.component').then((m) => m.CoursesComponent)
  },
  {
    path: 'admin/courses',
    loadComponent: () =>
      import('./features/courses/courses.component').then((m) => m.CoursesComponent)
  },
  {
    path: 'instructors',
    loadComponent: () =>
      import('./features/instructors/instructors.component').then((m) => m.InstructorsComponent)
  },
  {
    path: 'students',
    loadComponent: () =>
      import('./features/students/students.component').then((m) => m.StudentsComponent)
  },
  {
    path: 'financials',
    loadComponent: () =>
      import('./features/financials/financials.component').then((m) => m.FinancialsComponent)
  },

  // Student Routes
  {
    path: 'student/dashboard',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Student', 'Admin'] },
    loadComponent: () =>
      import('./features/student-dashboard/student-dashboard.component').then((m) => m.StudentDashboardComponent)
  },
  {
    path: 'student/courses',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Student', 'Admin'] },
    loadComponent: () =>
      import('./features/courses/courses.component').then((m) => m.CoursesComponent)
  },
  {
    path: 'student/courses/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/course-detail/course-detail.component').then((m) => m.CourseDetailComponent)
  },
  {
    path: 'student/certificates',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Student', 'Admin'] },
    loadComponent: () =>
      import('./features/certificate-view/certificate-view.component').then((m) => m.CertificateViewComponent)
  },

  // Instructor Routes
  {
    path: 'instructor/dashboard',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Instructor', 'Admin'] },
    loadComponent: () =>
      import('./features/instructor-dashboard/instructor-dashboard.component').then((m) => m.InstructorDashboardComponent)
  },

  // Catch-all
  {
    path: '**',
    redirectTo: 'admin/dashboard'
  }
];
