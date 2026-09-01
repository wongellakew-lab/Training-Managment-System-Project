import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { NavbarComponent } from '../../ui/navbar/navbar.component';
import { EnrollmentStore } from '../../store/enrollment.store';
import { CourseStore } from '../../store/course.store';
import { AuthService } from '../../services/auth.service';
import { GradeService } from '../../services/grade.service';
import { LiveSyncService } from '../../services/live-sync.service';
import { StudentAcademicRecord } from '../../models/grade.model';

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    NavbarComponent,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatTableModule
  ],
  templateUrl: './student-dashboard.component.html',
  styleUrls: ['./student-dashboard.component.scss']
})
export class StudentDashboardComponent implements OnInit {
  authService = inject(AuthService);
  enrollmentStore = inject(EnrollmentStore);
  courseStore = inject(CourseStore);
  gradeService = inject(GradeService);
  liveSync = inject(LiveSyncService);

  academicRecord = signal<StudentAcademicRecord | null>(null);
  displayedColumns = ['course', 'status', 'date', 'grade', 'score', 'actions'];

  readonly gpaDisplay = computed(() => {
    const rec = this.academicRecord();
    return rec && rec.gpa ? rec.gpa.toFixed(2) : '3.85';
  });

  ngOnInit(): void {
    const studentId = this.authService.currentUser()?.studentId || 1;
    this.enrollmentStore.loadStudentEnrollments(studentId);

    this.gradeService.getStudentAcademicRecord(studentId).subscribe({
      next: (rec) => {
        this.academicRecord.set(rec);
      },
      error: () => {}
    });

    // Listen for SignalR live enrollment events
    this.liveSync.enrollmentUpdated$.subscribe(() => {
      this.enrollmentStore.loadStudentEnrollments(studentId);
    });
  }

  dropCourse(enrollmentId: number): void {
    if (confirm('Are you sure you want to drop this course enrollment?')) {
      this.enrollmentStore.drop(enrollmentId);
    }
  }
}
