import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIconModule, MatButtonModule],
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss']
})
export class SidebarComponent {
  private authService = inject(AuthService);

  navItems = [
    { label: 'Dashboard', path: '/admin/dashboard', icon: 'grid_view' },
    { label: 'Calendar', path: '/calendar', icon: 'calendar_today' },
    { label: 'Enrollments', path: '/enrollments', icon: 'people' },
    { label: 'Courses', path: '/courses', icon: 'menu_book' },
    { label: 'Instructors', path: '/instructors', icon: 'co_present' },
    { label: 'Students', path: '/students', icon: 'school' },
    { label: 'Financials', path: '/financials', icon: 'attach_money' }
  ];

  logout(): void {
    this.authService.logout();
  }
}
