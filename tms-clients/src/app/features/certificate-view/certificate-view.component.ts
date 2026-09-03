import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { CertificateService } from '../../services/certificate.service';
import { TranscriptService } from '../../services/transcript.service';
import { AuthService } from '../../services/auth.service';
import {
  CertificateResponse,
  TranscriptDto,
  TranscriptJobResponse,
  TranscriptStatusResponse
} from '../../models/transcript.model';
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

  // Top tabs
  activeTab = signal<'certificates' | 'transcript'>('certificates');

  // Certificates State
  certificates = signal<CertificateResponse[]>([]);
  isLoadingCertificates = signal<boolean>(false);
  selectedCertificate = signal<CertificateResponse | null>(null);

  // Academic Transcript State
  transcript = signal<TranscriptDto | null>(null);
  isLoadingTranscript = signal<boolean>(false);
  isGeneratingTranscript = signal<boolean>(false);
  showTranscriptSheet = signal<boolean>(true);

  // Background Job Notification State
  transcriptJob = signal<TranscriptJobResponse | null>(null);
  transcriptStatus = signal<TranscriptStatusResponse | null>(null);

  ngOnInit(): void {
    this.loadCertificates();
    this.loadTranscript();

    // Listen to real-time SignalR transcript notifications
    this.liveSync.transcriptCompleted$.subscribe((evt) => {
      if (this.transcriptJob() && this.transcriptJob()?.jobId === evt.jobId) {
        this.transcriptStatus.set({
          jobId: evt.jobId,
          studentId: evt.studentId,
          status: 'Completed',
          downloadUrl: evt.downloadUrl
        });
        this.loadTranscript();
      }
    });
  }

  setTab(tab: 'certificates' | 'transcript'): void {
    this.activeTab.set(tab);
  }

  // ─── Certificates ────────────────────────────────────────────────────────
  loadCertificates(): void {
    this.isLoadingCertificates.set(true);
    this.certificateService.getMyCertificates().subscribe({
      next: (certs) => {
        this.certificates.set(certs || []);
        this.isLoadingCertificates.set(false);
      },
      error: () => {
        this.isLoadingCertificates.set(false);
        this.snackBar.open('Unable to load certificates.', 'Close', { duration: 3000 });
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

  // ─── Academic Transcript ──────────────────────────────────────────────────
  loadTranscript(): void {
    this.isLoadingTranscript.set(true);
    this.transcriptService.getMyTranscript().subscribe({
      next: (data) => {
        this.transcript.set(data);
        this.isLoadingTranscript.set(false);
      },
      error: (err) => {
        console.error('Failed to load transcript:', err);
        this.isLoadingTranscript.set(false);
      }
    });
  }

  generateTranscript(): void {
    this.isGeneratingTranscript.set(true);
    this.transcriptService.requestTranscript().subscribe({
      next: (res) => {
        this.isGeneratingTranscript.set(false);
        this.transcriptJob.set({
          jobId: res.requestId,
          status: 'Completed',
          estimatedWaitSeconds: 0,
          submittedAt: new Date().toISOString()
        });
        this.snackBar.open('Official academic transcript generated successfully from database.', 'OK', { duration: 3500 });
        this.loadTranscript();
      },
      error: () => {
        this.isGeneratingTranscript.set(false);
        this.snackBar.open('Error generating transcript.', 'Dismiss', { duration: 3000 });
      }
    });
  }

  printTranscript(): void {
    const docNum = this.transcript()?.documentNumber || 'official-transcript';
    window.open(this.transcriptService.getDownloadUrl(`${docNum}.pdf`), '_blank');
  }

  downloadTranscript(): void {
    const docNum = this.transcript()?.documentNumber || 'official-transcript';
    const url = this.transcriptService.getDownloadUrl(`${docNum}.pdf`);
    const a = document.createElement('a');
    a.href = url;
    a.target = '_blank';
    a.download = `${docNum}.pdf`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
  }

  getStudentFullName(): string {
    const user = this.authService.currentUser();
    if (!user) return 'Student';
    return `${user.firstName || ''} ${user.lastName || ''}`.trim() || user.email;
  }
}
