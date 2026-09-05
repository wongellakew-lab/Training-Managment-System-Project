import { Course, CourseSchedule } from './course.model';

export interface InstructorTodayClass {
  scheduleId: number;
  courseId: number;
  courseName: string;
  courseCode: string;
  startTime: string;
  endTime: string;
  room: string;
  location: string;
  studentCount: number;
  attendanceStatus: 'Taken' | 'Pending';
  status?: 'In Progress' | 'Completed' | 'Upcoming';
}

export interface InstructorPendingGrading {
  assignmentId: number;
  courseId: number;
  assignmentTitle: string;
  courseName: string;
  submissionsCount: number;
  dueDate: string;
}

export interface InstructorUpcomingSession {
  scheduleId: number;
  courseId: number;
  courseName: string;
  date: string;
  startTime: string;
  endTime: string;
  room: string;
  location?: string;
  enrolledStudentsCount: number;
}

export interface InstructorStudentQuestion {
  id: number;
  studentId: number;
  studentName: string;
  studentAvatar?: string;
  courseId: number;
  courseName: string;
  question: string;
  answer?: string;
  date: string;
  isAnswered: boolean;
}

export interface InstructorDashboard {
  assignedCoursesCount: number;
  activeStudentsCount: number;
  pendingEnrollmentsCount: number;
  pendingGradesCount: number;
  certificatesAwardedCount: number;
  attendanceRate: number;
  todaysClasses: InstructorTodayClass[];
  pendingGrading: InstructorPendingGrading[];
  upcomingSessions: InstructorUpcomingSession[];
  recentStudentQuestions: InstructorStudentQuestion[];
  assignedCourses: Course[];
}

export interface AttendanceStudentItem {
  studentId: number;
  firstName: string;
  lastName: string;
  email: string;
  registrationNumber?: string;
  avatarUrl?: string;
  status: 'Present' | 'Late' | 'Absent' | 'Unmarked';
  statusCode: number; // 0 = unmarked, 1 = present, 2 = late, 3 = absent
  remarks?: string;
  recordedAt?: string;
}

export interface CourseAttendanceResponse {
  courseId: number;
  courseTitle: string;
  courseCode: string;
  sessionDate: string;
  hasScheduledSession?: boolean;
  isRecorded?: boolean;
  scheduleInfo?: {
    startTime: string;
    endTime: string;
    location?: string;
    room?: string;
    scheduleType?: string;
  };
  totalEnrolled: number;
  presentCount: number;
  lateCount: number;
  absentCount: number;
  students: AttendanceStudentItem[];
}

export interface CourseAttendanceStats {
  courseId: number;
  sessionsCount: number;
  totalRecords: number;
  presentCount: number;
  lateCount: number;
  absentCount: number;
  attendanceRate: number;
}

export interface BuilderLesson {
  id: number;
  title: string;
  description?: string;
  durationMinutes: number;
  contentText?: string;
  pdfUrl?: string;
  pdfFileName?: string;
  orderIndex: number;
}

export interface BuilderModule {
  id: number;
  title: string;
  description?: string;
  orderIndex: number;
  lessons: BuilderLesson[];
}

export interface CourseResourceItem {
  id: number;
  title: string;
  fileType: string;
  fileSize: string;
  url: string;
}

export interface CourseBuilderDetail {
  id: number;
  code: string;
  title: string;
  description: string;
  about: string;
  category: string;
  level: string;
  status: string;
  durationHours: number;
  lessonsCount: number;
  imageUrl: string;
  department: string;
  featuresJson: string;
  instructorId?: number;
  instructorName?: string;
  startDate?: string;
  endDate?: string;
  maxCapacity?: number;
  schedules?: CourseSchedule[];
  rejectionReason?: string;
  expectedAssignmentCount?: number;
  assignmentMaxPoints?: number;
  hasMidterm?: boolean;
  midtermMaxPoints?: number;
  expectedMidtermCount?: number;
  hasFinal?: boolean;
  finalMaxPoints?: number;
  expectedFinalCount?: number;
  totalAssessments?: number;
  totalCoursePoints?: number;
  createdAssignmentsCount?: number;
  modules: BuilderModule[];
  resources: CourseResourceItem[];
}

