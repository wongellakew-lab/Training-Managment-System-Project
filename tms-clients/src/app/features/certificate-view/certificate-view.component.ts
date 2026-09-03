import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { CertificateService } from '../../services/certificate.service';
import { TranscriptService } from '../../services/transcript.service';
import { AuthService } from '../../services/auth.service';
import { CertificateResponse, TranscriptJobResponse, TranscriptStatusResponse } from '../../models/transcript.model';
import { LiveSyncService } from '../../services/live-sync.service';

@Component({
  selector: 'app-certificate-view',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    LayoutComponent,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './certificate-view.component.html',
  styleUrls: ['./certificate-view.component.scss']
})
export class CertificateViewComponent implements OnInit {
  private certificateService = inject(CertificateService);
  private transcriptService = inject(TranscriptService);
  readonly authService = inject(AuthService);
  private liveSync = inject(LiveSyncService);
  private snackBar = inject(MatSnackBar);

  certificates = signal<CertificateResponse[]>([]);
  isLoading = signal(false);

  transcriptJob = signal<TranscriptJobResponse | null>(null);
  transcriptStatus = signal<TranscriptStatusResponse | null>(null);
  isRequestingTranscript = signal(false);

  verificationCode = '';
  isVerifying = signal(false);
  verificationResult = signal<CertificateResponse | null>(null);
  verificationError = signal<string | null>(null);

  // Preview Modal state
  selectedCertificate = signal<CertificateResponse | null>(null);

  ngOnInit(): void {
    this.loadCertificates();

    // Listen to real-time SignalR transcript notifications
    this.liveSync.transcriptCompleted$.subscribe((evt) => {
      if (this.transcriptJob() && this.transcriptJob()?.jobId === evt.jobId) {
        this.transcriptStatus.set({
          jobId: evt.jobId,
          studentId: evt.studentId,
          status: 'Completed',
          downloadUrl: evt.downloadUrl
        });
      }
    });
  }

  loadCertificates(): void {
    this.isLoading.set(true);

    // Only load certificates belonging to the authenticated student via token
    this.certificateService.getMyCertificates().subscribe({
      next: (certs) => {
        this.certificates.set(certs);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open('Unable to load certificates. Please try again.', 'Close', { duration: 3000 });
      }
    });
  }

  requestTranscript(): void {
    const studentId = this.authService.currentUser()?.studentId || 0;
    this.isRequestingTranscript.set(true);
    this.transcriptService.requestTranscript(studentId).subscribe({
      next: (job) => {
        this.isRequestingTranscript.set(false);
        this.transcriptJob.set(job);
        this.pollTranscriptStatus(job.jobId);
      },
      error: () => {
        this.isRequestingTranscript.set(false);
        this.snackBar.open('Failed to queue transcript generation.', 'Close', { duration: 3000 });
      }
    });
  }

  pollTranscriptStatus(jobId: string): void {
    const interval = setInterval(() => {
      this.transcriptService.getJobStatus(jobId).subscribe({
        next: (res) => {
          this.transcriptStatus.set(res);
          if (res.status === 'Completed' || res.status === 'Failed') {
            clearInterval(interval);
          }
        },
        error: () => clearInterval(interval)
      });
    }, 2000);
  }

  verifyCode(): void {
    const trimmed = this.verificationCode.trim();
    if (!trimmed) return;
    this.isVerifying.set(true);
    this.verificationResult.set(null);
    this.verificationError.set(null);

    this.certificateService.verifyCertificate(trimmed).subscribe({
      next: (res) => {
        this.isVerifying.set(false);
        this.verificationResult.set(res);
      },
      error: () => {
        this.isVerifying.set(false);
        this.verificationError.set('Certificate code is invalid or does not exist.');
      }
    });
  }

  openPreview(cert: CertificateResponse): void {
    this.selectedCertificate.set(cert);
  }

  closePreview(): void {
    this.selectedCertificate.set(null);
  }

  downloadCertificate(cert: CertificateResponse, event?: Event): void {
    if (event) event.stopPropagation();
    window.open(this.certificateService.getDownloadUrl(cert.id), '_blank');
  }

  getStudentFullName(): string {
    const user = this.authService.currentUser();
    if (!user) return 'Student';
    return `${user.firstName || ''} ${user.lastName || ''}`.trim() || user.email;
  }
}
