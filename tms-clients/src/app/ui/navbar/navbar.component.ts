import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { LiveSyncService } from '../../services/live-sync.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <nav class="glass-panel sticky top-0 z-50 px-6 py-3.5 mb-8 flex items-center justify-between border-b border-white/10">
      <!-- Brand / Logo -->
      <div class="flex items-center gap-3">
        <div class="w-10 h-10 rounded-xl bg-gradient-to-tr from-indigo-600 via-indigo-500 to-cyan-400 flex items-center justify-center shadow-lg shadow-indigo-500/25">
          <span class="material-icons text-white text-xl">school</span>
        </div>
        <div>
          <a routerLink="/" class="text-xl font-extrabold tracking-tight bg-gradient-to-r from-white via-slate-200 to-indigo-200 bg-clip-text text-transparent no-underline">
            TMS Core
          </a>
          <span class="block text-[10px] uppercase font-bold tracking-widest text-indigo-400">Enterprise LMS</span>
        </div>
      </div>

      <!-- Navigation Links -->
      @if (authService.isAuthenticated()) {
        <div class="flex items-center gap-2">
          @if (authService.isStudent()) {
            <a routerLink="/student/dashboard" routerLinkActive="bg-indigo-600/20 text-indigo-300 border-indigo-500/30" class="nav-item">
              <span class="material-icons text-sm">dashboard</span>
              <span>My Dashboard</span>
            </a>
            <a routerLink="/student/courses" routerLinkActive="bg-indigo-600/20 text-indigo-300 border-indigo-500/30" class="nav-item">
              <span class="material-icons text-sm">search</span>
              <span>Course Catalog</span>
            </a>
            <a routerLink="/student/certificates" routerLinkActive="bg-indigo-600/20 text-indigo-300 border-indigo-500/30" class="nav-item">
              <span class="material-icons text-sm">workspace_premium</span>
              <span>Certificates & Transcripts</span>
            </a>
          }

          @if (authService.isInstructor()) {
            <a routerLink="/instructor/dashboard" routerLinkActive="bg-indigo-600/20 text-indigo-300 border-indigo-500/30" class="nav-item">
              <span class="material-icons text-sm">assignment</span>
              <span>Gradebook</span>
            </a>
          }

          @if (authService.isAdmin()) {
            <a routerLink="/admin/dashboard" routerLinkActive="bg-indigo-600/20 text-indigo-300 border-indigo-500/30" class="nav-item">
              <span class="material-icons text-sm">dashboard</span>
              <span>Admin Dashboard</span>
            </a>
            <a routerLink="/admin/courses" routerLinkActive="bg-indigo-600/20 text-indigo-300 border-indigo-500/30" class="nav-item">
              <span class="material-icons text-sm">settings</span>
              <span>Course Management</span>
            </a>
          }
        </div>
      }

      <!-- Right Actions: Status + Notifications + Profile -->
      <div class="flex items-center gap-4">
        <!-- Live Sync Status Indicator -->
        <div class="flex items-center gap-2 px-3 py-1.5 rounded-full bg-slate-900/60 border border-white/5 text-xs text-slate-300" title="SignalR Live Connection">
          <span class="w-2 h-2 rounded-full" [ngClass]="liveSync.isConnected() ? 'bg-emerald-400 shadow-[0_0_8px_#34d399]' : 'bg-rose-500'"></span>
          <span class="font-mono text-[11px]">{{ liveSync.isConnected() ? 'LIVE' : 'OFFLINE' }}</span>
        </div>

        @if (authService.isAuthenticated()) {
          <!-- Notifications Bell -->
          <div class="relative">
            <button (click)="toggleNotifications()" class="relative p-2 rounded-lg bg-slate-800/80 hover:bg-slate-700/80 text-slate-300 transition-colors border border-white/5">
              <span class="material-icons text-lg">notifications</span>
              @if (liveSync.unreadCount() > 0) {
                <span class="absolute -top-1 -right-1 w-5 h-5 bg-rose-500 text-white rounded-full text-[10px] font-bold flex items-center justify-center animate-bounce">
                  {{ liveSync.unreadCount() }}
                </span>
              }
            </button>

            <!-- Notifications Dropdown -->
            @if (showNotifications()) {
              <div class="absolute right-0 mt-2 w-80 max-h-96 overflow-y-auto glass-panel p-3 shadow-2xl z-50 border border-white/10">
                <div class="flex items-center justify-between pb-2 border-b border-white/10 mb-2">
                  <span class="text-xs font-bold uppercase tracking-wider text-slate-400">Live Notifications</span>
                  <button (click)="liveSync.markAllAsRead()" class="text-xs text-indigo-400 hover:text-indigo-300">Mark read</button>
                </div>
                @if (liveSync.notifications().length === 0) {
                  <div class="py-6 text-center text-xs text-slate-500">No new notifications</div>
                } @else {
                  <div class="space-y-2">
                    @for (item of liveSync.notifications(); track item.id) {
                      <div class="p-2.5 rounded-lg bg-slate-900/60 border border-white/5 text-xs">
                        <div class="font-semibold text-slate-200">{{ item.title }}</div>
                        <div class="text-slate-400 text-[11px] mt-0.5">{{ item.message }}</div>
                        <div class="text-slate-500 text-[9px] mt-1">{{ item.timestamp | date:'shortTime' }}</div>
                      </div>
                    }
                  </div>
                }
              </div>
            }
          </div>

          <!-- User Info & Logout -->
          <div class="flex items-center gap-3 pl-3 border-l border-white/10">
            <div class="text-right hidden sm:block">
              <div class="text-xs font-bold text-slate-200">{{ authService.currentUser()?.firstName }} {{ authService.currentUser()?.lastName }}</div>
              <div class="text-[10px] text-indigo-400 font-medium">{{ authService.userRole() }}</div>
            </div>
            <button (click)="authService.logout()" class="p-2 rounded-lg bg-rose-500/10 hover:bg-rose-500/20 text-rose-400 border border-rose-500/20 transition-colors" title="Logout">
              <span class="material-icons text-lg">logout</span>
            </button>
          </div>
        } @else {
          <a routerLink="/login" class="btn-primary text-xs py-2 px-4">
            <span>Sign In</span>
          </a>
        }
      </div>
    </nav>
  `,
  styles: [`
    .nav-item {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.5rem 0.875rem;
      font-size: 0.8125rem;
      font-weight: 500;
      color: #94a3b8;
      border: 1px solid transparent;
      border-radius: var(--radius-md);
      text-decoration: none;
      transition: all 0.2s ease;

      &:hover {
        color: #f8fafc;
        background: rgba(255, 255, 255, 0.05);
      }
    }
  `]
})
export class NavbarComponent {
  authService = inject(AuthService);
  liveSync = inject(LiveSyncService);
  showNotifications = signal(false);

  toggleNotifications() {
    this.showNotifications.update((v) => !v);
    if (this.showNotifications()) {
      this.liveSync.markAllAsRead();
    }
  }
}
