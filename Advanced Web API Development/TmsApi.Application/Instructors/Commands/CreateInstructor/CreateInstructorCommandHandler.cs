using MediatR;
using TmsApi.Application.DTOs.InstructorDtos;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Instructors.Commands;

public class CreateInstructorCommandHandler(IInstructorService _instructorService) : IRequestHandler<CreateInstructorCommand, InstructorDto>
{
    public async Task<InstructorDto> Handle(CreateInstructorCommand request, CancellationToken cancellationToken)
    {
        return await _instructorService.CreateInstructorAsync(request.Dto, cancellationToken);
    }
}
