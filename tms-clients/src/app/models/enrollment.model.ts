export type EnrollmentStatus = 'Pending' | 'Active' | 'Approved' | 'Completed' | 'Dropped' | 'Waitlisted' | 'Applied' | 'Enrolled';

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
  fee?: number;
  paymentStatus?: 'Paid' | 'Pending' | 'Unpaid' | string;
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
  fee?: number;
  paymentStatus?: string;
  phoneNumber?: string;
  country?: string;
}

export interface UpdateEnrollmentStatusRequest {
  enrollmentId: number;
  status: EnrollmentStatus;
  paymentStatus?: string;
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
