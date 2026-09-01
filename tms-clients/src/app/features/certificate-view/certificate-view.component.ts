import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { NavbarComponent } from '../../ui/navbar/navbar.component';
import { CertificateService } from '../../services/certificate.service';
import { TranscriptService } from '../../services/transcript.service';
import { AuthService } from '../../services/auth.service';
import { CertificateResponse, TranscriptJobResponse, TranscriptStatusResponse } from '../../models/transcript.model';
import { LiveSyncService } from '../../services/live-sync.service';

@Component({
  selector: 'app-certificate-view',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, NavbarComponent],
  template: `
    <div class="min-h-screen bg-slate-950 px-4 sm:px-8 py-6 max-w-7xl mx-auto">
      <app-navbar></app-navbar>

      <!-- Header -->
      <div class="glass-panel p-6 sm:p-8 mb-8 relative overflow-hidden">
        <div class="absolute right-0 top-0 w-80 h-80 bg-emerald-500/10 rounded-full blur-3xl pointer-events-none"></div>
        <div class="relative z-10 flex flex-col md:flex-row md:items-center md:justify-between gap-4">
          <div>
            <div class="flex items-center gap-2 mb-2">
              <span class="badge badge-success text-[10px]">Academic Credentials</span>
              <span class="text-xs text-slate-400">Official Verification</span>
            </div>
            <h1 class="text-3xl font-black text-white tracking-tight">Certificates & Transcripts</h1>
            <p class="text-xs text-slate-400 mt-1">Request async transcript generation with Channel background workers and verify certificate codes.</p>
          </div>

          <button (click)="requestTranscript()" [disabled]="isRequestingTranscript()" class="btn-primary text-xs py-2.5 px-4">
            <span class="material-icons text-sm">description</span>
            <span>{{ isRequestingTranscript() ? 'Queuing Worker...' : 'Request Official Transcript' }}</span>
          </button>
        </div>
      </div>

      <!-- Transcript Job Status Banner -->
      @if (transcriptJob()) {
        <div class="glass-panel p-6 mb-8 border border-indigo-500/30 bg-indigo-950/20">
          <div class="flex items-start justify-between">
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-xl bg-indigo-600/30 flex items-center justify-center text-indigo-400">
                <span class="material-icons">hourglass_top</span>
              </div>
              <div>
                <h3 class="text-base font-bold text-white">Transcript Job Queued (#{{ transcriptJob()?.jobId }})</h3>
                <p class="text-xs text-indigo-300">
                  Status: <strong>{{ transcriptStatus()?.status || transcriptJob()?.status }}</strong> • Est. Wait: {{ transcriptJob()?.estimatedWaitSeconds }}s
                </p>
              </div>
            </div>

            @if (transcriptStatus()?.status === 'Completed' || transcriptStatus()?.downloadUrl) {
              <a [href]="transcriptStatus()?.downloadUrl" target="_blank" class="btn-primary text-xs py-1.5 px-3">
                <span class="material-icons text-xs">download</span>
                <span>Download Transcript PDF</span>
              </a>
            } @else {
              <div class="flex items-center gap-2 text-xs text-indigo-400">
                <span class="inline-block animate-spin w-4 h-4 border-2 border-indigo-400 border-t-transparent rounded-full"></span>
                <span>Processing in background channel...</span>
              </div>
            }
          </div>
        </div>
      }

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <!-- Certificates List (2 Cols) -->
        <div class="lg:col-span-2 glass-panel p-6 sm:p-8">
          <h2 class="text-xl font-bold text-white mb-1">Earned Course Certificates</h2>
          <p class="text-xs text-slate-400 mb-6">Verified credentials issued upon course completion</p>

          @if (isLoading()) {
            <div class="py-12 text-center text-slate-400 text-sm">
              <span class="inline-block animate-spin w-6 h-6 border-2 border-indigo-500 border-t-transparent rounded-full mb-2"></span>
              <div>Loading certificate records...</div>
            </div>
          } @else if (certificates().length === 0) {
            <div class="py-12 text-center">
              <span class="material-icons text-4xl text-slate-600 mb-2">workspace_premium</span>
              <h3 class="text-sm font-semibold text-slate-300">No certificates awarded yet</h3>
              <p class="text-xs text-slate-500 mt-1">Complete courses with passing grades to receive certified diplomas.</p>
            </div>
          } @else {
            <div class="space-y-4">
              @for (cert of certificates(); track cert.id) {
                <div class="p-5 rounded-xl bg-slate-900/70 border border-white/10 flex items-center justify-between">
                  <div class="flex items-center gap-4">
                    <div class="w-12 h-12 rounded-xl bg-emerald-500/20 text-emerald-400 flex items-center justify-center border border-emerald-500/30">
                      <span class="material-icons text-2xl">verified</span>
                    </div>
                    <div>
                      <h4 class="font-bold text-white text-sm">{{ cert.courseTitle }}</h4>
                      <div class="text-xs text-slate-400 mt-0.5">
                        Code: <span class="font-mono text-emerald-400 font-bold">{{ cert.certificateCode }}</span>
                      </div>
                      <div class="text-[11px] text-slate-500 mt-0.5">Issued: {{ cert.issuedAt | date:'mediumDate' }}</div>
                    </div>
                  </div>

                  <span class="badge badge-success text-[10px]">Valid Credential</span>
                </div>
              }
            </div>
          }
        </div>

        <!-- Verification Search Card (1 Col) -->
        <div class="glass-panel p-6 sm:p-8 h-fit">
          <h2 class="text-lg font-bold text-white mb-1">Verify Certificate</h2>
          <p class="text-xs text-slate-400 mb-4">Validate authenticity of any certificate code</p>

          <form (ngSubmit)="verifyCode()" class="space-y-3">
            <div>
              <label class="block text-xs font-semibold text-slate-300 mb-1.5">Verification Code</label>
              <input
                type="text"
                [(ngModel)]="verificationCode"
                name="verificationCode"
                placeholder="e.g. CERT-2026-CS101"
                class="form-input text-xs font-mono uppercase"
                required
              />
            </div>
            <button type="submit" [disabled]="isVerifying()" class="btn-secondary w-full text-xs py-2">
              {{ isVerifying() ? 'Validating...' : 'Verify Authenticity' }}
            </button>
          </form>

          @if (verificationResult()) {
            <div class="mt-6 p-4 rounded-xl bg-emerald-500/15 border border-emerald-500/30 text-xs text-emerald-300 space-y-1.5 animate-fade-in">
              <div class="font-bold flex items-center gap-1.5 text-emerald-200">
                <span class="material-icons text-sm">check_circle</span>
                <span>Authentic Certificate</span>
              </div>
              <div>Student: <strong class="text-white">{{ verificationResult()?.studentName }}</strong></div>
              <div>Course: <strong class="text-white">{{ verificationResult()?.courseTitle }}</strong></div>
              <div>Issued: <strong class="text-white">{{ verificationResult()?.issuedAt | date:'mediumDate' }}</strong></div>
            </div>
          } @else if (verificationError()) {
            <div class="mt-6 p-4 rounded-xl bg-rose-500/15 border border-rose-500/30 text-xs text-rose-300">
              {{ verificationError() }}
            </div>
          }
        </div>
      </div>
    </div>
  `
})
export class CertificateViewComponent implements OnInit {
  private certificateService = inject(CertificateService);
  private transcriptService = inject(TranscriptService);
  private authService = inject(AuthService);
  private liveSync = inject(LiveSyncService);

