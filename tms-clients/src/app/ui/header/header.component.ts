import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { AuthService } from '../../services/auth.service';

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
    MatDividerModule
  ],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent {
  @Input() pageTitle: string = 'Good Morning, Noah!';
  @Input() showGreeting: boolean = false;

  private authService = inject(AuthService);
  currentUser = this.authService.currentUser;

  getUserName(): string {
    const user = this.currentUser();
    if (!user) return 'Noah Hansen';
    return `${user.firstName || 'Noah'} ${user.lastName || 'Hansen'}`;
  }

  getUserRole(): string {
    const user = this.currentUser();
    return user?.role || 'Admin';
  }
}
