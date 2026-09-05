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
        path: 'students/:id',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./features/students/student-detail/student-detail.component').then((m) => m.StudentDetailComponent)
      },
      {
        path: 'instructors',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./features/instructors/instructors.component').then((m) => m.InstructorsComponent)
      },
      {
        path: 'instructors/:id',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./features/instructors/instructor-detail/instructor-detail.component').then((m) => m.InstructorDetailComponent)
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
        path: 'schedule',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Instructor', 'Admin'] },
        loadComponent: () =>
          import('./features/instructor-schedule/instructor-schedule.component').then((m) => m.InstructorScheduleComponent)
      },
      {
        path: 'courses',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Instructor', 'Admin'] },
        loadComponent: () =>
          import('./features/instructor-courses/instructor-courses.component').then((m) => m.InstructorCoursesComponent)
      },
      {
        path: 'courses/:id',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Instructor', 'Admin'] },
        loadComponent: () =>
          import('./features/course-detail/course-detail.component').then((m) => m.CourseDetailComponent)
      },
      {
        path: 'courses/:id/builder',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Instructor', 'Admin'] },
        loadComponent: () =>
          import('./features/course-builder/course-builder.component').then((m) => m.CourseBuilderComponent)
      },
      {
        path: 'attendance',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Instructor', 'Admin'] },
        loadComponent: () =>
          import('./features/instructor-attendance/instructor-attendance.component').then((m) => m.InstructorAttendanceComponent)
      },
      {
        path: 'assignments',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Instructor', 'Admin'] },
        loadComponent: () =>
          import('./features/instructor-assignments/instructor-assignments.component').then((m) => m.InstructorAssignmentsComponent)
      },
      {
        path: 'gradebook',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Instructor', 'Admin'] },
        loadComponent: () =>
          import('./features/instructor-gradebook/instructor-gradebook.component').then((m) => m.InstructorGradebookComponent)
      },
      {
        path: 'submissions',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Instructor', 'Admin'] },
        loadComponent: () =>
          import('./features/instructor-submissions/instructor-submissions.component').then((m) => m.InstructorSubmissionsComponent)
      },
      {
        path: 'profile',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Instructor', 'Admin'] },
        loadComponent: () =>
          import('./features/instructor-profile/instructor-profile.component').then((m) => m.InstructorProfileComponent)
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
        path: 'catalog',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Student', 'Admin'] },
        loadComponent: () =>
          import('./features/courses/courses.component').then((m) => m.CoursesComponent)
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
          import('./features/student-course-details/student-course-details.component').then((m) => m.StudentCourseDetailsComponent)
      },
      {
        path: 'schedule',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Student', 'Admin'] },
        loadComponent: () =>
          import('./features/student-schedule/student-schedule.component').then((m) => m.StudentScheduleComponent)
      },
      {
        path: 'assignments',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Student', 'Admin'] },
        loadComponent: () =>
          import('./features/student-assignments/student-assignments.component').then((m) => m.StudentAssignmentsComponent)
      },
      {
        path: 'gradebook',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Student', 'Admin'] },
        loadComponent: () =>
          import('./features/student-gradebook/student-gradebook.component').then((m) => m.StudentGradebookComponent)
      },
      {
        path: 'progress',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Student', 'Admin'] },
        loadComponent: () =>
          import('./features/student-progress/student-progress.component').then((m) => m.StudentProgressComponent)
      },
      {
        path: 'progress/:courseId',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Student', 'Admin'] },
        loadComponent: () =>
          import('./features/course-progress-details/course-progress-details.component').then((m) => m.CourseProgressDetailsComponent)
      },
      {
        path: 'profile',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Student', 'Admin'] },
        loadComponent: () =>
          import('./features/student-profile/student-profile.component').then((m) => m.StudentProfileComponent)
      },
      {
        path: 'my-courses',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Student', 'Admin'] },
        loadComponent: () =>
          import('./features/student-my-courses/student-my-courses.component').then((m) => m.StudentMyCoursesComponent)
      },
      {
        path: 'enrollments',
        redirectTo: 'my-courses',
        pathMatch: 'full'
      },
      {
        path: 'grades',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Student', 'Admin'] },
        loadComponent: () =>
          import('./features/student-gradebook/student-gradebook.component').then((m) => m.StudentGradebookComponent)
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
