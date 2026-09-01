export type EnrollmentStatus = 'Pending' | 'Active' | 'Completed' | 'Dropped';

export interface Enrollment {
  id: number;
  studentId: number;
  studentName?: string;
  courseId: number;
  courseTitle?: string;
  courseCode?: string;
  enrollmentDate: string;
  status: EnrollmentStatus;
  grade?: string;
  numericScore?: number;
}

export interface EnrollRequest {
  studentId: number;
  courseId: number;
}

export interface UpdateEnrollmentStatusRequest {
  enrollmentId: number;
  status: EnrollmentStatus;
}

export interface EnrollmentStats {
  totalEnrollments: number;
  activeEnrollments: number;
  completedEnrollments: number;
  droppedEnrollments: number;
}
