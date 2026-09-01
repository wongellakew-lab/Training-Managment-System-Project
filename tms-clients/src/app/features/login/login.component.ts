import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { UserRole } from '../../models/user.model';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="min-h-screen flex items-center justify-center p-4 bg-[radial-gradient(ellipse_at_top,_var(--tw-gradient-stops))] from-indigo-950/40 via-slate-950 to-slate-950">
      <!-- Glow effect background -->
      <div class="absolute w-[500px] h-[500px] bg-indigo-500/10 rounded-full blur-3xl pointer-events-none -top-20 -left-20"></div>
      <div class="absolute w-[400px] h-[400px] bg-cyan-500/10 rounded-full blur-3xl pointer-events-none -bottom-20 -right-20"></div>

      <div class="glass-panel w-full max-w-md p-8 relative z-10 border border-white/10 shadow-2xl">
        <!-- Logo & Header -->
        <div class="text-center mb-8">
          <div class="inline-flex w-14 h-14 rounded-2xl bg-gradient-to-tr from-indigo-600 to-cyan-400 items-center justify-center shadow-xl shadow-indigo-500/30 mb-3">
            <span class="material-icons text-white text-2xl">school</span>
          </div>
          <h1 class="text-2xl font-black text-white tracking-tight">Training Management System</h1>
          <p class="text-xs text-slate-400 mt-1">Enterprise Course Administration & Live Sync Portal</p>
        </div>

        <!-- Mode Toggle Tabs -->
        <div class="flex p-1 bg-slate-900/80 rounded-xl mb-6 border border-white/5">
          <button
            type="button"
            (click)="isRegisterMode.set(false)"
            class="flex-1 py-2 text-xs font-semibold rounded-lg transition-all"
            [ngClass]="!isRegisterMode() ? 'bg-indigo-600 text-white shadow-md' : 'text-slate-400 hover:text-slate-200'"
          >
            Sign In
          </button>
          <button
            type="button"
            (click)="isRegisterMode.set(true)"
            class="flex-1 py-2 text-xs font-semibold rounded-lg transition-all"
            [ngClass]="isRegisterMode() ? 'bg-indigo-600 text-white shadow-md' : 'text-slate-400 hover:text-slate-200'"
          >
            Register
          </button>
        </div>

        <!-- Error Message Alert -->
        @if (errorMessage()) {
          <div class="mb-5 p-3 rounded-lg bg-rose-500/15 border border-rose-500/30 flex items-start gap-2.5 text-xs text-rose-300">
            <span class="material-icons text-base text-rose-400">error_outline</span>
            <span class="leading-relaxed">{{ errorMessage() }}</span>
          </div>
        }

        <!-- Quick Demo Switchers -->
        @if (!isRegisterMode()) {
          <div class="mb-6">
            <div class="text-[11px] font-semibold text-slate-400 mb-2 uppercase tracking-wider">Quick Demo Login:</div>
            <div class="grid grid-cols-3 gap-2">
              <button (click)="fillCredentials('student@tms.com', 'Student')" class="py-1.5 px-2 bg-slate-800/90 hover:bg-indigo-950/50 hover:border-indigo-500/40 border border-white/5 rounded-lg text-[11px] font-medium text-slate-300 transition-colors">
                Student
              </button>
              <button (click)="fillCredentials('instructor@tms.com', 'Instructor')" class="py-1.5 px-2 bg-slate-800/90 hover:bg-indigo-950/50 hover:border-indigo-500/40 border border-white/5 rounded-lg text-[11px] font-medium text-slate-300 transition-colors">
                Instructor
              </button>
              <button (click)="fillCredentials('admin@tms.com', 'Admin')" class="py-1.5 px-2 bg-slate-800/90 hover:bg-indigo-950/50 hover:border-indigo-500/40 border border-white/5 rounded-lg text-[11px] font-medium text-slate-300 transition-colors">
                Admin
              </button>
            </div>
          </div>
        }

        <form (ngSubmit)="onSubmit()" class="space-y-4">
          @if (isRegisterMode()) {
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-xs font-semibold text-slate-300 mb-1.5">First Name</label>
                <input
                  type="text"
                  [(ngModel)]="firstName"
                  name="firstName"
                  required
                  placeholder="Jane"
                  class="form-input text-xs"
                />
              </div>
              <div>
                <label class="block text-xs font-semibold text-slate-300 mb-1.5">Last Name</label>
                <input
                  type="text"
                  [(ngModel)]="lastName"
                  name="lastName"
                  required
                  placeholder="Doe"
                  class="form-input text-xs"
                />
              </div>
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-300 mb-1.5">Account Role</label>
              <select [(ngModel)]="role" name="role" class="form-select text-xs">
                <option value="Student">Student</option>
                <option value="Instructor">Instructor</option>
                <option value="Admin">Administrator</option>
              </select>
            </div>
          }

          <div>
            <label class="block text-xs font-semibold text-slate-300 mb-1.5">Email Address</label>
            <input
              type="email"
              [(ngModel)]="email"
              name="email"
              required
              placeholder="user@example.com"
              class="form-input text-xs"
            />
          </div>

          <div>
            <label class="block text-xs font-semibold text-slate-300 mb-1.5">Password</label>
            <input
              type="password"
              [(ngModel)]="password"
              name="password"
              required
              placeholder="••••••••"
              class="form-input text-xs"
            />
          </div>

          <button
            type="submit"
            [disabled]="isLoading()"
            class="btn-primary w-full mt-6 py-2.5 text-sm"
          >
            @if (isLoading()) {
              <span class="inline-block animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full mr-2"></span>
              <span>Processing...</span>
            } @else {
              <span>{{ isRegisterMode() ? 'Create Account' : 'Sign In to Portal' }}</span>
            }
          </button>
        </form>
      </div>
    </div>
  `
})
export class LoginComponent {
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  email = 'student@tms.com';
  password = 'Password123!';
  firstName = 'Jane';
  lastName = 'Doe';
  role: UserRole = 'Student';

  isRegisterMode = signal(false);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);

  fillCredentials(email: string, role: UserRole) {
    this.email = email;
    this.password = 'Password123!';
    this.role = role;
  }

  onSubmit() {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    if (this.isRegisterMode()) {
      this.authService.register({
        email: this.email,
        password: this.password,
        firstName: this.firstName,
        lastName: this.lastName,
        role: this.role
      }).subscribe({
        next: () => this.navigateAfterAuth(),
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err.message || 'Registration failed');
        }
      });
    } else {
      this.authService.login({
        email: this.email,
        password: this.password
      }).subscribe({
        next: () => this.navigateAfterAuth(),
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err.message || 'Invalid credentials');
        }
      });
    }
  }

  private navigateAfterAuth() {
    this.isLoading.set(false);
    const returnUrl = this.route.snapshot.queryParams['returnUrl'];
    if (returnUrl) {
      this.router.navigateByUrl(returnUrl);
      return;
    }

    const role = this.authService.userRole();
    if (role === 'Instructor') {
      this.router.navigate(['/instructor/dashboard']);
    } else if (role === 'Admin') {
      this.router.navigate(['/admin/courses']);
    } else {
      this.router.navigate(['/student/dashboard']);
    }
  }
}
