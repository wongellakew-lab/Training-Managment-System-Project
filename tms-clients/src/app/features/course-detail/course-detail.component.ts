import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { NavbarComponent } from '../../ui/navbar/navbar.component';
import { CourseService } from '../../services/course.service';
import { Course } from '../../models/course.model';

@Component({
  selector: 'app-course-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, NavbarComponent],
  template: `
    <div class="min-h-screen bg-slate-950 px-4 sm:px-8 py-6 max-w-5xl mx-auto">
      <app-navbar></app-navbar>

      @if (isLoading()) {
        <div class="py-24 text-center text-slate-400">
          <span class="inline-block animate-spin w-8 h-8 border-2 border-indigo-500 border-t-transparent rounded-full mb-3"></span>
          <div class="text-sm">Loading course syllabus and details...</div>
        </div>
      } @else if (course()) {
        <div class="glass-panel p-8 mb-8 border border-white/10 relative overflow-hidden">
          <div class="flex items-center justify-between mb-4">
            <span class="badge badge-primary font-mono text-xs">{{ course()?.code }}</span>
            <span class="text-xs font-semibold text-slate-400 bg-slate-900/80 px-3 py-1.5 rounded-lg border border-white/5">
              {{ course()?.credits }} Academic Credits
            </span>
          </div>

          <h1 class="text-3xl font-black text-white mb-2">{{ course()?.title }}</h1>
          <div class="text-sm text-cyan-400 font-medium mb-6 flex items-center gap-2">
            <span class="material-icons text-base">domain</span>
            <span>{{ course()?.department }}</span>
          </div>

          <div class="p-5 rounded-xl bg-slate-900/60 border border-white/5 mb-8">
            <h3 class="text-xs uppercase font-bold text-slate-400 tracking-wider mb-2">Description & Objectives</h3>
            <p class="text-sm text-slate-300 leading-relaxed">{{ course()?.description }}</p>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-8">
            <div class="p-4 rounded-xl bg-slate-900/40 border border-white/5">
              <span class="text-[11px] uppercase font-bold text-slate-400 block mb-1">Max Class Capacity</span>
              <span class="text-lg font-bold text-white">{{ course()?.maxCapacity }} Students</span>
            </div>
            <div class="p-4 rounded-xl bg-slate-900/40 border border-white/5">
              <span class="text-[11px] uppercase font-bold text-slate-400 block mb-1">Currently Enrolled</span>
              <span class="text-lg font-bold text-indigo-300">{{ course()?.enrolledCount }} Students</span>
            </div>
            <div class="p-4 rounded-xl bg-slate-900/40 border border-white/5">
              <span class="text-[11px] uppercase font-bold text-slate-400 block mb-1">Lead Instructor</span>
              <span class="text-lg font-bold text-emerald-400">{{ course()?.instructorName || 'Department Faculty' }}</span>
            </div>
          </div>

          <div class="flex items-center justify-between pt-6 border-t border-white/10">
            <a routerLink="/student/courses" class="btn-secondary text-xs py-2 px-4">
              &larr; Back to Catalog
            </a>
          </div>
        </div>
      }
    </div>
  `
})
export class CourseDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private courseService = inject(CourseService);

  course = signal<Course | null>(null);
  isLoading = signal(true);

  ngOnInit() {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      const id = parseInt(idParam, 10);
      this.courseService.getCourseById(id).subscribe({
        next: (c) => {
          this.course.set(c);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      });
    }
  }
}
