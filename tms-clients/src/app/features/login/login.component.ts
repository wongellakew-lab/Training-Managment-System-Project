import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
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
export class LoginComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  isRegisterMode = signal(false);
  isLoading = signal(false);
  hidePassword = signal(true);
  errorMessage = signal<string | null>(null);

    ngOnInit(): void {
    // Completely clear login fields to prevent browser autofill/autocomplete repopulation
    this.form.reset({
      email: '',
      password: '',
      firstName: '',
      lastName: '',
      role: 'Student'
    });

    const sessionExpired = this.route.snapshot.queryParams['sessionExpired'];
    if (sessionExpired === 'true') {
      this.errorMessage.set('Your session has expired. Please sign in again.');
    }
  }

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
    this.form.reset({ role: 'Student' });

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

  onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const val = this.form.value;

    const email = val.email;
    const password = val.password;
    // Wipe password from reactive form state immediately
    this.form.patchValue({ password: '' }, { emitEvent: false });

    if (this.isRegisterMode()) {
      this.authService.register({
        email,
        password,
        firstName: val.firstName,
        lastName: val.lastName,
        role: val.role
      }).subscribe({
        next: () => this.navigateAfterAuth(),
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(
            err.error?.message || err.error?.errors?.[0]?.description || 'Registration failed. Please try again.'
          );
        }
      });
    } else {
      this.authService.login({
        email,
        password
      }).subscribe({
        next: () => this.navigateAfterAuth(),
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(
            err.error?.message || 'Invalid email or password. Please check your credentials.'
          );
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
