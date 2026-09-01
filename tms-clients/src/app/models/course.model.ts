export interface CourseResource {
  title: string;
  type: string; // PDF, PPTX, ZIP
  size: string;
  url?: string;
}

export interface Course {
  id: number;
  title: string;
  code: string;
  description: string;
  department: string;
  category: string; // Design, Web Dev, Marketing, Language, Communication, Business, Wellness
  level: string; // Beginner, Intermediate, Advanced
  price: number;
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
  maxCapacity: number;
  enrolledCount: number;
  instructorId?: number;
  instructorName?: string;
  instructorAvatar?: string;
  isActive: boolean;
  startDate?: string;
  endDate?: string;
}

export interface CreateCourseDto {
  title: string;
  code: string;
  description: string;
  department: string;
  category?: string;
  level?: string;
  price?: number;
  status?: string;
  durationHours?: number;
  lessonsCount?: number;
  imageUrl?: string;
  about?: string;
  credits: number;
  maxCapacity: number;
  instructorId?: number;
}

export interface UpdateCourseDto {
  title: string;
  description: string;
  department: string;
  category?: string;
  level?: string;
  price?: number;
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
  sortBy?: string;
  sortDescending?: boolean;
  fields?: string;
  instructorId?: number;
}
