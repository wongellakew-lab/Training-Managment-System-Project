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
  notes: string;
  attendeesCount: number;
  createdAt?: string;
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
