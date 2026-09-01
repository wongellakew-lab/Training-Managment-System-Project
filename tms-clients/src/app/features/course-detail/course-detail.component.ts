import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { CourseService } from '../../services/course.service';
import { EnrollmentService } from '../../services/enrollment.service';
import { AuthService } from '../../services/auth.service';
import { Course } from '../../models/course.model';
import { Enrollment } from '../../models/enrollment.model';

@Component({
  selector: 'app-course-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    LayoutComponent,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './course-detail.component.html',
  styleUrls: ['./course-detail.component.scss']
})
export class CourseDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private courseService = inject(CourseService);
  private enrollmentService = inject(EnrollmentService);
  authService = inject(AuthService);

  course = signal<Course | null>(null);
  userEnrollment = signal<Enrollment | null>(null);
  isLoading = signal(true);
  isEnrolling = signal(false);
  enrollmentMessage = signal<string | null>(null);
  isEnrollSuccess = signal(true);

  readonly isStudent = computed(() => this.authService.userRole() === 'Student');

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      const id = parseInt(idParam, 10);
      this.loadCourse(id);
    }
  }

  loadCourse(id: number): void {
    this.isLoading.set(true);
    this.courseService.getCourseById(id).subscribe({
      next: (c) => {
        this.course.set(c);
        this.checkExistingEnrollment(id);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  checkExistingEnrollment(courseId: number): void {
    const studentId = this.authService.currentUser()?.studentId;
    if (!studentId) return;

    this.enrollmentService.getEnrollments(studentId, courseId).subscribe({
      next: (list) => {
        if (list && list.length > 0) {
          this.userEnrollment.set(list[0]);
        }
      },
      error: () => {}
    });
  }

  enroll(): void {
    const c = this.course();
    if (!c) return;

    const studentId = this.authService.currentUser()?.studentId || 1;
    this.isEnrolling.set(true);
    this.enrollmentMessage.set(null);

    this.enrollmentService.enrollStudent({
      studentId: studentId,
      courseId: c.id,
      batch: 'Batch 1',
      fee: c.price,
      paymentStatus: 'Pending'
    }).subscribe({
      next: (res) => {
        this.userEnrollment.set(res);
        this.isEnrolling.set(false);
        this.isEnrollSuccess.set(true);
        this.enrollmentMessage.set('Enrollment submitted successfully! Awaiting instructor approval.');
      },
      error: (err) => {
        this.isEnrolling.set(false);
        this.isEnrollSuccess.set(false);
        this.enrollmentMessage.set(err.error?.message || err.message || 'Enrollment failed. You might already be enrolled.');
      }
    });
  }

  goBack(): void {
    const role = this.authService.userRole();
    if (role === 'Instructor') {
      this.router.navigate(['/instructor/courses']);
    } else if (role === 'Admin') {
      this.router.navigate(['/admin/courses']);
    } else {
      this.router.navigate(['/student/courses']);
    }
  }
}
