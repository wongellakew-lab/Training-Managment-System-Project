import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { InstructorWorkspaceService } from '../../services/instructor-workspace.service';
import { CourseAttendanceResponse, AttendanceStudentItem, InstructorCourseSummary } from '../../models/instructor-workspace.model';

@Component({
  selector: 'app-instructor-attendance',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LayoutComponent,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  templateUrl: './instructor-attendance.component.html',
  styleUrls: ['./instructor-attendance.component.scss']
})
export class InstructorAttendanceComponent implements OnInit {
  private instructorService = inject(InstructorWorkspaceService);
  private route = inject(ActivatedRoute);
  private snackBar = inject(MatSnackBar);

  readonly todayDateStr = new Date().toISOString().split('T')[0];

  courses = signal<InstructorCourseSummary[]>([]);
  selectedCourseId = signal<number | null>(null);
  selectedDate = signal<string>(this.todayDateStr);
  maxDate = signal<string>(this.todayDateStr);

  attendanceData = signal<CourseAttendanceResponse | null>(null);
  students = signal<AttendanceStudentItem[]>([]);
  hasScheduledSession = signal<boolean>(true);
  isRecorded = signal<boolean>(false);
  scheduleInfo = signal<any>(null);

  isLoading = signal<boolean>(true);
  isSaving = signal<boolean>(false);

  // Date Navigation State
  isToday = computed(() => this.selectedDate() === this.todayDateStr);
  canGoNext = computed(() => this.selectedDate() < this.todayDateStr);

  // Computed summary counts based on current student pills
  presentCount = computed(() => this.students().filter(s => s.statusCode === 1).length);
  lateCount = computed(() => this.students().filter(s => s.statusCode === 2).length);
  absentCount = computed(() => this.students().filter(s => s.statusCode === 3).length);
  unmarkedCount = computed(() => this.students().filter(s => s.statusCode === 0).length);

  attendanceRate = computed(() => {
    const total = this.students().length;
    if (total === 0) return 0;
    const attended = this.presentCount() + this.lateCount();
    return Math.round((attended / total) * 100);
  });

  ngOnInit(): void {
    const queryCourseId = this.route.snapshot.queryParamMap.get('courseId');
    const queryDate = this.route.snapshot.queryParamMap.get('date');

    if (queryDate) {
      this.selectedDate.set(queryDate);
    }

    this.instructorService.getMyCourses().subscribe({
      next: (courseList) => {
        this.courses.set(courseList);
        if (queryCourseId) {
          this.selectedCourseId.set(Number(queryCourseId));
        } else if (courseList.length > 0) {
          this.selectedCourseId.set(courseList[0].id);
        }
        if (this.selectedCourseId()) {
          this.loadAttendance();
        } else {
          this.isLoading.set(false);
        }
      },
      error: (err) => {
        console.error('Failed to load courses:', err);
        this.isLoading.set(false);
      }
    });
  }

  loadAttendance(): void {
    const cid = this.selectedCourseId();
    if (!cid) return;

    this.isLoading.set(true);
    this.instructorService.getCourseAttendance(cid, this.selectedDate()).subscribe({
      next: (resp) => {
        this.attendanceData.set(resp);
        this.students.set(resp.students || []);
        this.hasScheduledSession.set(resp.hasScheduledSession ?? true);
        this.isRecorded.set(resp.isRecorded ?? false);
        this.scheduleInfo.set(resp.scheduleInfo || null);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load attendance:', err);
        this.isLoading.set(false);
      }
    });
  }

  onCourseChange(): void {
    this.loadAttendance();
  }

  onDateChange(): void {
    this.loadAttendance();
  }

  goToPreviousDay(): void {
    const current = new Date(this.selectedDate() + 'T00:00:00');
    current.setDate(current.getDate() - 1);
    this.selectedDate.set(current.toISOString().split('T')[0]);
    this.loadAttendance();
  }

  goToNextDay(): void {
    if (!this.canGoNext()) return;
    const current = new Date(this.selectedDate() + 'T00:00:00');
    current.setDate(current.getDate() + 1);
    this.selectedDate.set(current.toISOString().split('T')[0]);
    this.loadAttendance();
  }

  goToToday(): void {
    this.selectedDate.set(this.todayDateStr);
    this.loadAttendance();
  }

  setStatus(student: AttendanceStudentItem, status: number): void {
    const updated = this.students().map(s => {
      if (s.studentId === student.studentId) {
        const statusName = status === 1 ? 'Present' : status === 2 ? 'Late' : 'Absent';
        return { ...s, statusCode: status, status: statusName as 'Present' | 'Late' | 'Absent' };
      }
      return s;
    });
    this.students.set(updated);
  }

  markAllPresent(): void {
    const updated = this.students().map(s => ({
      ...s,
      statusCode: 1,
      status: 'Present' as const
    }));
    this.students.set(updated);
    this.snackBar.open('Marked all students as Present. Click "Save Attendance" to persist.', 'OK', { duration: 3000 });
  }

  saveAttendance(): void {
    const cid = this.selectedCourseId();
    if (!cid) return;

    this.isSaving.set(true);
    const items = this.students().map(s => ({
      studentId: s.studentId,
      status: s.statusCode > 0 ? s.statusCode : 1, // default to present if unmarked
      remarks: s.remarks
    }));

    this.instructorService.bulkAttendance({
      courseId: cid,
      sessionDate: this.selectedDate(),
      items
    }).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.snackBar.open('Attendance roll call recorded successfully in database!', 'OK', { duration: 3500 });
        this.loadAttendance();
      },
      error: (err) => {
        this.isSaving.set(false);
        console.error('Failed to save attendance:', err);
        this.snackBar.open('Failed to save attendance. Please try again.', 'Dismiss', { duration: 4000 });
      }
    });
  }
}
