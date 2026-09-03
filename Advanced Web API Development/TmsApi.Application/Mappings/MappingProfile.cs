using AutoMapper;
using TmsApi.Application.DTOs.CourseDtos;
using TmsApi.Application.DTOs.EnrollmentDtos;
using TmsApi.Application.DTOs.InstructorDtos;
using TmsApi.Application.DTOs.ScheduleDtos;
using TmsApi.Application.DTOs.StudentDtos;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Student, StudentDto>();
        CreateMap<CreateStudentDto, Student>();
        CreateMap<UpdateStudentDto, Student>();

        CreateMap<Instructor, InstructorDto>();
        CreateMap<CreateInstructorDto, Instructor>();

        CreateMap<Course, CourseDto>()
            .ForMember(dest => dest.EnrolledCount, opt => opt.MapFrom(src => src.Enrollments != null ? src.Enrollments.Count(e => (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Pending) && !e.IsDeleted) : 0))
            .ForMember(dest => dest.InstructorName, opt => opt.MapFrom(src => src.Instructor != null ? $"{src.Instructor.FirstName} {src.Instructor.LastName}" : null))
            .ForMember(dest => dest.InstructorDepartment, opt => opt.MapFrom(src => src.Instructor != null ? src.Instructor.Department : null))
            .ForMember(dest => dest.InstructorEmail, opt => opt.MapFrom(src => src.Instructor != null ? src.Instructor.Email : null))
            .ForMember(dest => dest.InstructorAvatar, opt => opt.MapFrom(src => "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80"))
            .ForMember(dest => dest.Schedules, opt => opt.MapFrom(src => src.Schedules))
            .ForMember(dest => dest.Modules, opt => opt.MapFrom(src => src.Modules))
            .ForMember(dest => dest.Resources, opt => opt.MapFrom(src => src.Resources));
        CreateMap<CourseSchedule, CourseScheduleDto>();
        CreateMap<CreateCourseScheduleDto, CourseSchedule>();
        CreateMap<CourseModule, CourseModuleDto>();
        CreateMap<CourseLesson, CourseLessonDto>();
        CreateMap<CourseResource, CourseResourceDto>();
        CreateMap<CreateCourseDto, Course>();
        CreateMap<UpdateCourseDto, Course>();

        CreateMap<Enrollment, EnrollmentDto>()
            .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student != null ? $"{src.Student.FirstName} {src.Student.LastName}" : string.Empty))
            .ForMember(dest => dest.StudentEmail, opt => opt.MapFrom(src => src.Student != null ? src.Student.Email : string.Empty))
            .ForMember(dest => dest.StudentAvatar, opt => opt.MapFrom(src => "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150&auto=format&fit=crop&q=80"))
            .ForMember(dest => dest.CourseCode, opt => opt.MapFrom(src => src.Course != null ? src.Course.Code : string.Empty))
            .ForMember(dest => dest.CourseTitle, opt => opt.MapFrom(src => src.Course != null ? src.Course.Title : string.Empty))
            .ForMember(dest => dest.CourseCategory, opt => opt.MapFrom(src => src.Course != null ? src.Course.Category : "Design"));
        CreateMap<CreateEnrollmentDto, Enrollment>();

        CreateMap<Schedule, ScheduleDto>()
            .ForMember(dest => dest.CourseTitle, opt => opt.MapFrom(src => src.Course != null ? src.Course.Title : string.Empty))
            .ForMember(dest => dest.CourseCode, opt => opt.MapFrom(src => src.Course != null ? src.Course.Code : string.Empty))
            .ForMember(dest => dest.InstructorName, opt => opt.MapFrom(src => src.Course != null && src.Course.Instructor != null ? $"{src.Course.Instructor.FirstName} {src.Course.Instructor.LastName}" : (string.IsNullOrEmpty(src.PicName) ? "Faculty Instructor" : src.PicName)))
            .ForMember(dest => dest.InstructorAvatar, opt => opt.MapFrom(src => src.Course != null && src.Course.Instructor != null && src.Course.Instructor.User != null && !string.IsNullOrEmpty(src.Course.Instructor.User.ProfilePictureUrl) ? src.Course.Instructor.User.ProfilePictureUrl : src.PicAvatar));
        CreateMap<CreateScheduleDto, Schedule>();
        CreateMap<UpdateScheduleDto, Schedule>();

        CreateMap<Notification, TmsApi.Application.DTOs.NotificationDtos.NotificationDto>();
    }
}
