import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { inject, computed } from '@angular/core';
import { Course, CourseFilterParams, PagedResult } from '../models/course.model';
import { CourseService } from '../services/course.service';
import { firstValueFrom } from 'rxjs';

interface CourseState {
  courses: Course[];
  selectedCourse: Course | null;
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  search: string;
  department: string;
  isLoading: boolean;
  error: string | null;
}

const initialState: CourseState = {
  courses: [],
  selectedCourse: null,
  totalCount: 0,
  pageNumber: 1,
  pageSize: 10,
  totalPages: 1,
  search: '',
  department: '',
  isLoading: false,
  error: null
};

export const CourseStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed((store) => ({
    activeCoursesCount: computed(() => store.courses().filter((c) => c.isActive).length),
    availableCourses: computed(() => store.courses().filter((c) => c.enrolledCount < c.maxCapacity)),
    hasCourses: computed(() => store.courses().length > 0)
  })),
  withMethods((store, courseService = inject(CourseService)) => ({
    async loadCourses(filter: CourseFilterParams = {}) {
      patchState(store, { isLoading: true, error: null });
      try {
        const result: PagedResult<Course> = await firstValueFrom(courseService.getCourses(filter));
        patchState(store, {
          courses: result.items,
          totalCount: result.totalCount,
          pageNumber: result.pageNumber,
          pageSize: result.pageSize,
          totalPages: result.totalPages,
          isLoading: false
        });
      } catch (err: any) {
        patchState(store, { error: err.message || 'Failed to load courses', isLoading: false });
      }
    },

    async loadCachedCourses(filter: CourseFilterParams = {}) {
      patchState(store, { isLoading: true, error: null });
      try {
        const result: PagedResult<Course> = await firstValueFrom(courseService.getCachedCourses(filter));
        patchState(store, {
          courses: result.items,
          totalCount: result.totalCount,
          pageNumber: result.pageNumber,
          pageSize: result.pageSize,
          totalPages: result.totalPages,
          isLoading: false
        });
      } catch (err: any) {
        patchState(store, { error: err.message || 'Failed to load cached courses', isLoading: false });
      }
    },

    setSelectedCourse(course: Course | null) {
      patchState(store, { selectedCourse: course });
    },

    updateCapacity(courseId: number, enrolledCount: number, maxCapacity: number) {
      patchState(store, {
        courses: store.courses().map((c) =>
          c.id === courseId ? { ...c, enrolledCount, maxCapacity } : c
        )
      });
    },

    setFilter(search: string, department: string) {
      patchState(store, { search, department, pageNumber: 1 });
    }
  }))
);
