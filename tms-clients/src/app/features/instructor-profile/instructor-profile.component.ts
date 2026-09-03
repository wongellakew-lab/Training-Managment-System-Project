import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { AuthService } from '../../services/auth.service';
import { InstructorWorkspaceService } from '../../services/instructor-workspace.service';
import { InstructorProfile } from '../../models/instructor-workspace.model';

@Component({
  selector: 'app-instructor-profile',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LayoutComponent,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './instructor-profile.component.html',
  styleUrls: ['./instructor-profile.component.scss']
})
export class InstructorProfileComponent implements OnInit {
  private instructorService = inject(InstructorWorkspaceService);
  private authService = inject(AuthService);
  private snackBar = inject(MatSnackBar);

  isLoading = signal<boolean>(true);
  isSavingProfile = signal<boolean>(false);
  isUploadingAvatar = signal<boolean>(false);
  isChangingPassword = signal<boolean>(false);

  profile = signal<InstructorProfile | null>(null);

  // Edit fields
  firstName = signal<string>('');
  lastName = signal<string>('');
  phoneNumber = signal<string>('');
  department = signal<string>('');
  bio = signal<string>('');

  // Password fields
  currentPassword = signal<string>('');
  newPassword = signal<string>('');
  confirmPassword = signal<string>('');

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.isLoading.set(true);
    this.instructorService.getProfile().subscribe({
      next: (data) => {
        this.profile.set(data);
        this.firstName.set(data.firstName || '');
        this.lastName.set(data.lastName || '');
        this.phoneNumber.set(data.phoneNumber || '');
        this.department.set(data.department || '');
        this.bio.set(data.bio || '');
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load instructor profile:', err);
        this.isLoading.set(false);
      }
    });
  }

  onAvatarSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    this.isUploadingAvatar.set(true);

    this.instructorService.uploadAvatar(file).subscribe({
      next: (res) => {
        this.isUploadingAvatar.set(false);
        this.profile.update(p => p ? { ...p, profilePictureUrl: res.profilePictureUrl } : null);
        // Sync AuthService for header & sidebar avatar
        this.authService.updateUserProfile({ profilePictureUrl: res.profilePictureUrl });
        this.snackBar.open('Profile picture updated successfully!', 'OK', { duration: 3000 });
      },
      error: () => {
        this.isUploadingAvatar.set(false);
        this.snackBar.open('Failed to upload picture.', 'Close', { duration: 3000 });
      }
    });
  }

  saveProfile(): void {
    if (!this.firstName().trim() || !this.lastName().trim()) {
      this.snackBar.open('First and last name are required.', 'Close', { duration: 3000 });
      return;
    }

    this.isSavingProfile.set(true);
    this.instructorService.updateProfile({
      firstName: this.firstName(),
      lastName: this.lastName(),
      phoneNumber: this.phoneNumber(),
      department: this.department(),
      bio: this.bio()
    }).subscribe({
      next: (res) => {
        this.isSavingProfile.set(false);
        this.profile.update(p => p ? {
          ...p,
          firstName: res.firstName,
          lastName: res.lastName,
          phoneNumber: res.phoneNumber,
          department: res.department || p.department
        } : null);

        // Sync header and sidebar name
        this.authService.updateUserProfile({
          firstName: res.firstName,
          lastName: res.lastName
        });

        this.snackBar.open('Instructor profile updated successfully!', 'OK', { duration: 3000 });
      },
      error: () => {
        this.isSavingProfile.set(false);
        this.snackBar.open('Failed to update profile.', 'Close', { duration: 3000 });
      }
    });
  }

  changePassword(): void {
    if (!this.currentPassword() || !this.newPassword()) {
      this.snackBar.open('Please provide your current and new password.', 'Close', { duration: 3000 });
      return;
    }

    if (this.newPassword() !== this.confirmPassword()) {
      this.snackBar.open('New passwords do not match.', 'Close', { duration: 3000 });
      return;
    }

    this.isChangingPassword.set(true);
    this.instructorService.changePassword({
      currentPassword: this.currentPassword(),
      newPassword: this.newPassword()
    }).subscribe({
      next: () => {
        this.isChangingPassword.set(false);
        this.currentPassword.set('');
        this.newPassword.set('');
        this.confirmPassword.set('');
        this.snackBar.open('Password changed successfully!', 'OK', { duration: 3500 });
      },
      error: (err) => {
        this.isChangingPassword.set(false);
        const msg = err.error?.message || 'Failed to change password. Ensure it meets complexity rules.';
        this.snackBar.open(msg, 'Close', { duration: 4000 });
      }
    });
  }
}
