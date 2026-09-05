import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NavbarComponent } from '../../ui/navbar/navbar.component';
import { CourseService } from '../../services/course.service';
import { Course, CreateCourseDto, UpdateCourseDto } from '../../models/course.model';

@Component({
  selector: 'app-admin-course-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    NavbarComponent,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './admin-course-list.component.html',
  styleUrls: ['./admin-course-list.component.scss']
})
export class AdminCourseListComponent implements OnInit {
  private courseService = inject(CourseService);
  private router = inject(Router);

  courses = signal<Course[]>([]);
  isLoading = signal(false);
  isSaving = signal(false);
  alertMessage = signal<string | null>(null);
  isSuccess = signal(true);

  selectedStatus = signal<string>('All');
  searchTerm = signal<string>('');

  showModal = signal(false);
  editingCourseId = signal<number | null>(null);

  displayedColumns = ['code', 'title', 'status', 'credits', 'capacity', 'instructor', 'actions'];

  formData = {
    code: '',
    title: '',
    department: 'Computer Science',
    credits: 3,
    maxCapacity: 30,
    description: ''
  };

  ngOnInit(): void {
    this.loadCourses();
  }

  loadCourses(): void {
    this.isLoading.set(true);
    const filter: any = { pageSize: 100 };
    if (this.selectedStatus() !== 'All') {
      filter.status = this.selectedStatus();
    }
    if (this.searchTerm().trim()) {
      filter.searchTerm = this.searchTerm().trim();
    }

    this.courseService.getCourses(filter).subscribe({
      next: (res) => {
        this.courses.set(res.items);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  filterByStatus(status: string): void {
    this.selectedStatus.set(status);
    this.loadCourses();
  }

  onSearch(term: string): void {
    this.searchTerm.set(term);
    this.loadCourses();
  }

  viewCourse(course: Course): void {
    this.router.navigate(['/admin/courses', course.id]);
  }

  getStatusClass(status?: string): string {
    const s = (status || 'draft').toLowerCase();
    if (s === 'active' || s === 'published') return 'active';
    if (s === 'under review') return 'under-review';
    if (s === 'rejected') return 'rejected';
    if (s === 'draft') return 'draft';
    return 'draft';
  }

  openCreateModal(): void {
    this.editingCourseId.set(null);
    this.formData = {
      code: '',
      title: '',
      department: 'Computer Science',
      credits: 3,
      maxCapacity: 30,
      description: ''
    };
    this.showModal.set(true);
  }

  openEditModal(course: Course): void {
    this.editingCourseId.set(course.id);
    this.formData = {
      code: course.code,
      title: course.title,
      department: course.department,
      credits: course.credits,
      maxCapacity: course.maxCapacity,
      description: course.description
    };
    this.showModal.set(true);
  }

  saveCourse(): void {
    this.isSaving.set(true);
    const id = this.editingCourseId();

    if (id) {
      const updateDto: UpdateCourseDto = {
        title: this.formData.title,
        description: this.formData.description,
        credits: this.formData.credits,
        maxCapacity: this.formData.maxCapacity,
        department: this.formData.department
      };
      this.courseService.updateCourse(id, updateDto).subscribe({
        next: () => {
          this.isSaving.set(false);
          this.showModal.set(false);
          this.isSuccess.set(true);
          this.alertMessage.set('Course updated successfully!');
          this.loadCourses();
        },
        error: (err) => {
          this.isSaving.set(false);
          this.isSuccess.set(false);
          this.alertMessage.set(err.message || 'Failed to update course.');
        }
      });
    } else {
      const createDto: CreateCourseDto = {
        code: this.formData.code,
        title: this.formData.title,
        description: this.formData.description,
        credits: this.formData.credits,
        maxCapacity: this.formData.maxCapacity,
        department: this.formData.department
      };
      this.courseService.createCourse(createDto).subscribe({
        next: () => {
          this.isSaving.set(false);
          this.showModal.set(false);
          this.isSuccess.set(true);
          this.alertMessage.set('Course created successfully!');
          this.loadCourses();
        },
        error: (err) => {
          this.isSaving.set(false);
          this.isSuccess.set(false);
          this.alertMessage.set(err.message || 'Failed to create course.');
        }
      });
    }
  }

  deleteCourse(id: number): void {
    if (confirm('Are you sure you want to delete this course from the catalog?')) {
      this.courseService.deleteCourse(id).subscribe({
        next: () => {
          this.isSuccess.set(true);
          this.alertMessage.set('Course deleted.');
          this.loadCourses();
        },
        error: (err) => {
          this.isSuccess.set(false);
          this.alertMessage.set(err.message || 'Failed to delete course.');
        }
      });
    }
  }
}
