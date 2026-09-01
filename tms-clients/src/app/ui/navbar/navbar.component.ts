import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { AuthService } from '../../services/auth.service';
import { LiveSyncService } from '../../services/live-sync.service';
import { ThemeService } from '../../services/theme.service';
import { NotificationService, TmsNotification } from '../../services/notification.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatMenuModule,
    MatBadgeModule
  ],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent implements OnInit {
  authService = inject(AuthService);
  liveSync = inject(LiveSyncService);
  themeService = inject(ThemeService);
  notificationService = inject(NotificationService);

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      this.notificationService.loadUnreadCount().subscribe();
      this.notificationService.loadNotifications(1, 10).subscribe();
    }
  }

  onOpenNotifications(): void {
    this.notificationService.loadNotifications(1, 10).subscribe();
  }

  markAllRead(event: Event): void {
    event.stopPropagation();
    this.notificationService.markAllAsRead().subscribe();
  }

  onNotificationClick(item: TmsNotification): void {
    if (!item.isRead) {
      this.notificationService.markAsRead(item.id).subscribe();
    }
  }
}
