import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../services/auth.service';
import { UserRole } from '../../models/user.model';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  isRegisterMode = signal(false);
  isLoading = signal(false);
  hidePassword = signal(true);
  errorMessage = signal<string | null>(null);

  form: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    firstName: [''],
    lastName: [''],
    role: ['Student' as UserRole]
  });

  setMode(register: boolean) {
    this.isRegisterMode.set(register);
    this.errorMessage.set(null);

    const firstNameControl = this.form.get('firstName');
    const lastNameControl = this.form.get('lastName');

    if (register) {
      firstNameControl?.setValidators([Validators.required]);
      lastNameControl?.setValidators([Validators.required]);
    } else {
      firstNameControl?.clearValidators();
      lastNameControl?.clearValidators();
    }
    firstNameControl?.updateValueAndValidity();
    lastNameControl?.updateValueAndValidity();
  }

  fillCredentials(email: string, pass: string) {
    this.form.patchValue({
      email,
      password: pass
    });
  }

  onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const val = this.form.value;

    if (this.isRegisterMode()) {
      this.authService.register({
        email: val.email,
        password: val.password,
        firstName: val.firstName,
        lastName: val.lastName,
        role: val.role
      }).subscribe({
        next: () => this.navigateAfterAuth(),
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err.error?.message || err.message || 'Registration failed.');
        }
      });
    } else {
      this.authService.login({
        email: val.email,
        password: val.password
      }).subscribe({
        next: () => this.navigateAfterAuth(),
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(err.error?.message || 'Invalid email or password.');
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
      this.router.navigate(['/admin/dashboard']);
    } else {
      this.router.navigate(['/student/dashboard']);
    }
  }
}
