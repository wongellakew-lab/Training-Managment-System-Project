import { Component, Input, OnInit, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../services/auth.service';
import { NotificationService, TmsNotification } from '../../services/notification.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatBadgeModule,
    MatDividerModule,
    MatTooltipModule,
    MatSnackBarModule
  ],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent implements OnInit {
  @Input() pageTitle: string = 'Dashboard';
  @Input() showGreeting: boolean = false;

  private authService = inject(AuthService);
  readonly notificationService = inject(NotificationService);
  private snackBar = inject(MatSnackBar);

  currentUser = this.authService.currentUser;

  /** Dashboard path based on role */
  readonly dashboardPath = computed(() => {
    const role = this.authService.userRole();
    if (role === 'Instructor') return '/instructor/dashboard';
    if (role === 'Student') return '/student/dashboard';
    return '/admin/dashboard';
  });

  /** Dedicated profile path based on role */
  readonly profilePath = computed(() => {
    const role = this.authService.userRole();
    if (role === 'Instructor') return '/instructor/profile';
    if (role === 'Student') return '/student/profile';
    return '/admin/dashboard';
  });

  readonly greeting = computed(() => {
    const hour = new Date().getHours();
    if (hour < 12) return 'Good morning';
    if (hour < 17) return 'Good afternoon';
    return 'Good evening';
  });

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      this.notificationService.loadNotifications(1, 20).subscribe({ error: () => {} });
      this.notificationService.loadUnreadCount().subscribe({ error: () => {} });
    }
  }

  getUserName(): string {
    const user = this.currentUser();
    if (!user) return 'User';
    return `${user.firstName || ''} ${user.lastName || ''}`.trim() || user.email;
  }

  getUserInitials(): string {
    const user = this.currentUser();
    if (!user) return 'U';
    const first = user.firstName?.[0] ?? '';
    const last = user.lastName?.[0] ?? '';
    return (first + last).toUpperCase() || user.email[0].toUpperCase();
  }

  getUserRole(): string {
    return this.authService.userRole() || 'User';
  }

  markNotificationRead(n: TmsNotification): void {
    this.notificationService.markAsRead(n.id).subscribe({
      error: () => {
        this.snackBar.open('Failed to mark notification as read.', 'Close', { duration: 3000 });
      }
    });
  }

  markAllRead(): void {
    this.notificationService.markAllAsRead().subscribe({
      error: () => {
        this.snackBar.open('Failed to mark all notifications as read.', 'Close', { duration: 3000 });
      }
    });
  }

  logout(): void {
    this.authService.logout();
  }
}
