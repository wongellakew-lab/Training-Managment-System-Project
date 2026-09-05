import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { StudentAssignmentService } from '../../services/student-assignment.service';
import { LiveSyncService } from '../../services/live-sync.service';
import { StudentAssignmentItem } from '../../models/student-assignment.model';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-student-assignments',
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
    MatSelectModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './student-assignments.component.html',
  styleUrls: ['./student-assignments.component.scss']
})
export class StudentAssignmentsComponent implements OnInit, OnDestroy {
  private assignmentService = inject(StudentAssignmentService);
  private liveSync = inject(LiveSyncService);
  private snackBar = inject(MatSnackBar);
  private route = inject(ActivatedRoute);

  assignments = signal<StudentAssignmentItem[]>([]);
  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);

  // Filters
  activeFilter = signal<'All' | 'Pending' | 'Submitted' | 'Graded'>('All');
  selectedCourseId = signal<number | 'all'>('all');
  searchQuery = signal<string>('');

  // Submission Modal State
  isModalOpen = signal<boolean>(false);
  activeAssignment = signal<StudentAssignmentItem | null>(null);
  selectedFile = signal<File | null>(null);
  submissionText = signal<string>('');
  isSubmitting = signal<boolean>(false);

  private sub = new Subscription();

  readonly courseOptions = computed(() => {
    const list = this.assignments();
    const map = new Map<number, string>();
    for (const a of list) {
      if (!map.has(a.courseId)) {
        map.set(a.courseId, `${a.courseCode} - ${a.courseTitle}`);
      }
    }
    return Array.from(map.entries()).map(([id, title]) => ({ id, title }));
  });

  readonly stats = computed(() => {
    const list = this.assignments();
    return {
      total: list.length,
      pending: list.filter(a => a.status === 'Pending').length,
      submitted: list.filter(a => a.status === 'Submitted').length,
      graded: list.filter(a => a.status === 'Graded').length
    };
  });

  readonly filteredAssignments = computed(() => {
    let list = this.assignments();

    // Status filter
    const status = this.activeFilter();
    if (status !== 'All') {
      list = list.filter(a => a.status === status);
    }

    // Course filter
    const courseId = this.selectedCourseId();
    if (courseId !== 'all') {
      list = list.filter(a => a.courseId === courseId);
    }

    // Search query
    const q = this.searchQuery().trim().toLowerCase();
    if (q) {
      list = list.filter(a =>
        a.title.toLowerCase().includes(q) ||
        a.courseTitle.toLowerCase().includes(q) ||
        a.courseCode.toLowerCase().includes(q) ||
        (a.instructions && a.instructions.toLowerCase().includes(q))
      );
    }

    return list;
  });

  ngOnInit(): void {
    this.loadAssignments();

    // Handle query param ?id=123 (e.g. clicked from dashboard)
    this.route.queryParamMap.subscribe(params => {
      const idStr = params.get('id');
      if (idStr) {
        const id = parseInt(idStr, 10);
        this.openSubmissionForId(id);
      }
    });

    // Real-time updates via SignalR
    this.sub.add(
      this.liveSync.assignmentUpdated$.subscribe(() => {
        this.loadAssignments(false);
      })
    );

    this.sub.add(
      this.liveSync.gradePublished$.subscribe(() => {
        this.loadAssignments(false);
      })
    );
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }

  loadAssignments(showSpinner = true): void {
    if (showSpinner) this.isLoading.set(true);
    this.errorMessage.set(null);

    this.assignmentService.getMyAssignments().subscribe({
      next: (data) => {
        this.assignments.set(data || []);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load assignments', err);
        this.errorMessage.set('Unable to load assignments from database. Please try again.');
        this.isLoading.set(false);
      }
    });
  }

  setFilter(filter: 'All' | 'Pending' | 'Submitted' | 'Graded'): void {
    this.activeFilter.set(filter);
  }

  openSubmitModal(asg: StudentAssignmentItem): void {
    this.activeAssignment.set(asg);
    this.selectedFile.set(null);
    this.submissionText.set(asg.submissionText || '');
    this.isModalOpen.set(true);
  }

  closeModal(): void {
    if (this.isSubmitting()) return;
    this.isModalOpen.set(false);
    this.activeAssignment.set(null);
    this.selectedFile.set(null);
    this.submissionText.set('');
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      if (file.size > 25 * 1024 * 1024) {
        this.snackBar.open('File exceeds maximum limit of 25MB.', 'Close', { duration: 4000 });
        input.value = '';
        return;
      }
      this.selectedFile.set(file);
    }
  }

  removeSelectedFile(): void {
    this.selectedFile.set(null);
  }

  submitWork(): void {
    const asg = this.activeAssignment();
    if (!asg) return;

    const file = this.selectedFile();
    const text = this.submissionText().trim();

    if (!file && !text) {
      this.snackBar.open('Please attach a file or type a written response.', 'OK', { duration: 3500 });
      return;
    }

    this.isSubmitting.set(true);
    this.assignmentService.submitAssignment(asg.id, file || undefined, text || undefined).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        this.snackBar.open(res.message || 'Assignment submitted successfully!', 'Close', { duration: 4000 });
        this.closeModal();
        this.loadAssignments(false);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        const msg = err.error?.message || 'Failed to submit assignment. Please verify rules and try again.';
        this.snackBar.open(msg, 'Close', { duration: 5000 });
      }
    });
  }

  getDownloadUrl(relativeUrl?: string): string {
    if (!relativeUrl) return '#';
    return this.assignmentService.getDownloadUrl(relativeUrl);
  }

  private openSubmissionForId(id: number): void {
    const asg = this.assignments().find(a => a.id === id);
    if (asg && asg.canSubmit) {
      this.openSubmitModal(asg);
    }
  }
}
