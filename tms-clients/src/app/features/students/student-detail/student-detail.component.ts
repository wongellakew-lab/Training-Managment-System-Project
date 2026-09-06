import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { LayoutComponent } from '../../../ui/layout/layout.component';
import { StudentService, AdminStudentDetail } from '../../../services/student.service';

export type EnrollmentFilter = 'All' | 'Active' | 'Completed' | 'Dropped';

@Component({
  selector: 'app-student-detail',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatProgressBarModule,
    LayoutComponent
  ],
  templateUrl: './student-detail.component.html',
  styleUrls: ['./student-detail.component.scss']
})
export class StudentDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private studentService = inject(StudentService);

  readonly student = signal<AdminStudentDetail | null>(null);
  readonly isLoading = signal<boolean>(true);
  readonly isProcessing = signal<boolean>(false);
  readonly error = signal<string>('');
  readonly selectedFilter = signal<EnrollmentFilter>('All');

  readonly totalCoursesCount = computed(() => {
    return this.student()?.enrolledCourses?.length || 0;
  });

  readonly activeEnrolledCount = computed(() => {
    const list = this.student()?.enrolledCourses || [];
    return list.filter(c => c.enrollmentStatus.toLowerCase() === 'active' || c.enrollmentStatus.toLowerCase() === 'enrolled' || c.enrollmentStatus.toLowerCase() === 'approved').length;
  });

  readonly completedCoursesCount = computed(() => {
    const list = this.student()?.enrolledCourses || [];
    return list.filter(c => c.enrollmentStatus.toLowerCase() === 'completed').length;
  });

  readonly droppedCoursesCount = computed(() => {
    const list = this.student()?.enrolledCourses || [];
    return list.filter(c => c.enrollmentStatus.toLowerCase() === 'dropped').length;
  });

  readonly filteredCourses = computed(() => {
    const all = this.student()?.enrolledCourses || [];
    const filter = this.selectedFilter();
    if (filter === 'All') return all;
    if (filter === 'Active') {
      return all.filter(c => c.enrollmentStatus.toLowerCase() === 'active' || c.enrollmentStatus.toLowerCase() === 'enrolled' || c.enrollmentStatus.toLowerCase() === 'approved');
    }
    if (filter === 'Completed') {
      return all.filter(c => c.enrollmentStatus.toLowerCase() === 'completed');
    }
    if (filter === 'Dropped') {
      return all.filter(c => c.enrollmentStatus.toLowerCase() === 'dropped');
    }
    return all;
  });

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const idStr = params.get('id');
      if (idStr) {
        const id = parseInt(idStr, 10);
        if (!isNaN(id)) {
          this.loadStudent(id);
          return;
        }
      }
      this.error.set('Invalid student ID specified.');
      this.isLoading.set(false);
    });
  }

  loadStudent(id: number): void {
    this.isLoading.set(true);
    this.error.set('');

    this.studentService.getStudentDetail(id).subscribe({
      next: (data) => {
        this.student.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load student detail:', err);
        this.error.set(err.error?.message || 'Could not find the requested student.');
        this.isLoading.set(false);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/admin/students']);
  }

  navigateToCourse(courseId: number): void {
    if (courseId) {
      this.router.navigate(['/admin/courses', courseId]);
    }
  }

  suspendStudent(): void {
    const s = this.student();
    if (!s) return;
    if (!confirm(`Are you sure you want to suspend ${s.fullName}? This will lock their account and prevent login.`)) {
      return;
    }

    this.isProcessing.set(true);
    this.studentService.suspendStudent(s.id).subscribe({
      next: () => {
        this.isProcessing.set(false);
        this.loadStudent(s.id);
      },
      error: (err) => {
        this.isProcessing.set(false);
        alert(err.error?.message || 'Failed to suspend student.');
      }
    });
  }

  placeOnProbation(): void {
    const s = this.student();
    if (!s) return;
    if (!confirm(`Place ${s.fullName} on academic probation? Status will be updated without locking login.`)) {
      return;
    }

    this.isProcessing.set(true);
    this.studentService.placeOnProbation(s.id).subscribe({
      next: () => {
        this.isProcessing.set(false);
        this.loadStudent(s.id);
      },
      error: (err) => {
        this.isProcessing.set(false);
        alert(err.error?.message || 'Failed to place student on probation.');
      }
    });
  }

  activateStudent(): void {
    const s = this.student();
    if (!s) return;
    if (!confirm(`Activate account for ${s.fullName}?`)) {
      return;
    }

    this.isProcessing.set(true);
    this.studentService.activateStudent(s.id).subscribe({
      next: () => {
        this.isProcessing.set(false);
        this.loadStudent(s.id);
      },
      error: (err) => {
        this.isProcessing.set(false);
        alert(err.error?.message || 'Failed to activate student.');
      }
    });
  }

  setFilter(filter: EnrollmentFilter): void {
    this.selectedFilter.set(filter);
  }

  onAvatarError(event: Event): void {
    const img = event.target as HTMLImageElement;
    if (img) {
      img.style.display = 'none';
    }
  }
}
