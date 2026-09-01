import { Component, Input, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../services/auth.service';
import { NotificationService } from '../../services/notification.service';
import { LiveSyncService } from '../../services/live-sync.service';

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
    MatTooltipModule
  ],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent {
  @Input() pageTitle: string = 'Dashboard';
  @Input() showGreeting: boolean = false;

  private authService = inject(AuthService);
  readonly notificationService = inject(NotificationService);
  private liveSync = inject(LiveSyncService);

  currentUser = this.authService.currentUser;
  readonly isConnected = this.liveSync.isConnected;

  /** Dashboard path based on role */
  readonly dashboardPath = computed(() => {
    const role = this.authService.userRole();
    if (role === 'Instructor') return '/instructor/dashboard';
    if (role === 'Student') return '/student/dashboard';
    return '/admin/dashboard';
  });

  readonly greeting = computed(() => {
    const hour = new Date().getHours();
    if (hour < 12) return 'Good morning';
    if (hour < 17) return 'Good afternoon';
    return 'Good evening';
  });

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

  markAllRead(): void {
    this.notificationService.markAllAsRead().subscribe({ error: console.warn });
  }

  logout(): void {
    this.authService.logout();
  }
}
