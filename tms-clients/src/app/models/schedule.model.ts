export interface Schedule {
  id: number;
  title: string;
  category: 'Meetings' | 'Workshops' | 'Events' | string;
  date: string;
  startTime: string;
  endTime: string;
  picName: string;
  picAvatar?: string;
  location: string;
  room?: string;
  notes: string;
  attendeesCount: number;
  courseId?: number;
  courseTitle?: string;
  courseCode?: string;
  instructorName?: string;
  instructorAvatar?: string;
  scheduleType?: string;
  courseStatus?: string;
  enrollmentStatus?: string;
  createdAt?: string;
}

export interface CourseLookup {
  id: number;
  title: string;
  code: string;
}

export interface StudentScheduleSummary {
  classesThisMonth: number;
  classesThisWeek: number;
  enrolledCoursesCount: number;
  nextClass: Schedule | null;
  enrolledCourses: CourseLookup[];
  schedules: Schedule[];
}

export interface CreateScheduleDto {
  title: string;
  category: string;
  date: string;
  startTime: string;
  endTime: string;
  picName: string;
  picAvatar?: string;
  location: string;
  notes: string;
  attendeesCount?: number;
}

export interface UpdateScheduleDto {
  title: string;
  category: string;
  date: string;
  startTime: string;
  endTime: string;
  picName: string;
  picAvatar?: string;
  location: string;
  notes: string;
  attendeesCount?: number;
}
