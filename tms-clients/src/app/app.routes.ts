import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { roleGuard } from './guards/role.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'login'
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
      import('./features/student-dashboard/course-browser.component').then((m) => m.CourseBrowserComponent)
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

  // Admin Routes
  {
    path: 'admin/courses',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['Admin'] },
    loadComponent: () =>
      import('./features/admin-course-list/admin-course-list.component').then((m) => m.AdminCourseListComponent)
  },

  // Catch-all
  {
    path: '**',
    redirectTo: 'login'
  }
];
