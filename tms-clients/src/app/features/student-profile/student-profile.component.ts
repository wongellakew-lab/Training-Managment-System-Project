import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { StudentProfileService, StudentProfile } from '../../services/student-profile.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-student-profile',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatIconModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    LayoutComponent
  ],
  templateUrl: './student-profile.component.html',
  styleUrls: ['./student-profile.component.scss']
})
export class StudentProfileComponent implements OnInit {
  private profileService = inject(StudentProfileService);
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);
  private snackBar = inject(MatSnackBar);

  readonly profile = signal<StudentProfile | null>(null);
  readonly isLoading = signal<boolean>(true);
  readonly isSavingProfile = signal<boolean>(false);
  readonly isChangingPassword = signal<boolean>(false);
  readonly isUploadingAvatar = signal<boolean>(false);

  profileForm: FormGroup = this.fb.group({
    firstName: ['', [Validators.required]],
    lastName: ['', [Validators.required]],
    phoneNumber: [''],
    department: [''],
    bio: ['']
  });

  passwordForm: FormGroup = this.fb.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
    confirmNewPassword: ['', [Validators.required]]
  });

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.isLoading.set(true);
    this.profileService.getProfile().subscribe({
      next: (data) => {
        this.profile.set(data);
        this.profileForm.patchValue({
          firstName: data.firstName,
          lastName: data.lastName,
          phoneNumber: data.phoneNumber,
          department: data.department,
          bio: data.bio
        });
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open('Unable to load student profile.', 'Close', { duration: 3000 });
      }
    });
  }

  saveProfile(): void {
    if (this.profileForm.invalid) return;
    this.isSavingProfile.set(true);

    const formValues = this.profileForm.value;

    this.profileService.updateProfile(formValues).subscribe({
      next: () => {
        this.isSavingProfile.set(false);
        this.snackBar.open('Profile updated successfully!', 'Close', { duration: 3000 });

        // Instantly synchronize global user state so header & sidebar update without refresh
        this.authService.updateUserProfile({
          firstName: formValues.firstName,
          lastName: formValues.lastName
        });

        this.loadProfile();
      },
      error: (err) => {
        this.isSavingProfile.set(false);
        const msg = err.error?.message || 'Failed to update profile.';
        this.snackBar.open(msg, 'Close', { duration: 4000 });
      }
    });
  }

  changePassword(): void {
    if (this.passwordForm.invalid) return;

    if (this.passwordForm.value.newPassword !== this.passwordForm.value.confirmNewPassword) {
      this.snackBar.open('New password and confirmation do not match.', 'Close', { duration: 3000 });
      return;
    }

    this.isChangingPassword.set(true);
    this.profileService.changePassword(this.passwordForm.value).subscribe({
      next: () => {
        this.isChangingPassword.set(false);
        this.passwordForm.reset();
        this.snackBar.open('Password changed successfully!', 'Close', { duration: 3000 });
      },
      error: (err) => {
        this.isChangingPassword.set(false);
        const msg = err.error?.[0]?.description || err.error?.message || 'Password update failed.';
        this.snackBar.open(msg, 'Close', { duration: 4000 });
      }
    });
  }

  onFileSelected(event: Event): void {
    const target = event.target as HTMLInputElement;
    if (!target.files || target.files.length === 0) return;

    const file = target.files[0];
    this.isUploadingAvatar.set(true);

    this.profileService.uploadAvatar(file).subscribe({
      next: (res) => {
        this.isUploadingAvatar.set(false);
        this.snackBar.open('Avatar photo updated!', 'Close', { duration: 3000 });

        // Instantly synchronize photo across header & sidebar
        this.authService.updateUserProfile({
          profilePictureUrl: res.avatarUrl
        });

        this.loadProfile();
      },
      error: (err) => {
        this.isUploadingAvatar.set(false);
        const msg = err.error?.message || 'Failed to upload photo.';
        this.snackBar.open(msg, 'Close', { duration: 3000 });
      }
    });
  }

  getUserInitials(): string {
    const p = this.profile();
    if (!p) return 'ST';
    const first = p.firstName?.[0] ?? '';
    const last = p.lastName?.[0] ?? '';
    return (first + last).toUpperCase() || 'ST';
  }

  getGpaTier(gpa?: number): string {
    const val = gpa ?? 0;
    if (val >= 3.8) return 'Summa Cum Laude (Distinction)';
    if (val >= 3.5) return 'Magna Cum Laude (Honors)';
    if (val >= 3.0) return 'Dean\'s List Standing';
    if (val >= 2.0) return 'Good Academic Standing';
    return 'Academic Review Required';
  }

  getGpaTierClass(gpa?: number): string {
    const val = gpa ?? 0;
    if (val >= 3.5) return 'tier-honor';
    if (val >= 2.0) return 'tier-good';
    return 'tier-warning';
  }
}
