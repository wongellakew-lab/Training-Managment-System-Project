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
            .ForMember(dest => dest.EnrolledCount, opt => opt.MapFrom(src => src.Enrollments.Count > 0 ? src.Enrollments.Count : src.EnrolledStudentsCount))
            .ForMember(dest => dest.InstructorName, opt => opt.MapFrom(src => src.Instructor != null ? $"{src.Instructor.FirstName} {src.Instructor.LastName}" : "Gina Alexander"))
            .ForMember(dest => dest.InstructorAvatar, opt => opt.MapFrom(src => "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80"));
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

        CreateMap<Schedule, ScheduleDto>();
        CreateMap<CreateScheduleDto, Schedule>();
        CreateMap<UpdateScheduleDto, Schedule>();

        CreateMap<Notification, TmsApi.Application.DTOs.NotificationDtos.NotificationDto>();
    }
}
