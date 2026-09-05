export interface CourseResource {
  id?: number;
  title: string;
  fileType: string; // PDF, PPTX, XLSX, DOCX
  fileSize: string;
  url?: string;
  type?: string;
  size?: string;
}

export interface CourseLesson {
  id: number;
  title: string;
  description?: string;
  orderIndex: number;
  durationMinutes?: number;
  contentText?: string;
  pdfUrl?: string;
  pdfFileName?: string;
}

export interface CourseModule {
  id: number;
  title: string;
  description?: string;
  orderIndex: number;
  lessons: CourseLesson[];
  expanded?: boolean;
}

export interface Course {
  id: number;
  title: string;
  code: string;
  description: string;
  department: string;
  category: string; // Design, Web Dev, Marketing, Language, Communication, Business, Wellness
  level: string; // Beginner, Intermediate, Advanced
  status: string; // Active, Draft, Archived
  durationHours: number;
  lessonsCount: number;
  rating: number;
  reviewsCount: number;
  imageUrl: string;
  about: string;
  featuresJson: string;
  resourcesJson: string;
  enrolledStudentsCount: number;
  credits: number;
  enrolledCount: number;
  availableSeats?: number;
  maxCapacity: number;
  prerequisites?: string;
  enrollmentDeadline?: string;
  isStudentEnrolled?: boolean;
  studentEnrollmentStatus?: string;
  studentProgress?: number;
  instructorId?: number;
  instructorName?: string;
  instructorAvatar?: string;
  instructorDepartment?: string;
  instructorEmail?: string;
  isActive: boolean;
  startDate?: string;
  endDate?: string;
  durationWeeks?: number;
  schedules?: CourseSchedule[];
  modules?: CourseModule[];
  resources?: CourseResource[];
  rejectionReason?: string;
  expectedAssignmentCount?: number;
  expectedMidtermCount?: number;
  expectedFinalCount?: number;
  totalAssessments?: number;
  createdAssignmentsCount?: number;
}

export interface CourseSchedule {
  id?: number;
  courseId?: number;
  dayOfWeek: number;
  dayName?: string;
  startTime: string;
  endTime: string;
  location?: string;
  room?: string;
  scheduleType?: string;
}

export interface CreateCourseScheduleDto {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  location?: string;
  room?: string;
  scheduleType?: string;
}

export interface CreateCourseDto {
  title: string;
  code: string;
  description: string;
  department: string;
  category?: string;
  level?: string;
  status?: string;
  durationHours?: number;
  lessonsCount?: number;
  imageUrl?: string;
  about?: string;
  credits: number;
  maxCapacity: number;
  instructorId?: number;
  startDate?: string;
  endDate?: string;
  expectedAssignmentCount?: number;
  expectedMidtermCount?: number;
  expectedFinalCount?: number;
  schedules?: CreateCourseScheduleDto[];
}

export interface UpdateCourseDto {
  title: string;
  description: string;
  department: string;
  category?: string;
  level?: string;
  status?: string;
  durationHours?: number;
  lessonsCount?: number;
  rating?: number;
  reviewsCount?: number;
  imageUrl?: string;
  about?: string;
  credits: number;
  maxCapacity: number;
  instructorId?: number;
  startDate?: string;
  endDate?: string;
  rejectionReason?: string;
  expectedAssignmentCount?: number;
  expectedMidtermCount?: number;
  expectedFinalCount?: number;
  schedules?: CreateCourseScheduleDto[];
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  page?: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface CourseFilterParams {
  pageNumber?: number;
  page?: number;
  pageSize?: number;
  search?: string;
  searchTerm?: string;
  department?: string;
  category?: string;
  status?: string;
  skillLevel?: string;
  subjectMatter?: string;
  availability?: string;
  sortBy?: string;
  sortDescending?: boolean;
  fields?: string;
  instructorId?: number;
}
