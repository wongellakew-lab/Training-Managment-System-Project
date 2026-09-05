export interface CourseSummary {
  courseId: number;
  courseCode: string;
  courseTitle: string;
  description: string;
  department: string;
  level: string;
  category: string;
  credits: number;
  startDate: string;
  endDate: string;
  enrollmentStatus: string;
  instructorName: string;
  instructorEmail: string;
}

export interface CourseProgressSummary {
  progressPercentage: number;
  completedAssessments: number;
  totalAssessments: number;
  status: string;
}

export interface AttendanceSession {
  id: number;
  sessionDate: string;
  status: 'Present' | 'Late' | 'Absent' | string;
  remarks?: string | null;
}

export interface AttendanceSummary {
  presentCount: number;
  lateCount: number;
  absentCount: number;
  totalSessions: number;
  attendanceRate: number;
  sessions: AttendanceSession[];
}

export interface InProgressAssignment {
  assignmentId: number;
  assignmentNumber: number;
  title: string;
  dueDate: string;
  maxPoints: number;
  submissionStatus: 'Not Submitted' | 'Submitted';
  submittedAt?: string | null;
}

export interface CompletedAssignment {
  assignmentId: number;
  assignmentNumber: number;
  title: string;
  dueDate: string;
  maxPoints: number;
  scoredPoints: number;
  gradedAt?: string | null;
  feedback?: string | null;
}

export interface AssignmentSections {
  inProgress: InProgressAssignment[];
  completed: CompletedAssignment[];
}

export interface FinalGrade {
  isEvaluated: boolean;
  status: 'Evaluated' | 'Pending';
  score?: number | null;
  letter?: string | null;
  gradePoint?: number | null;
}

export interface CourseScheduleItem {
  id: number;
  dayOfWeek: number; // 0 = Sunday, 1 = Monday, etc.
  startTime: string;
  endTime: string;
  location: string;
  room: string;
  scheduleType: string;
}

export interface StudentCourseProgressDetail {
  courseSummary: CourseSummary;
  progress: CourseProgressSummary;
  attendance: AttendanceSummary;
  assignments: AssignmentSections;
  finalGrade: FinalGrade;
  schedules: CourseScheduleItem[];
}
