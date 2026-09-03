import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { StudentProfileService, StudentProfile } from '../../services/student-profile.service';

@Component({
  selector: 'app-student-profile',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
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
      error: () => this.isLoading.set(false)
    });
  }

  saveProfile(): void {
    if (this.profileForm.invalid) return;
    this.isSavingProfile.set(true);

    this.profileService.updateProfile(this.profileForm.value).subscribe({
      next: () => {
        this.isSavingProfile.set(false);
        this.snackBar.open('Profile updated successfully!', 'Close', { duration: 3000 });
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
        this.loadProfile();
      },
      error: (err) => {
        this.isUploadingAvatar.set(false);
        const msg = err.error?.message || 'Failed to upload photo.';
        this.snackBar.open(msg, 'Close', { duration: 3000 });
      }
    });
  }
}
