using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using AutoMapper;

namespace API.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<UserInfoDto, AppUser>();
            //CreateMap<IssueReportDto, IssueReport>();
            /*
            CreateMap<IssueStatus, IssueReportStatusDto>()
                .ForMember(dest => dest.DateReported, opt => opt.MapFrom(src => src.IssueReports.FirstOrDefault().DateReported))
                .ForMember(dest => dest.IssueType, opt => opt.MapFrom(src => string.Join(", ", src.IssueReports.Select(ir => ir.IssueType.Name).Distinct().ToList())))
                .ForMember(dest => dest.District, opt => opt.MapFrom(src => string.Join(", ", src.IssueReports.Select(ir => ir.District.Name).Distinct().ToList())))
                .ForMember(dest => dest.IssueReportCount, opt => opt.MapFrom(src => src.IssueReports.Count()));

                .ForMember(dest => dest.IssueType, opt => opt.MapFrom(src => src.IssueReports.FirstOrDefault().IssueType.Name))
            */

            List<int> pinned = null;
            List<int> hidden = null;
            CreateMap<IssueStatus, IssueReportStatusDto>()
                .ForMember(dest => dest.DateReported, opt => opt.MapFrom(src => src.IssueReports.FirstOrDefault().DateReported))
                .ForMember(dest => dest.DateLastReported, opt => opt.MapFrom(src => src.IssueReports.OrderBy(i => i.DateReported).FirstOrDefault().DateReported))
                .ForMember(dest => dest.IssueType, opt => opt.MapFrom(src => src.IssueReports.FirstOrDefault().IssueType.Name))
                .ForMember(dest => dest.Districts, opt => opt.MapFrom(src => src.District != null ? (new HashSet<string>{src.District.Name}) : src.IssueReports.Where(i => i.District != null).Select(i => i.District.Name).ToHashSet()))
                .ForMember(dest => dest.IssueTypes, opt => opt.MapFrom(src => src.IssueReports.Select(i => i.IssueType.Name).ToHashSet()))
                .ForMember(dest => dest.District, opt => opt.MapFrom(src => src.District != null ? src.District.Name : src.IssueReports.FirstOrDefault().District.Name))
                .ForMember(dest => dest.IssueReportCount, opt => opt.MapFrom(src => src.IssueReports.Count()))
                .ForMember(dest => dest.StatusUpdateCount, opt => opt.MapFrom(src => src.StatusUpdates.Count()))
                .ForMember(dest => dest.LocationDescription, opt => opt.MapFrom(src => src.LocationDescription != null ? src.LocationDescription : src.IssueReports.FirstOrDefault(x => x.LocationDescription != null && x.LocationDescription != "").LocationDescription))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description != null ? src.Description : src.IssueReports.FirstOrDefault(x => x.Description != null && x.Description != "").Description))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.CurrentStatus.Name))
                .ForMember(dest => dest.ClosedStatus, opt => opt.MapFrom(src => src.CurrentStatus.Final))
                .ForMember(dest => dest.PreviousStatus, opt => opt.MapFrom(src => src.StatusUpdates.OrderByDescending(ir => ir.DateReported).FirstOrDefault().PreviousStatus.Name))
                .ForMember(dest => dest.Pinned, opt => opt.MapFrom(src => pinned == null ? false : pinned.Contains(src.Id)))
                .ForMember(dest => dest.Hidden, opt => opt.MapFrom(src => hidden == null ? false : hidden.Contains(src.Id)))
                .ForMember(dest => dest.DateUpdated, opt => opt.MapFrom(src => src.StatusUpdates.Any() ? src.StatusUpdates.FirstOrDefault().DateReported : src.IssueReports.FirstOrDefault().DateReported));

        
            CreateMap<IssueReport, IssueReportItemDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.AppUser.UserName))
                .ForMember(dest => dest.Uploader, opt => opt.MapFrom(src => src.AppUser.FirstName + (src.AppUser.LastName!=null?" " + src.AppUser.LastName:"")))
                .ForMember(dest => dest.IssueType, opt => opt.MapFrom(src => src.IssueType.Name))
                .ForMember(dest => dest.District, opt => opt.MapFrom(src => src.District.Name))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.ReporterAddress))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.ReporterPhoneNumber))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.ReporterEmail))
                .ForMember(dest => dest.OrignalIssueSourceId, opt => opt.MapFrom(src => src.OrignalIssueSourceId))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images.Select(img => new IssueReportItemDto.Image{Id = img.Id, Path = img.Path})));

            CreateMap<IssueReport, IssueStatusReportDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.AppUser.UserName))
                .ForMember(dest => dest.IssueType, opt => opt.MapFrom(src => src.IssueType.Name))
                .ForMember(dest => dest.District, opt => opt.MapFrom(src => src.District.Name))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.ReporterAddress))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.ReporterPhoneNumber))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.ReporterEmail))
                .ForMember(dest => dest.MobileUserId, opt => opt.MapFrom(src => src.MobileIssueId))
                .ForMember(dest => dest.CurrentStatus, opt => opt.MapFrom(src => src.IssueStatus.CurrentStatus.Name))
                .ForMember(dest => dest.ClosedStatus, opt => opt.MapFrom(src => src.IssueStatus.CurrentStatus.Final))
                .ForMember(dest => dest.ImageCount, opt => opt.MapFrom(src => src.Images.Count()));

            CreateMap<AppUser, UserItemDto>()
                .ForMember(dest => dest.AccountType, opt => opt.MapFrom(src => src.UserRoles.FirstOrDefault().Role.Name))
                .ForMember(dest => dest.UserDistrictsAccess, opt => opt.MapFrom(src => src.UserDistricts.Select(i => i.District.Name).ToArray()))
                .ForMember(dest => dest.UserIssueTypesAccess, opt => opt.MapFrom(src => src.UserIssueTypes.Select(i => i.IssueType.Name).ToArray()));

            int? currentStatusUpdateId = null;
            CreateMap<StatusUpdate, IssueStatusUpdateDto>()
                .ForMember(dest => dest.Uploader, opt => opt.MapFrom(s => s.AppUser.FirstName != null ? s.AppUser.FirstName + (s.AppUser.LastName != null ? " " + s.AppUser.LastName : "") : s.AppUser.UserName))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(s => s.AppUser.UserName))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(s => s.Status.Name))
                .ForMember(dest => dest.PreviousStatus, opt => opt.MapFrom(s => s.PreviousStatus.Name))
                .ForMember(dest => dest.IsCurrentStatus, opt => opt.MapFrom(s => currentStatusUpdateId != null ? s.Id == currentStatusUpdateId : false))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(s => s.Images.Select(d => new IssueStatusUpdateDto.Image{Id = d.Id, Path = d.Path})))
                .ForMember(dest => dest.ApprovalItems, opt => opt.MapFrom(s => s.ApprovalItems.Select(r => r.Description)));



            //     Id = s.Id,
            //     Name = s.AppUser.FirstName != null ? s.AppUser.FirstName + (s.AppUser.LastName != null ? " " + s.AppUser.LastName : "") : s.AppUser.UserName,
            //     Status = s.Status.Name,
            //     PreviousStatus = s.PreviousStatus.Name,
            //     ResponsibleUnit = s.ResponsibleUnit,
            //     NewUnit = s.NewUnit,
            //     Date = s.Date,
            //     DateReported = s.DateReported,
            //     // Images = s,
            //     // ApprovalItems = s.ApprovalItems,
            //     ReasonDetails = s.ReasonDetails,
            //     StatusUpdateDetails = s.StatusUpdateDetails,
            //     WorkType = s.WorkType
            //
        }
    }
}