export interface InstructorCourseSummary {
  id: number;
  code: string;
  title: string;
  category: string;
  level: string;
  status: string;
  durationHours: number;
  lessonsCount: number;
  modulesCount: number;
  resourcesCount: number;
  activeStudentsCount: number;
  imageUrl: string;
  about: string;
  description: string;
  startDate: string;
  endDate: string;
  rejectionReason?: string;
  schedulesCount?: number;
  expectedAssignmentCount?: number;
  assignmentMaxPoints?: number;
  hasMidterm?: boolean;
  midtermMaxPoints?: number;
  expectedMidtermCount?: number;
  hasFinal?: boolean;
  finalMaxPoints?: number;
  expectedFinalCount?: number;
  totalCoursePoints?: number;
}

export interface CourseAssessmentConfig {
  courseId: number;
  courseTitle: string;
  expectedAssignments: number;
  expectedMidterm: number;
  expectedFinal: number;
  totalAssessments: number;
  createdAssignmentNumbers: number[];
  availableAssignmentNumbers: number[];
  createdCount: number;
  isAllAssignmentsCreated: boolean;
}

export interface AssignmentItem {
  id: number;
  assignmentNumber: number;
  courseId: number;
  courseModuleId?: number;
  moduleTitle?: string;
  courseLessonId?: number;
  lessonTitle?: string;
  title: string;
  instructions: string;
  maxPoints: number;
  dueDate: string;
  attachmentUrl?: string;
  attachmentFileName?: string;
  isPublished: boolean;
  createdAt: string;
  totalSubmissions: number;
  pendingGrading: number;
  gradedCount: number;
}

export interface SubmissionItem {
  id: number;
  assignmentId: number;
  assignmentTitle: string;
  maxPoints: number;
  studentId: number;
  studentName: string;
  studentEmail?: string;
  studentRegistrationNumber?: string;
  studentAvatar?: string;
  submittedAt?: string;
  status: 'Pending' | 'Submitted' | 'Graded';
  scoredPoints?: number;
  feedback?: string;
  submissionFileUrl?: string;
  submissionText?: string;
  gradedAt?: string;
}

export interface SubmissionDetail extends SubmissionItem {
  assignmentInstructions?: string;
  assignmentAttachmentUrl?: string;
  courseId: number;
  courseTitle?: string;
  dueDate: string;
  rubricScoresJson?: string;
}

export interface GradebookCell {
  assignmentId: number;
  assessmentId?: number;
  score?: number;
  maxScore: number;
  status: string;
  isGraded: boolean;
  feedback?: string;
}

export interface GradebookRow {
  studentId: number;
  studentName: string;
  registrationNumber?: string;
  avatarUrl?: string;
  grades: GradebookCell[];
  totalScore: number | null;
  totalMaxScore: number | null;
  averagePercentage: number | null;
  letterGrade: string;
  isGradingComplete?: boolean;
  gradedAssessmentCount?: number;
  requiredAssessmentCount?: number;
}

export interface GradebookAssignmentHeader {
  id: number;
  assignmentNumber?: number;
  title: string;
  maxPoints: number;
  dueDate: string;
}

export interface GradebookResponse {
  courseId: number;
  courseTitle: string;
  courseCode: string;
  courseStatus?: string;
  isApproved?: boolean;
  message?: string;
  assignments: GradebookAssignmentHeader[];
  rows: GradebookRow[];
}

export interface InstructorProfile {
  id: number;
  userId: string;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  department?: string;
  hireDate: string;
  bio?: string;
  profilePictureUrl?: string;
  totalCourses: number;
  activeStudents: number;
  totalCertificates: number;
  attendanceRate: number;
}
