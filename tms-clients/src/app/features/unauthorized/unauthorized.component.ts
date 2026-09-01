import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-unauthorized',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="min-h-screen flex items-center justify-center p-4 bg-slate-950">
      <div class="glass-panel max-w-md w-full p-8 text-center border border-white/10 shadow-2xl">
        <div class="w-16 h-16 rounded-2xl bg-rose-500/20 text-rose-400 flex items-center justify-center mx-auto mb-4 border border-rose-500/30">
          <span class="material-icons text-3xl">lock</span>
        </div>
        <h1 class="text-2xl font-bold text-white mb-2">Access Restricted</h1>
        <p class="text-xs text-slate-400 mb-6 leading-relaxed">
          You do not have the required permissions or role to view this page. Please sign in with an account having appropriate authorization.
        </p>
        <div class="flex items-center justify-center gap-3">
          <a routerLink="/login" class="btn-primary text-xs py-2 px-4">
            Sign In with Different Account
          </a>
        </div>
      </div>
    </div>
  `
})
export class UnauthorizedComponent {}