  certificates = signal<CertificateResponse[]>([]);
  isLoading = signal(false);

  transcriptJob = signal<TranscriptJobResponse | null>(null);
  transcriptStatus = signal<TranscriptStatusResponse | null>(null);
  isRequestingTranscript = signal(false);

  verificationCode = '';
  isVerifying = signal(false);
  verificationResult = signal<CertificateResponse | null>(null);
  verificationError = signal<string | null>(null);

  ngOnInit() {
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

  loadCertificates() {
    const studentId = this.authService.currentUser()?.studentId || 1;
    this.isLoading.set(true);
    this.certificateService.getStudentCertificates(studentId).subscribe({
      next: (certs) => {
        this.certificates.set(certs);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  requestTranscript() {
    const studentId = this.authService.currentUser()?.studentId || 1;
    this.isRequestingTranscript.set(true);
    this.transcriptService.requestTranscript(studentId).subscribe({
      next: (job) => {
        this.isRequestingTranscript.set(false);
        this.transcriptJob.set(job);
        this.pollTranscriptStatus(job.jobId);
      },
      error: () => this.isRequestingTranscript.set(false)
    });
  }

  pollTranscriptStatus(jobId: string) {
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

  verifyCode() {
    if (!this.verificationCode) return;
    this.isVerifying.set(true);
    this.verificationResult.set(null);
    this.verificationError.set(null);

    this.certificateService.verifyCertificate(this.verificationCode).subscribe({
      next: (res) => {
        this.isVerifying.set(false);
        this.verificationResult.set(res);
      },
      error: (err) => {
        this.isVerifying.set(false);
        this.verificationError.set('Certificate code is invalid or does not exist.');
      }
    });
  }
}
