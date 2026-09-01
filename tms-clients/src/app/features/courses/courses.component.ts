import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { LayoutComponent } from '../../ui/layout/layout.component';
import { CourseService } from '../../services/course.service';
import { Course } from '../../models/course.model';

@Component({
  selector: 'app-courses-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    LayoutComponent
  ],
  templateUrl: './courses.component.html',
  styleUrls: ['./courses.component.scss']
})
export class CoursesComponent implements OnInit {
  private courseService = inject(CourseService);

  readonly courses = signal<Course[]>([]);
  readonly selectedStatus = signal<string>('All');
  readonly selectedCategory = signal<string>('All Category');
  readonly viewMode = signal<'list' | 'grid'>('grid');
  readonly selectedCourse = signal<Course | null>(null);
  readonly showDetailsDrawer = signal<boolean>(false);
  readonly isLoading = signal<boolean>(false);

  // Pagination
  currentPage = 1;
  pageSize = 8;
  totalCount = 320;

  ngOnInit(): void {
    this.loadCourses();
  }

  loadCourses(): void {
    this.isLoading.set(true);
    this.courseService.getCourses({
      pageNumber: this.currentPage,
      pageSize: this.pageSize,
      status: this.selectedStatus() === 'All' ? undefined : this.selectedStatus(),
      category: this.selectedCategory() === 'All Category' ? undefined : this.selectedCategory()
    }).subscribe({
      next: (res) => {
        this.courses.set(res.items);
        if (res.totalCount) this.totalCount = res.totalCount;
        this.isLoading.set(false);

        // Auto-select Python for Beginners for detail view preview if none selected
        if (!this.selectedCourse() && res.items.length > 0) {
          const python = res.items.find(c => c.title.includes('Python')) || res.items[0];
          this.selectedCourse.set(python);
        }
      },
      error: () => this.isLoading.set(false)
    });
  }

  filterStatus(status: string): void {
    this.selectedStatus.set(status);
    this.currentPage = 1;
    this.loadCourses();
  }

  filterCategory(cat: string): void {
    this.selectedCategory.set(cat);
    this.currentPage = 1;
    this.loadCourses();
  }

  toggleViewMode(mode: 'list' | 'grid'): void {
    this.viewMode.set(mode);
  }

  openCourseDetails(course: Course): void {
    this.selectedCourse.set(course);
    this.showDetailsDrawer.set(true);
  }

  closeCourseDetails(): void {
    this.showDetailsDrawer.set(false);
  }

  setPage(p: number): void {
    if (p < 1) return;
    this.currentPage = p;
    this.loadCourses();
  }

  getCategoryClass(category?: string): string {
    const cat = (category || 'design').toLowerCase();
    if (cat.includes('web')) return 'category-webdev';
    if (cat.includes('market')) return 'category-marketing';
    if (cat.includes('commun')) return 'category-communication';
    if (cat.includes('busi')) return 'category-business';
    if (cat.includes('well')) return 'category-wellness';
    if (cat.includes('lang')) return 'category-language';
    return 'category-design';
  }

  getStatusClass(status?: string): string {
    const s = (status || 'active').toLowerCase();
    if (s === 'active') return 'active';
    if (s === 'draft') return 'draft';
    if (s === 'archived') return 'archived';
    return 'active';
  }

  downloadResource(resourceName: string): void {
    alert(`Starting download for ${resourceName}...`);
  }
}
