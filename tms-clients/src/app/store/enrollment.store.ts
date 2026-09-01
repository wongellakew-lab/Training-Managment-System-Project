import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';
import { inject, computed } from '@angular/core';
import { Enrollment, EnrollRequest, EnrollmentStatus } from '../models/enrollment.model';
import { EnrollmentService } from '../services/enrollment.service';
import { firstValueFrom } from 'rxjs';

interface EnrollmentState {
  enrollments: Enrollment[];
  selectedEnrollment: Enrollment | null;
  isLoading: boolean;
  error: string | null;
  statusFilter: EnrollmentStatus | 'All';
}

const initialState: EnrollmentState = {
  enrollments: [],
  selectedEnrollment: null,
  isLoading: false,
  error: null,
  statusFilter: 'All'
};

export const EnrollmentStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed((store) => ({
    filteredEnrollments: computed(() => {
      const filter = store.statusFilter();
      if (filter === 'All') return store.enrollments();
      return store.enrollments().filter((e) => e.status === filter);
    }),
    activeEnrollmentsCount: computed(() => store.enrollments().filter((e) => e.status === 'Active').length),
    completedEnrollmentsCount: computed(() => store.enrollments().filter((e) => e.status === 'Completed').length),
    totalCreditsEnrolled: computed(() => store.enrollments().length * 3)
  })),
  withMethods((store, enrollmentService = inject(EnrollmentService)) => ({
    async loadStudentEnrollments(studentId: number) {
      patchState(store, { isLoading: true, error: null });
      try {
        const result = await firstValueFrom(enrollmentService.getStudentEnrollments(studentId));
        patchState(store, { enrollments: result, isLoading: false });
      } catch (err: any) {
        patchState(store, { error: err.message || 'Failed to load enrollments', isLoading: false });
      }
    },

    async enroll(request: EnrollRequest) {
      patchState(store, { isLoading: true, error: null });
      try {
        const newEnrollment = await firstValueFrom(enrollmentService.enrollStudent(request));
        patchState(store, {
          enrollments: [...store.enrollments(), newEnrollment],
          isLoading: false
        });
        return newEnrollment;
      } catch (err: any) {
        patchState(store, { error: err.message || 'Enrollment failed', isLoading: false });
        throw err;
      }
    },

    async drop(enrollmentId: number) {
      patchState(store, { isLoading: true, error: null });
      try {
        await firstValueFrom(enrollmentService.dropEnrollment(enrollmentId));
        patchState(store, {
          enrollments: store.enrollments().map((e) =>
            e.id === enrollmentId ? { ...e, status: 'Dropped' as EnrollmentStatus } : e
          ),
          isLoading: false
        });
      } catch (err: any) {
        patchState(store, { error: err.message || 'Failed to drop enrollment', isLoading: false });
        throw err;
      }
    },

    setStatusFilter(filter: EnrollmentStatus | 'All') {
      patchState(store, { statusFilter: filter });
    }
  }))
);
