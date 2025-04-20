using AutoMapper;
using ROTA_API.DTOs;
using ROTA_API.Models;

namespace ROTA_API
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Source -> Target
            CreateMap<Employee, EmployeeDto>()
                .ForMember(dest => dest.TeamName, opt => opt.MapFrom(src => src.Team != null ? src.Team.TeamName : null)); // Map TeamName if Team is loaded

            CreateMap<CreateEmployeeDto, Employee>(); // Map Create DTO to Entity
            CreateMap<UpdateEmployeeDto, Employee>(); // Map Update DTO to Entity


            CreateMap<Shift, ShiftDto>()
           .ForMember(dest => dest.EmployeeFirstName, opt => opt.MapFrom(src => src.Employee.FirstName))
           .ForMember(dest => dest.EmployeeLastName, opt => opt.MapFrom(src => src.Employee.LastName))
           .ForMember(dest => dest.TeamId, opt => opt.MapFrom(src => src.Employee.TeamId)) // Get TeamId from Employee
           .ForMember(dest => dest.TeamName, opt => opt.MapFrom(src => src.Employee.Team != null ? src.Employee.Team.TeamName : null)) // Get TeamName from Employee
           .ForMember(dest => dest.ShiftTypeName, opt => opt.MapFrom(src => src.ShiftType != null ? src.ShiftType.TypeName : null))
           .ForMember(dest => dest.IsOnCall, opt => opt.MapFrom(src => src.ShiftType != null ? src.ShiftType.IsOnCall : false)); // Get IsOnCall from ShiftType

            CreateMap<CreateShiftDto, Shift>(); // Simple mapping for creation
            CreateMap<UpdateShiftDto, Shift>(); // Simple mapping for updates

            CreateMap<LeaveRequest, LeaveRequestDto>()
            .ForMember(dest => dest.EmployeeFirstName, opt => opt.MapFrom(src => src.Employee.FirstName))
            .ForMember(dest => dest.EmployeeLastName, opt => opt.MapFrom(src => src.Employee.LastName))
            .ForMember(dest => dest.TeamId, opt => opt.MapFrom(src => src.Employee.TeamId))
            .ForMember(dest => dest.TeamName, opt => opt.MapFrom(src => src.Employee.Team != null ? src.Employee.Team.TeamName : null))
            .ForMember(dest => dest.LeaveTypeName, opt => opt.MapFrom(src => src.LeaveType.LeaveTypeName))
            .ForMember(dest => dest.ApproverUsername, opt => opt.MapFrom(src => src.ApproverUser != null ? src.ApproverUser.Username : null)); // Map approver username

            CreateMap<CreateLeaveRequestDto, LeaveRequest>(); // Simple map for creation


            // --- Team Mappings ---
            CreateMap<Team, TeamDto>()
                .ForMember(dest => dest.EmployeeCount, opt => opt.MapFrom(src => src.Employees.Count));

            CreateMap<CreateTeamDto, Team>(); // Map create DTO to entity
            CreateMap<UpdateTeamDto, Team>(); // Map update DTO to entity

            CreateMap<LeaveType, LeaveTypeDto>();
            CreateMap<CreateLeaveTypeDto, LeaveType>();
            CreateMap<UpdateLeaveTypeDto, LeaveType>();

            CreateMap<User, UserDetailDto>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.RoleName)) // Assumes Role is loaded
            .ForMember(dest => dest.EmployeeFullName, opt => opt.MapFrom(src => $"{src.Employee.FirstName} {src.Employee.LastName}")); // Assumes Employee is loaded

            CreateMap<ShiftType, ShiftTypeDto>();
            CreateMap<CreateShiftTypeDto, ShiftType>();
            CreateMap<UpdateShiftTypeDto, ShiftType>();

            CreateMap<Role, RoleDto>();
            // MappingProfile.cs - Optional: Add if using ProjectTo for OnCallAssignmentDto
            // CreateMap<Shift, OnCallAssignmentDto>()
            //     .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => $"{src.Employee.FirstName} {src.Employee.LastName}"))
            //     .ForMember(dest => dest.TeamName, opt => opt.MapFrom(src => src.Employee.Team != null ? src.Employee.Team.TeamName : null))
            //     .ForMember(dest => dest.ShiftTypeName, opt => opt.MapFrom(src => src.ShiftType != null ? src.ShiftType.TypeName : "Unknown"));
        }
    }
}
