import { Component, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterLinkActive } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../services/auth.service';

interface NavItem {
  label: string;
  path: string;
  icon: string;
  exact?: boolean;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIconModule, MatButtonModule, MatTooltipModule],
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss']
})
export class SidebarComponent {
  private authService = inject(AuthService);

  currentUser = this.authService.currentUser;
  userRole = this.authService.userRole;

  getUserInitials(): string {
    const user = this.currentUser();
    if (!user) return 'U';
    const first = user.firstName?.[0] ?? '';
    const last = user.lastName?.[0] ?? '';
    return (first + last).toUpperCase() || user.email[0].toUpperCase();
  }

  getUserName(): string {
    const user = this.currentUser();
    if (!user) return 'User';
    return `${user.firstName || ''} ${user.lastName || ''}`.trim() || user.email;
  }

  readonly navItems = computed<NavItem[]>(() => {
    const role = this.userRole();

    if (role === 'Admin') {
      return [
        { label: 'Dashboard', path: '/admin/dashboard', icon: 'grid_view', exact: true },
        { label: 'Students', path: '/admin/students', icon: 'school' },
        { label: 'Instructors', path: '/admin/instructors', icon: 'co_present' },
        { label: 'Courses', path: '/admin/courses', icon: 'menu_book' },
        { label: 'Enrollments', path: '/admin/enrollments', icon: 'people' },
        { label: 'Calendar', path: '/admin/calendar', icon: 'calendar_today' },
        { label: 'Financials', path: '/admin/financials', icon: 'attach_money' }
      ];
    }

    if (role === 'Instructor') {
      return [
        { label: 'Dashboard', path: '/instructor/dashboard', icon: 'grid_view', exact: true },
        { label: 'My Courses', path: '/instructor/courses', icon: 'menu_book' }
      ];
    }

    // Student
    return [
      { label: 'Dashboard', path: '/student/dashboard', icon: 'grid_view', exact: true },
      { label: 'My Courses', path: '/student/courses', icon: 'menu_book' },
      { label: 'My Enrollments', path: '/student/enrollments', icon: 'people' },
      { label: 'Grades', path: '/student/grades', icon: 'grade' },
      { label: 'Certificates', path: '/student/certificates', icon: 'verified' }
    ];
  });

  logout(): void {
    this.authService.logout();
  }
}
