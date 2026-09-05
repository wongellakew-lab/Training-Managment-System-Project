import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { InstructorWorkspaceService } from '../../services/instructor-workspace.service';
import { AssignmentItem, InstructorCourseSummary, CourseAssessmentConfig } from '../../models/instructor-workspace.model';

@Component({
  selector: 'app-instructor-assignments',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    LayoutComponent,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatFormFieldModule,
    MatInputModule,
    MatSlideToggleModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './instructor-assignments.component.html',
  styleUrls: ['./instructor-assignments.component.scss']
})
export class InstructorAssignmentsComponent implements OnInit {
  private instructorService = inject(InstructorWorkspaceService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  courses = signal<InstructorCourseSummary[]>([]);
  selectedCourseId = signal<number | null>(null);
  assignments = signal<AssignmentItem[]>([]);
  assessmentConfig = signal<CourseAssessmentConfig | null>(null);
  isLoading = signal<boolean>(true);
  isSaving = signal<boolean>(false);

  // Dialog State
  showCreateModal = signal<boolean>(false);
  isEditing = signal<boolean>(false);
  editingId = signal<number | null>(null);

  formTitle = signal<string>('');
  formInstructions = signal<string>('');
  formAssignmentNumber = signal<number | null>(null);
  formMaxPoints = signal<number>(100);
  formDueDate = signal<string>('');
  formIsPublished = signal<boolean>(true);
  selectedAttachmentFile: File | null = null;

  ngOnInit(): void {
    const queryCourseId = this.route.snapshot.queryParamMap.get('courseId');

    this.instructorService.getMyCourses().subscribe({
      next: (list) => {
        this.courses.set(list || []);
        if (queryCourseId) {
          this.selectedCourseId.set(Number(queryCourseId));
        } else if (list.length > 0) {
          this.selectedCourseId.set(list[0].id);
        }
        if (this.selectedCourseId()) {
          this.loadAssignments();
        } else {
          this.isLoading.set(false);
        }
      },
      error: () => this.isLoading.set(false)
    });
  }

  loadAssignments(): void {
    const cid = this.selectedCourseId();
    if (!cid) return;

    this.isLoading.set(true);

    this.instructorService.getCourseAssessmentConfig(cid).subscribe({
      next: (config) => {
        this.assessmentConfig.set(config);
      },
      error: (err) => console.error('Failed to load assessment config:', err)
    });

    this.instructorService.getAssignments(cid).subscribe({
      next: (list) => {
        this.assignments.set(list || []);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load assignments:', err);
        this.isLoading.set(false);
      }
    });
  }

  onCourseChange(): void {
    this.loadAssignments();
  }

  openCreateDialog(): void {
    const config = this.assessmentConfig();
    const available = config?.availableAssignmentNumbers || [];

    this.isEditing.set(false);
    this.editingId.set(null);
    this.formTitle.set('');
    this.formInstructions.set('');
    this.formAssignmentNumber.set(available.length > 0 ? available[0] : null);
    this.formMaxPoints.set(100);
    const d = new Date();
    d.setDate(d.getDate() + 7);
    this.formDueDate.set(d.toISOString().slice(0, 16));
    this.formIsPublished.set(true);
    this.selectedAttachmentFile = null;
    this.showCreateModal.set(true);
  }

  openEditDialog(a: AssignmentItem): void {
    this.isEditing.set(true);
    this.editingId.set(a.id);
    this.formTitle.set(a.title);
    this.formInstructions.set(a.instructions);
    this.formAssignmentNumber.set(a.assignmentNumber || 1);
    this.formMaxPoints.set(a.maxPoints);
    this.formDueDate.set(new Date(a.dueDate).toISOString().slice(0, 16));
    this.formIsPublished.set(a.isPublished);
    this.selectedAttachmentFile = null;
    this.showCreateModal.set(true);
  }

  closeDialog(): void {
    this.showCreateModal.set(false);
  }

  onAttachmentFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedAttachmentFile = input.files[0];
    }
  }

  saveAssignment(): void {
    const cid = this.selectedCourseId();
    if (!cid || !this.formTitle().trim()) return;

    this.isSaving.set(true);

    if (this.isEditing() && this.editingId()) {
      this.instructorService.updateAssignment(this.editingId()!, {
        title: this.formTitle(),
        instructions: this.formInstructions(),
        maxPoints: this.formMaxPoints(),
        dueDate: this.formDueDate(),
        isPublished: this.formIsPublished()
      }).subscribe({
        next: () => {
          if (this.selectedAttachmentFile) {
            this.instructorService.uploadAssignmentAttachment(this.editingId()!, this.selectedAttachmentFile).subscribe({
              next: () => this.finalizeSave()
            });
          } else {
            this.finalizeSave();
          }
        },
        error: () => this.isSaving.set(false)
      });
    } else {
      this.instructorService.createAssignment({
        courseId: cid,
        assignmentNumber: this.formAssignmentNumber() ? Number(this.formAssignmentNumber()) : undefined,
        title: this.formTitle(),
        instructions: this.formInstructions(),
        maxPoints: this.formMaxPoints(),
        dueDate: this.formDueDate(),
        isPublished: this.formIsPublished()
      }).subscribe({
        next: (res) => {
          if (this.selectedAttachmentFile && res.id) {
            this.instructorService.uploadAssignmentAttachment(res.id, this.selectedAttachmentFile).subscribe({
              next: () => this.finalizeSave()
            });
          } else {
            this.finalizeSave();
          }
        },
        error: (err) => {
          this.isSaving.set(false);
          const errMsg = err?.error?.message || 'Failed to create assignment. Please verify the assignment number and fields.';
          this.snackBar.open(errMsg, 'Close', { duration: 4000 });
        }
      });
    }
  }

  private finalizeSave(): void {
    this.isSaving.set(false);
    this.closeDialog();
    this.snackBar.open('Assignment saved successfully!', 'OK', { duration: 3000 });
    this.loadAssignments();
  }

  deleteAssignment(id: number): void {
    if (!confirm('Are you sure you want to remove this assignment?')) return;

    this.instructorService.deleteAssignment(id).subscribe({
      next: () => {
        this.snackBar.open('Assignment deleted.', 'OK', { duration: 2500 });
        this.loadAssignments();
      }
    });
  }

  viewSubmissions(assignmentId: number): void {
    this.router.navigate(['/instructor/submissions'], {
      queryParams: {
        assignmentId,
        courseId: this.selectedCourseId()
      }
    });
  }
}
