using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using SchedulerWpfApp.Model;

namespace SchedulerWpfApp.Helper
{
    public class AutoMappingProfiles : Profile
    {
        /// <summary>
        /// Initializes a new instance of the AutoMappingProfiles class.
        /// This class is used to define mapping profiles for AutoMapper.
        /// </summary>
        public AutoMappingProfiles()
        {
            CreateMap<Room, Room>()
           .ForMember(dest => dest.RoomId, opt => opt.Ignore()); // No RoomId map
            CreateMap<GroupClass, GroupClass>();
            // Initialize AutoMapper profiles here if needed
            // For example, you can create mappings between different models
            // Mapper.Initialize(cfg => cfg.CreateMap<SourceModel, DestinationModel>());
        }
    }
}
