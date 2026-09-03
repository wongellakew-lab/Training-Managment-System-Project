export type EnrollmentStatus = 'Pending' | 'Active' | 'Approved' | 'Rejected' | 'Completed' | 'Dropped' | 'Waitlisted' | 'Applied' | 'Enrolled';

export interface Enrollment {
  id: number;
  studentId: number;
  studentName?: string;
  studentEmail?: string;
  studentAvatar?: string;
  phoneNumber?: string;
  courseId: number;
  courseTitle?: string;
  courseCode?: string;
  courseCategory?: string;
  batch?: string;
  country?: string;
  enrollmentDate?: string;
  enrolledAt?: string;
  status: EnrollmentStatus;
  grade?: string;
  numericScore?: number;
}

export interface EnrollRequest {
  studentId: number;
  courseId: number;
  batch?: string;
  phoneNumber?: string;
  country?: string;
  status?: EnrollmentStatus;
}

export interface UpdateEnrollmentStatusRequest {
  enrollmentId: number;
  status: EnrollmentStatus;
}

export interface EnrollmentFilterParams {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: string;
  category?: string;
  studentId?: number;
  courseId?: number;
}
