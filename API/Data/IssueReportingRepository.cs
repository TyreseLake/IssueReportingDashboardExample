using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers.DBSCANHelpers;
using API.Helpers.Pagination;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using FuzzySharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace API.Data
{
    /// <summary>
    /// Repository for handling all issue reporting, issue status and issue report related requests
    /// </summary>
    public class IssueReportingRepository : IIssueReportingRepository
    {
        private readonly IConfiguration _config;
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        public IssueReportingRepository(DataContext context,
                                        IConfiguration config,
                                        IMapper mapper)
        {
            _mapper = mapper;
            _config = config;
            _context = context;
        }

        /// <summary>
        /// This adds an issue status to the issue status database
        /// </summary>
        /// <param name="issueStatus">The issue status</param>
        public void AddIssueStatus(IssueStatus issueStatus)
        {
            _context.IssueStatus.Add(issueStatus);
        }

        /// <summary>
        /// Get an issue status by its ID
        /// </summary>
        /// <param name="issueStatusId">THe issue status' ID</param>
        /// <returns>The issue status</returns>
        public async Task<IssueStatus> GetIssueStatus(int issueStatusId)
        {
            return await _context.IssueStatus
                .Include(i => i.IssueReports).ThenInclude(ir => ir.IssueType)
                .Include(i => i.IssueReports).ThenInclude(ir => ir.District)
                .Include(i => i.IssueReports).ThenInclude(ir => ir.Images)
                // .Include(i => i.Remarks).ThenInclude(r => r.AppUser)
                .Include(i => i.District)
                .Include(i => i.CurrentStatus)
                .Include(i => i.StatusUpdates)
                .SingleOrDefaultAsync(i => i.Id == issueStatusId);
        }

        /// <summary>
        /// Get an issue report by ID
        /// </summary>
        /// <param name="issueReportId">The issue report's ID</param>
        /// <returns>The issue report</returns>
        public async Task<IssueReport> GetIssueReport(int issueReportId)
        {
            return await _context.IssueReports
                .Include(ir => ir.IssueStatus).ThenInclude(i => i.IssueReports)
                .SingleOrDefaultAsync(ir => ir.Id == issueReportId);
        }

        /// <summary>
        /// Add an issue report top the database
        /// </summary>
        /// <param name="issueReport">The issue report to be added</param>
        public void AddIssueReport(IssueReport issueReport)
        {
            _context.IssueReports.Add(issueReport);
        }

        /// <summary>
        /// This gets all the platforms from the database
        /// </summary>
        /// <returns>Array of platforms</returns>
        public string[] getPlatforms()
        {
            return _config.GetSection("Platforms").Get<string[]>();
        }

        /// <summary>
        /// Save all changes for the issue reporting table
        /// </summary>
        /// <returns>Save result</returns>
        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// This get all the issue
        /// </summary>
        /// <param name="issueReportStatusParams">Specifies the query paramters</param>
        /// <returns>Paginated list of issue statuses</returns>
        public async Task<PagedList<IssueReportStatusDto>> AsyncGetPaginatedIssueReportStatuses(IssueReportStatusParams issueReportStatusParams, AppUser currentUser = null)
        {
            var query = _context.IssueStatus
                .Include(i => i.IssueReports.OrderByDescending(ir => ir.DateReported)).ThenInclude(ir => ir.District)
                .Include(i => i.IssueReports.OrderByDescending(ir => ir.DateReported)).ThenInclude(ir => ir.IssueType)
                .Include(i => i.CurrentStatus)
                .AsQueryable();
            
            //.OrderByDescending(i => i.IssueReports.FirstOrDefault().DateReported)

            // string searchKey = null;
            // int? keyInt = null;
            
            // if(issueReportStatusParams.Key != null)
            // {
            //     searchKey = issueReportStatusParams.Key.Trim().ToUpper();

            //     //Check if the search key could be converted to an integer to be compared to the Id

            //     try
            //     {
            //         keyInt = Int32.Parse(searchKey);
            //     }
            //     catch (FormatException){ }

            //     if(keyInt != null){
            //         query  = query.Where(i => i.LocationDescription.ToUpper().Contains(searchKey) || 
            //             i.Description.ToUpper().Contains(searchKey) || 
            //             i.Id == keyInt || 
            //             i.IssueReports.Any(ir => ir.IssueType.Name.ToUpper().Contains(searchKey)));
            //     }else{
            //         query  = query.Where(i => i.LocationDescription.ToUpper().Contains(searchKey) || 
            //             i.Description.ToUpper().Contains(searchKey) || 
            //             i.IssueReports.Any(ir => ir.IssueType.Name.ToUpper().Contains(searchKey)));
            //     }

            //     //Test
            //     // var valuesAtThisPoint2 = await query.ToListAsync();
            //     // query  = query.Where(i => i.LocationDescription.ToUpper().Contains(searchKey));
            //     // var valuesAtThisPoint3 = await query.ToListAsync();
            // }

            if(!issueReportStatusParams.ShowClosed)
            {
                query = query.Where(i => !i.CurrentStatus.Final);
            }

            if(issueReportStatusParams.StatusList != null)
            {
                query = query.Where(i => issueReportStatusParams.StatusList.Contains(i.CurrentStatus.Name));
            }

            if(issueReportStatusParams.maxReportCount != null)
            {
                query = query.Where(i => i.IssueReports.Count() <= issueReportStatusParams.maxReportCount);
            }

            if(issueReportStatusParams.minReportCount != null)
            {
                query = query.Where(i => i.IssueReports.Count() >= issueReportStatusParams.minReportCount);
            }

            if(issueReportStatusParams.IssueTypeAccess.Length > 0){
                query = query.Where(i => i.IssueReports.Any(x => issueReportStatusParams.IssueTypeAccess.Contains(x.IssueType.Name)));
            }

            if(issueReportStatusParams.DistrictAccess.Length > 0){
                query = query.Where(i => i.District != null ? issueReportStatusParams.DistrictAccess.Contains(i.District.Name) : i.IssueReports.Any(x => issueReportStatusParams.DistrictAccess.Contains(x.District.Name)));
            }

            var pinned = currentUser == null ? null : currentUser.UserPins.Select(i => i.IssueStatusId).ToList();
            var hidden = currentUser == null ? null : currentUser.UserHiddenItems.Select(i => i.IssueStatusId).ToList();
            
            if(!issueReportStatusParams.ShowHidden) {
                query = query.Where(i => !hidden.Contains(i.Id));
            }

            var issueStatuses = query.ProjectTo<IssueReportStatusDto>(_mapper.ConfigurationProvider, new { pinned = pinned, hidden = issueReportStatusParams.ShowHidden ? hidden : null});
            // var issueStatuses = query.ProjectTo<IssueReportStatusDto>(_mapper.ConfigurationProvider);

            // if(currentUser != null)
            // {
            //     foreach(var userPin in currentUser.UserPins.Select(i => i.IssueStatusId))
            //     {
            //         issueStatuses.Where(i => i.Id == userPin).First().Pinned = true;
            //     }
            // }

            string searchKey = null;
            int? keyInt = null;
            
            if(issueReportStatusParams.Key != null)
            {
                searchKey = issueReportStatusParams.Key.Trim().ToUpper();

                //Check if the search key could be converted to an integer to be compared to the Id

                try
                {
                    keyInt = Int32.Parse(searchKey);
                }
                catch (FormatException){ }

                if(keyInt != null){
                    issueStatuses  = issueStatuses.Where(i => i.Id == keyInt ||
                        i.LocationDescription.ToUpper().Contains(searchKey) || 
                        i.Description.ToUpper().Contains(searchKey) ||
                        i.IssueType.ToUpper().Contains(searchKey) ||
                        i.District.ToUpper().Contains(searchKey) );
                }else{
                    issueStatuses  = issueStatuses.Where(i => i.LocationDescription.ToUpper().Contains(searchKey) || 
                        i.Description.ToUpper().Contains(searchKey) ||
                        i.IssueType.ToUpper().Contains(searchKey) ||
                        i.District.ToUpper().Contains(searchKey));
                }

                // if(keyInt != null){
                //     issueStatuses  = issueStatuses.Where(i => i.LocationDescription.ToUpper().Contains(searchKey) || 
                //         i.Description.ToUpper().Contains(searchKey) || 
                //         i.Id == keyInt || 
                //         i.IssueTypes.Select(i => i.ToUpper().Contains(searchKey)).Any() ||
                //         i.Districts.Select(i => i.ToUpper().Contains(searchKey)).Any() );
                // }else{
                //     issueStatuses  = issueStatuses.Where(i => i.LocationDescription.ToUpper().Contains(searchKey) || 
                //         i.Description.ToUpper().Contains(searchKey) ||
                //         i.IssueTypes.Select(i => i.ToUpper().Contains(searchKey)).Any() ||
                //         i.Districts.Select(i => i.ToUpper().Contains(searchKey)).Any() );
                // }

                //Test
                // var valuesAtThisPoint2 = await query.ToListAsync();
                // query  = query.Where(i => i.LocationDescription.ToUpper().Contains(searchKey));
                // var valuesAtThisPoint3 = await query.ToListAsync();
            }


            if(issueReportStatusParams.Order == "Ascending")
            {
                if(issueReportStatusParams.PinnedOnTop)
                {
                    issueStatuses = issueReportStatusParams.SortBy switch {
                        "Type" => issueStatuses.OrderByDescending(i => i.Pinned).ThenBy(i => i.IssueType),
                        "Reports" => issueStatuses.OrderByDescending(i => i.Pinned).ThenBy(i => i.IssueReportCount),
                        "Status" => issueStatuses.OrderByDescending(i => i.Pinned).ThenBy(i => i.Status),
                        "DateUpdated" => issueStatuses.OrderByDescending(i => i.Pinned).ThenBy(i => i.DateUpdated),
                        "DateReported" => issueStatuses.OrderByDescending(i => i.Pinned).ThenBy(i => i.DateReported),
                        _ => issueStatuses.OrderByDescending(i => i.Pinned).ThenBy(i => i.DateReported)
                    };
                }
                else
                {
                    issueStatuses = issueReportStatusParams.SortBy switch {
                        "Type" => issueStatuses.OrderBy(i => i.IssueType),
                        "Reports" => issueStatuses.OrderBy(i => i.IssueReportCount),
                        "Status" => issueStatuses.OrderBy(i => i.Status),
                        "DateUpdated" => issueStatuses.OrderBy(i => i.DateUpdated),
                        "DateReported" => issueStatuses.OrderBy(i => i.DateReported),
                        _ => issueStatuses.OrderBy(i => i.DateReported)
                    };
                }
            }
            else
            {
                if(issueReportStatusParams.PinnedOnTop)
                {
                    issueStatuses = issueReportStatusParams.SortBy switch {
                        "Type" => issueStatuses.OrderByDescending(i => i.Pinned).ThenByDescending(i => i.IssueType),
                        "Reports" => issueStatuses.OrderByDescending(i => i.Pinned).ThenByDescending(i => i.IssueReportCount),
                        "Status" => issueStatuses.OrderByDescending(i => i.Pinned).ThenByDescending(i => i.Status),
                        "DateUpdated" => issueStatuses.OrderByDescending(i => i.Pinned).ThenByDescending(i => i.DateUpdated),
                        "DateReported" => issueStatuses.OrderByDescending(i => i.Pinned).ThenByDescending(i => i.DateReported),
                        _ => issueStatuses.OrderByDescending(i => i.Pinned).ThenByDescending(i => i.DateReported)
                    };
                }
                else
                {
                    issueStatuses = issueReportStatusParams.SortBy switch {
                        "Type" => issueStatuses.OrderByDescending(i => i.IssueType),
                        "Reports" => issueStatuses.OrderByDescending(i => i.IssueReportCount),
                        "Status" => issueStatuses.OrderByDescending(i => i.Status),
                        "DateUpdated" => issueStatuses.OrderByDescending(i => i.DateUpdated),
                        "DateReported" => issueStatuses.OrderByDescending(i => i.DateReported),
                        _ => issueStatuses.OrderByDescending(i => i.DateReported)
                    };
                }
                
            }

            // if(issueReportStatusParams.PinnedOnTop){
            //     issueStatuses = issueStatuses.OrderByDescending(i => i.Pinned).ThenBy;
            // }

            // if(issueReportStatusParams.IssueTypeAccess.Length > 0)
            //     issueStatuses = issueStatuses.Where(i => issueReportStatusParams.IssueTypeAccess.Contains(i.IssueType));

            // if(issueReportStatusParams.DistrictAccess.Length > 0)
            //     issueStatuses = issueStatuses.Where(i => issueReportStatusParams.DistrictAccess.Contains(i.District));

            // if(issueReportStatusParams.IssueTypeAccess.Length > 0){
            //     issueStatuses = issueStatuses.Where(i => i.IssueTypes.AsEnumerable().Contains("Flooding"));
            // }
                // issueStatuses = issueStatuses.Where(i => issueReportStatusParams.IssueTypeAccess.Any(x => i.IssueTypes.Any(y => y == x)));
                // issueStatuses = issueStatuses.Where(i => issueReportStatusParams.IssueTypeAccess.Intersect(i.IssueTypes).Count() > 0);

            // if(issueReportStatusParams.DistrictAccess.Length > 0)
            //     issueStatuses = issueStatuses.Where(i => issueReportStatusParams.DistrictAccess.Any(x => i.Districts.Any(y => y == x)));
            //     // issueStatuses = issueStatuses.Where(i => issueReportStatusParams.DistrictAccess.Intersect(i.Districts).Count() > 0);

            if(issueReportStatusParams.dateLower != null)
            {
                issueStatuses = issueStatuses.Where(i => i.DateReported >= issueReportStatusParams.dateLower);
            }

            if(issueReportStatusParams.dateUpper != null)
            {
                issueStatuses = issueStatuses.Where(i => i.DateReported <= issueReportStatusParams.dateUpper);
            }
            
            var results = await PagedList<IssueReportStatusDto>.CreateAsync(issueStatuses, issueReportStatusParams.PageNumber, issueReportStatusParams.PageSize);

            return results;
        }

        /// <summary>
        /// This get all the issue reports for a status
        /// </summary>
        /// <param name="issueReportStatusParams">Specifies the issue status id and the query paramters</param>
        /// <returns>Paginated list of issue reports for a status</returns>
        public async Task<PagedList<IssueReportItemDto>> GetIssueReportsForStatus(IssueReportParams issueReportParams)
        {
            var query = _context.IssueReports
                .Where(ir => ir.IssueStatusId == issueReportParams.IssueStatusId)
                .Include(ir => ir.District)
                .Include(ir => ir.IssueStatus)
                .Include(ir => ir.AppUser)
                .OrderByDescending(ir => ir.DateReported)
                .AsQueryable();

            query = issueReportParams.SortBy switch {
                _ => query.OrderByDescending(ir => ir.DateReported)
            };

            var issueReports = query.ProjectTo<IssueReportItemDto>(_mapper.ConfigurationProvider);

            return await PagedList<IssueReportItemDto>.CreateAsync(issueReports, issueReportParams.PageNumber, issueReportParams.PageSize);
        }  
        
        /// <summary>
        /// Get only unreviewed issue reports with coordinates for a particular issue type. Used for proximity grouping
        /// </summary>
        /// <param name="id">Id of the issue type</param>
        /// <returns>List of issue reports</returns>
        public async Task<IEnumerable<IssueReport>> GetUnreviewedWithCoordinatesByIssueType(int id)
        {
            return await _context.IssueReports
                .Include(ir => ir.IssueStatus)
                .Include(ir => ir.IssueStatus).ThenInclude(i => i.CurrentStatus)
                .Where(ir => (ir.IssueTypeId == id &&
                              ir.LocationLatitude != 0.0f &&
                              ir.LocationLongitude != 0.0f  &&
                              ir.IssueStatus.CurrentStatus.Name != "Canceled" &&
                              ir.IssueStatus.CurrentStatus.Name != "Completed" ))
                .ToListAsync();
        }

        /// <summary>
        /// Add an issue report
        /// </summary>
        /// <param name="issueReports">Issue report</param>
        public void AddIssueReports(IEnumerable<IssueReport> issueReports)
        {
            _context.IssueReports.AddRange(issueReports);
        }

        /// <summary>
        /// Get issue reports but restrict by a platform
        /// </summary>
        /// <param name="platform">Platform name</param>
        /// <returns>List of issue reports</returns>
        public async Task<IEnumerable<IssueReport>> GetIssueReportsByPlatform(string platform)
        {
            return await _context.IssueReports.Where(ir => ir.Platform == platform)
                // .Include(x => x.IssueStatus).ThenInclude(y => y.Remarks)
                .Include(x => x.IssueStatus).ThenInclude(i => i.District)
                .Include(x => x.IssueStatus).ThenInclude(i => i.CurrentStatus)
                .Include(x => x.IssueStatus).ThenInclude(i => i.StatusUpdates.OrderByDescending(su => su.Date))
                .Include(x => x.IssueType)
                .Include(x => x.District)
                .ToListAsync();
        }

        /// <summary>
        /// Add a status update to the database
        /// </summary>
        /// <param name="statusUpdate">Status update</param>
        public void AddIssueStatusUpdate(StatusUpdate statusUpdate)
        {
            _context.StatusUpdates.Add(statusUpdate);
        }

        /// <summary>
        /// Gets the status updates for an issue status 
        /// </summary>
        /// <param name="issueReportParams">Specify issue status ID and query parameters</param>
        /// <returns>Paginated status updates</returns>
        public async Task<PagedList<IssueStatusUpdateDto>> AsyncGetIssueStatusUpdatesByIssueStatusId(IssueReportParams issueReportParams)
        {
            // var issueStatusUpdates = await _context.IssueStatus.Where(i => i.Id == issueReportParams.IssueStatusId)
            //     .Include(i => i.StatusUpdates).ThenInclude(s => s.Status)
            //     .Include(i => i.StatusUpdates).ThenInclude(s => s.PreviousStatus)
            //     .SelectMany(i => i.StatusUpdates
            //         .Select(s => new IssueStatusUpdateDto{
            //             Status = s.Status.Name,
            //             PreviousStatus = s.PreviousStatus.Name,
            //             Remark = s.Remark,
            //             DateRemarked = s.DateRemarked
            //         })
            //     )
            //     .ToListAsync();

            var query = _context.StatusUpdates.Where(i => i.IssueStatusId == issueReportParams.IssueStatusId)
                .Include(s => s.Status)
                .Include(s => s.PreviousStatus)
                .Include(s => s.AppUser)
                .Include(s => s.ApprovalItems)
                .AsQueryable();

            query = issueReportParams.SortBy switch {
                _ => query.OrderByDescending(ir => ir.DateReported)
            };

            var issueStatus = _context.IssueStatus
                .Include(i => i.StatusUpdates)
                .FirstOrDefault(i => i.Id == issueReportParams.IssueStatusId);

            var currentStatusUpdate = issueStatus.StatusUpdates.OrderByDescending(i => i.DateReported).FirstOrDefault();

            var currentStatusUpdateId = currentStatusUpdate?.Id;

            var issueStatusUpdates = query.ProjectTo<IssueStatusUpdateDto>(_mapper.ConfigurationProvider, new {currentStatusUpdateId = currentStatusUpdateId});

            //query = issueReportParams.SortBy switch {
            //     _ => query.OrderByDescending(ir => ir.DateReported)
            // };

            // try
            // {
            //     var data2 = await _context.StatusUpdates.ToListAsync();
            //     var data = await _context.StatusUpdates.Where(i => i.IssueStatusId == issueReportParams.IssueStatusId)
            //     .Include(s => s.Status)
            //     .Include(s => s.PreviousStatus)
            //     .Include(s => s.AppUser)
            //     .Include(s => s.ApprovalItems)
            //     .ToListAsync();
            // }
            // catch (System.Exception e)
            // {
            //     throw;
            // }

            var result = await PagedList<IssueStatusUpdateDto>.CreateAsync(issueStatusUpdates, issueReportParams.PageNumber, issueReportParams.PageSize);         
            
            // var result = result.Select(s => new IssueStatusUpdateDto{
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
            // });

            // var issueStatus = _context.IssueStatus.Where(i => i.Id == issueReportParams.IssueStatusId).SingleOrDefault();
            // var statusUpdates = issueStatus.StatusUpdates;
            // foreach(var item in result)
            // {
            //     var statusUpdate = statusUpdates.Where(a => a.Id == item.Id).SingleOrDefault();
            //     var images = statusUpdate.Images.Select(i => new IssueStatusUpdateDto.Image{Id = i.Id, Path = i.Path});
            //     item.Images = images;
            //     // item.Images = _context.IssueStatus.Where( i => i.Id == item.Id).Select(a => a.StatusUpdates).SelectMany(b => b.SelectMany(c => c.Images.Select(d => new IssueStatusUpdateDto.Image{Id = d.Id, Path = d.Path})));
            // }

            return result;
        }

        public async Task<List<IssueStatusUpdateDto>> AsyncGetIssueStatusUpdates(int statusId)
        {
            int issueStatusId = statusId;

            var query = _context.StatusUpdates.Where(i => i.IssueStatusId == issueStatusId)
                .Include(s => s.Status)
                .Include(s => s.PreviousStatus)
                .Include(s => s.AppUser)
                .Include(s => s.ApprovalItems)
                .AsQueryable();

            // query = issueReportParams.SortBy switch {
            //     _ => query.OrderByDescending(ir => ir.DateReported)
            // };

            var val = await query.ToListAsync();

            var issueStatusUpdates = query.ProjectTo<IssueStatusUpdateDto>(_mapper.ConfigurationProvider);

            var result = await issueStatusUpdates.ToListAsync();         
            
            return result;
        }

        /// <summary>
        /// The searches for issue statuses based on paramters
        /// </summary>
        /// <param name="issueStatusSearchParams">Specify query search paramters</param>
        /// <returns>Top 10 issue statuses for the search</returns>
        public async Task<IEnumerable<IssueReportStatusDto>> AsyncGetIssueStatusByDetails(IssueStatusSearchParams issueStatusSearchParams)
        {
            var issueStatusesQuery = _context.IssueStatus
                .Include(i => i.CurrentStatus)
                .Include(i => i.IssueReports)
                    .ThenInclude(ir => ir.IssueType)
                .Include(i => i.IssueReports)
                    .ThenInclude(ir => ir.District)
                .Include(i => i.District)
                .Include(i => i.IssueReports)
                .AsQueryable();
            
            if(issueStatusSearchParams.Completed != null && issueStatusSearchParams.Completed.Trim().ToLower() == "hide"){
                issueStatusesQuery = issueStatusesQuery.Where(i => i.CurrentStatus.Name == "Completed" || i.CurrentStatus.Name == "Canceled");
            }

            if(issueStatusSearchParams.IssueTypeIds?.Count() > 0){
                issueStatusesQuery = issueStatusesQuery.Where(i => i.IssueReports.Any(ir => ir.IssueStatusId != null ? issueStatusSearchParams.IssueTypeIds.Contains((int)ir.IssueTypeId) : false));
            }

            if(issueStatusSearchParams.IssueType != null && issueStatusSearchParams.IssueType.Trim() != ""){
                issueStatusesQuery = issueStatusesQuery.Where(i => i.IssueReports.Any(ir => ir.IssueType.Name == issueStatusSearchParams.IssueType.Trim()));
            }

            if(issueStatusSearchParams.DistrictIds?.Count() > 0){
                issueStatusesQuery = issueStatusesQuery.Where(i => i.District == null ? i.IssueReports.Any(ir => ir.DistrictId != null ? issueStatusSearchParams.DistrictIds.Contains((int)ir.DistrictId) : false):
                    issueStatusSearchParams.DistrictIds.Contains((int)i.DistrictId));
            }

            if(issueStatusSearchParams.District != null && issueStatusSearchParams.District.Trim() != ""){
                issueStatusesQuery = issueStatusesQuery.Where(i => i.District == null ?
                    (i.IssueReports.Select(i => i.District.Name).Distinct().Any(ir => ir == issueStatusSearchParams.District.Trim())) :
                    (i.District.Name == issueStatusSearchParams.District.Trim())
                );
            }

            issueStatusesQuery = issueStatusesQuery.OrderByDescending(i => i.IssueReports.FirstOrDefault().DateReported);
            
            if((issueStatusSearchParams.Description == null || issueStatusSearchParams.Description.Trim() == "") &&
               (issueStatusSearchParams.LocationDescription == null || issueStatusSearchParams.LocationDescription.Trim() == "") &&
               (issueStatusSearchParams.Subject == null || issueStatusSearchParams.Subject.Trim() == "")){
                return (await issueStatusesQuery.Take(10).ToListAsync()).Select(i => new IssueReportStatusDto {
                    DateReported = i.IssueReports.Select(ir => ir.DateReported).First(),
                    District = i.District == null ? i.IssueReports.FirstOrDefault(ir => ir.District != null)?.District.Name : i.District.Name,
                    Id = i.Id,
                    IssueReportCount = i.IssueReports.Count(),
                    IssueType = i.IssueReports.FirstOrDefault(ir => ir.IssueType != null)?.IssueType.Name,
                    Status = i.CurrentStatus.Name,
                    Description = i.Description == null ? i.IssueReports.FirstOrDefault(ir => ir.Description != null)?.Description : i.Description,
                    LocationDescription = i.LocationDescription == null ? i.IssueReports.FirstOrDefault(ir => ir.LocationDescription != null)?.LocationDescription : i.LocationDescription
                });
            }

            var issueStatuses = await issueStatusesQuery.ToListAsync();

            var issueStatusesChoices = issueStatuses.Select(i => String.Join(" ", i.IssueReports.Select(ir => ir.Description).ToArray().Append(i.Description)) + " " +
                String.Join(" ", i.IssueReports.Select(ir => ir.LocationDescription).ToArray().Append(i.LocationDescription)) + " " +
                String.Join(" ", i.IssueReports.Select(ir => ir.Subject)));

            var query = String.Join(" ", new[]{issueStatusSearchParams.Description?.Trim(), issueStatusSearchParams.LocationDescription?.Trim(), issueStatusSearchParams.Subject?.Trim()});

            var results = Process.ExtractTop(query, issueStatusesChoices, cutoff: 50, limit: 10);

            if(results.Count() == 0){
                return Enumerable.Empty<IssueReportStatusDto>();
            }

            var resultIndex = results.Select(r => r.Index);

            var resultValues = resultIndex.Select(i => new IssueReportStatusDto {
                DateReported = issueStatuses[i].IssueReports.Select(ir => ir.DateReported).First(),
                District = issueStatuses[i].District == null ? issueStatuses[i].IssueReports.FirstOrDefault(ir => ir.District != null)?.District.Name : issueStatuses[i].District.Name,
                Id = issueStatuses[i].Id,
                IssueReportCount = issueStatuses[i].IssueReports.Count(),
                IssueType = issueStatuses[i].IssueReports.FirstOrDefault(ir => ir.IssueType != null)?.IssueType.Name,
                Status = issueStatuses[i].CurrentStatus.Name,
                Description = issueStatuses[i].Description == null ? issueStatuses[i].IssueReports.FirstOrDefault(ir => ir.Description != null)?.Description : issueStatuses[i].Description,
                LocationDescription = issueStatuses[i].LocationDescription == null ? issueStatuses[i].IssueReports.FirstOrDefault(ir => ir.LocationDescription != null)?.LocationDescription : issueStatuses[i].LocationDescription
            });

            return resultValues;
        }

        /// <summary>
        /// Gets nearby issue reports given coodinates and an epsilon value. The Epsilon value is used to specify the radius to search for the issue reports
        /// </summary>
        /// <param name="issueTypeId">The issue type ID</param>
        /// <param name="locationLatitude">The latitude of the point</param>
        /// <param name="locationLongitude">The longitude of the point</param>
        /// <param name="completed">Weather or not to include completed issue statuses</param>
        /// <param name="eps">This valid is used to specify the radius to search</param>
        /// <returns>Top 10 nearly issue reports</returns>
        public async Task<IEnumerable<IssueReportStatusDto>> AsyncGetIssueStatusesByCoordinates(int issueTypeId, float locationLatitude, float locationLongitude, string completed, float eps)
        {
            //Find nearby issue reports
            Point newPoint = new Point(null, (float)locationLatitude, (float)locationLongitude);

            //This indicates how far away to check for similar isse reports
            eps *= eps;

            //Check for existing issue reports with coordinates
            var issueReportsWithCoordinatesQuery = _context.IssueReports
                .Include(ir => ir.IssueStatus)
                .Include(ir => ir.IssueStatus).ThenInclude(i => i.CurrentStatus)
                .Include(ir => ir.IssueStatus).ThenInclude(i => i.CurrentStatus)
                .Where(ir => (ir.IssueTypeId == issueTypeId &&
                              ir.LocationLatitude != 0.0f &&
                              ir.LocationLongitude != 0.0f))
                .AsQueryable();

            //Filter out completed/canceled issues 
            if(completed != null && completed.Trim().ToLower() == "hide"){
                issueReportsWithCoordinatesQuery = issueReportsWithCoordinatesQuery.Where(i => i.IssueStatus.CurrentStatus.Name == "Completed" || i.IssueStatus.CurrentStatus.Name == "Canceled");
            }

            var issueReportsWithCoordinates = await issueReportsWithCoordinatesQuery.ToListAsync();

            HashSet<int> nearbyStatusIds = new HashSet<int>();

            var currentPoints = new List<Point>();
            currentPoints = issueReportsWithCoordinates.Select(ir => new Point(ir, ir.LocationLatitude, ir.LocationLongitude)).ToList();

            nearbyStatusIds = DBSCAN.GetRegionSorted(currentPoints, newPoint, eps).Select(i => (int)i.IssueReport?.IssueStatusId).ToHashSet();            

            var nearByStatuses = (await _context.IssueStatus.Where(i => nearbyStatusIds.Contains(i.Id))
            .Include(i => i.CurrentStatus)
            .Include(i => i.IssueReports)
                .ThenInclude(ir => ir.IssueType)
            .Include(i => i.IssueReports)
                .ThenInclude(ir => ir.District)
            .Include(i => i.District)
            .Include(i => i.IssueReports)
            .Take(10)
            .ToListAsync()).Select(i => new IssueReportStatusDto {
                    DateReported = i.IssueReports.Select(ir => ir.DateReported).First(),
                    District = i.District == null ? i.IssueReports.FirstOrDefault(ir => ir.District != null)?.District.Name : i.District.Name,
                    Id = i.Id,
                    IssueReportCount = i.IssueReports.Count(),
                    IssueType = i.IssueReports.FirstOrDefault(ir => ir.IssueType != null)?.IssueType.Name,
                    Status = i.CurrentStatus.Name,
                    Description = i.Description == null ? i.IssueReports.FirstOrDefault(ir => ir.Description != null)?.Description : i.Description,
                    LocationDescription = i.LocationDescription == null ? i.IssueReports.FirstOrDefault(ir => ir.LocationDescription != null)?.LocationDescription : i.LocationDescription
                });

            return nearByStatuses;
        }

        public void DeleteIssueStatus(IssueStatus issueStatus)
        {
            _context.IssueStatus.Remove(issueStatus);
        }

        public void ClearStatusUpdateApprovalItems(StatusUpdate statusUpdate)
        {
            foreach(var item in statusUpdate.ApprovalItems){
                _context.ApprovalItems.Remove(item);
            }
        }

        public async Task<IEnumerable<IssueStatus>> AsyncGetIssueStatuses()
        {
            return await _context.IssueStatus.ToListAsync();
        }

        public async Task<Tuple<IEnumerable<IssueReportStatusDto>, int>> AsyncGetFilteredIssueStatuses(IssueReportStatusParams issueReportStatusParams = null, AppUser currentUser = null, int count = 10)
        {
            var query = _context.IssueStatus
                .Include(i => i.IssueReports.OrderByDescending(ir => ir.DateReported)).ThenInclude(ir => ir.District)
                .Include(i => i.IssueReports.OrderByDescending(ir => ir.DateReported)).ThenInclude(ir => ir.IssueType)
                .Include(i => i.CurrentStatus)
                .AsQueryable();

            if(issueReportStatusParams.StatusList != null)
            {
                query = query.Where(i => issueReportStatusParams.StatusList.Contains(i.CurrentStatus.Name));
            }

            if(issueReportStatusParams.IssueTypeAccess.Length > 0){
                query = query.Where(i => i.IssueReports.Any(x => issueReportStatusParams.IssueTypeAccess.Contains(x.IssueType.Name)));
            }

            if(issueReportStatusParams.DistrictAccess.Length > 0){
                query = query.Where(i => i.District != null ? issueReportStatusParams.DistrictAccess.Contains(i.District.Name) : i.IssueReports.Any(x => issueReportStatusParams.DistrictAccess.Contains(x.District.Name)));
            }

            if(issueReportStatusParams.OnlyNew)
            {
                query = query.Where(i => i.CurrentStatus.Default == true);
            }

            if(!issueReportStatusParams.ShowNew)
            {
                query = query.Where(i => i.CurrentStatus.Default != true);
            }

            var atThisPoint = await query.ToListAsync();

            var pinned = currentUser == null ? null : currentUser.UserPins.Select(i => i.IssueStatusId).ToList();
            var hidden = currentUser == null ? null : currentUser.UserHiddenItems.Select(i => i.IssueStatusId).ToList();
            
            query = query.Where(i => !hidden.Contains(i.Id));

            var issueStatuses = query.ProjectTo<IssueReportStatusDto>(_mapper.ConfigurationProvider, new { pinned = pinned, hidden = issueReportStatusParams.ShowHidden ? hidden : null});

            if(issueReportStatusParams.PinnedOnly)
            {
                issueStatuses = issueStatuses.Where(i => i.Pinned == true);
            }

            if(issueReportStatusParams.PinnedOnTop)
            {
                issueStatuses = issueReportStatusParams.SortBy switch {
                    "Type" => issueStatuses.OrderByDescending(i => i.Pinned).ThenByDescending(i => i.IssueType),
                    "Reports" => issueStatuses.OrderByDescending(i => i.Pinned).ThenByDescending(i => i.IssueReportCount),
                    "Status" => issueStatuses.OrderByDescending(i => i.Pinned).ThenByDescending(i => i.Status),
                    "DateUpdated" => issueStatuses.OrderByDescending(i => i.Pinned).ThenByDescending(i => i.DateUpdated),
                    "DateReported" => issueStatuses.OrderByDescending(i => i.Pinned).ThenByDescending(i => i.DateReported),
                    _ => issueStatuses.OrderByDescending(i => i.Pinned).ThenByDescending(i => i.DateReported)
                };
            }
            else
            {
                issueStatuses = issueReportStatusParams.SortBy switch {
                    "Type" => issueStatuses.OrderByDescending(i => i.IssueType),
                    "Reports" => issueStatuses.OrderByDescending(i => i.IssueReportCount),
                    "Status" => issueStatuses.OrderByDescending(i => i.Status),
                    "DateUpdated" => issueStatuses.OrderByDescending(i => i.DateUpdated),
                    "DateReported" => issueStatuses.OrderByDescending(i => i.DateReported),
                    _ => issueStatuses.OrderByDescending(i => i.DateReported)
                };
            }

            if(issueReportStatusParams.dateLower != null)
            {
                issueStatuses = issueStatuses.Where(i => i.DateReported >= issueReportStatusParams.dateLower);
            }

            var resultCount = await issueStatuses.CountAsync();
            var result = await issueStatuses.Take(count).ToListAsync();

            return new Tuple<IEnumerable<IssueReportStatusDto>, int>(result, resultCount);
        }

        public async Task<IEnumerable<Object>> AsyncGetIssueStatusTotals(string[] issueTypeAccess, string[] districtAccess, string type = null)
        {
            var query = _context.IssueStatus
                .Include(i => i.District)
                .Include(i => i.IssueReports).ThenInclude(ir => ir.District)
                .Include(i => i.IssueReports).ThenInclude(ir => ir.IssueType)
                .Include(i => i.CurrentStatus)
                .AsQueryable();

            if(issueTypeAccess.Length > 0){
                query = query.Where(i => i.IssueReports.Any(x => issueTypeAccess.Contains(x.IssueType.Name)));
            }

            if(districtAccess.Length > 0){
                query = query.Where(i => i.District != null ? districtAccess.Contains(i.District.Name) : i.IssueReports.Any(x => districtAccess.Contains(x.District.Name)));
            }

            query = query.Where(i => !i.CurrentStatus.Final);

            var issueStatuses = await query.ToListAsync();
            IEnumerable<Object> totals;

            if(type == "Issue Type")
            {
                totals = issueStatuses.GroupBy(grp => new {Id = grp.IssueReports.FirstOrDefault().IssueTypeId, IssueType = grp.IssueReports.FirstOrDefault().IssueType}).Select(grp => new {IssueType = grp.Key.IssueType.Name, Count = grp.Count()}).ToList();
                // totals = issueStatuses.GroupBy(grp => new {Id = grp.IssueReports.FirstOrDefault().IssueTypeId, IssueType = grp.IssueReports.FirstOrDefault().IssueType}).Select(grp => new {IssueType = grp.Key.IssueType.Name, Count = grp.Sum(i => i.Id)}).ToDictionary(i => i.IssueType, i => i.Count);
            }
            else
            {
                // totals = issueStatuses.GroupBy(grp => new {Id = grp.DistrictId != null ? grp.DistrictId : grp.IssueReports.DefaultIfEmpty(new IssueReport{DistrictId = -1}).FirstOrDefault(i => i.DistrictId != null).DistrictId, 
                //                                            District = grp.District != null ? grp.District : grp.IssueReports.FirstOrDefault(i => i.District != null)?.District}).Select(grp => new {District = grp.Key.District?.Name, Count = grp.Sum(i => i.Id)}).ToDictionary(i => i.District, i => i.Count);

                var list = issueStatuses.GroupBy(grp => new {Id = grp.DistrictId != null ? grp.DistrictId : grp.IssueReports.FirstOrDefault(i => i.DistrictId != null)?.DistrictId, 
                                                           District = grp.DistrictId != null ? grp.District : grp.IssueReports.FirstOrDefault().District}).Select(grp => new {District = grp.Key.District != null ? grp.Key.District?.Name : "Unknown", Count = grp.Count()}).ToList();

                totals = issueStatuses.GroupBy(grp => new {Id = grp.DistrictId != null ? grp.DistrictId : grp.IssueReports.FirstOrDefault(i => i.DistrictId != null)?.DistrictId, 
                                                           District = grp.DistrictId != null ? grp.District : grp.IssueReports.FirstOrDefault().District}).Select(grp => new {District = grp.Key.District != null ? grp.Key.District?.Name : "Unknown", Count = grp.Count()}).ToList();
    
            }

            return totals;
        }

        public async Task<List<IssueStatusReportDto>> AsyncGetIssueReports(IssueReportStatusParams issueReportStatusParams)
        {
            var query = _context.IssueReports
                .Include(i => i.IssueType)
                .Include(i => i.District)
                .Include(i => i.IssueStatus).ThenInclude(i => i.CurrentStatus)
                .Include(i => i.Images)
                .Include(i => i.AppUser)
                .AsQueryable();
            
            // if(issueReportStatusParams.IssueTypeAccess.Length > 0)
            //     query = query.Where(i => issueReportStatusParams.IssueTypeAccess.Contains(i.IssueType.Name));

            // if(issueReportStatusParams.DistrictAccess.Length > 0)
            //     query = query.Where(i => issueReportStatusParams.DistrictAccess.Contains(i.District.Name));

            if(issueReportStatusParams.StatusList.Length > 0)
                query = query.Where(i => issueReportStatusParams.StatusList.Contains(i.IssueStatus.CurrentStatus.Name));

            var reports = query.ProjectTo<IssueStatusReportDto>(_mapper.ConfigurationProvider);

            if(issueReportStatusParams.IssueTypeAccess.Length > 0)
                reports = reports.Where(i => issueReportStatusParams.IssueTypeAccess.Contains(i.IssueType));

            if(issueReportStatusParams.DistrictAccess.Length > 0)
                reports = reports.Where(i => issueReportStatusParams.DistrictAccess.Contains(i.District));

            if(issueReportStatusParams.dateLower != null)
            {
                reports = reports.Where(i => i.DateReported >= issueReportStatusParams.dateLower);
            }

            if(issueReportStatusParams.dateUpper != null)
            {
                reports = reports.Where(i => i.DateReported <= issueReportStatusParams.dateUpper);
            }

            var reportsResult = await reports.ToListAsync();

            return reportsResult;
        }

        public async Task<List<IssueReportStatusDto>> AsyncGetIssueReportStatuses(IssueReportStatusParams issueReportStatusParams)
        {
            var query = _context.IssueStatus
                .Include(i => i.IssueReports.OrderByDescending(ir => ir.DateReported)).ThenInclude(ir => ir.District)
                .Include(i => i.IssueReports.OrderByDescending(ir => ir.DateReported)).ThenInclude(ir => ir.IssueType)
                .Include(i => i.CurrentStatus)
                .AsQueryable();
            
            // if(!.ShowClosed)
            // {
            //     query = query.Where(i => !i.CurrentStatus.Final);
            // }

            if(issueReportStatusParams.StatusList.Length > 0)
            {
                query = query.Where(i => issueReportStatusParams.StatusList.Contains(i.CurrentStatus.Name));
            }

            // if(issueReportStatusParams.maxReportCount != null)
            // {
            //     query = query.Where(i => i.IssueReports.Count() <= issueReportStatusParams.maxReportCount);
            // }

            // if(issueReportStatusParams.minReportCount != null)
            // {
            //     query = query.Where(i => i.IssueReports.Count() >= issueReportStatusParams.minReportCount);
            // }

            // if(issueReportStatusParams.IssueTypeAccess.Length > 0){
            //     query = query.Where(i => i.IssueReports.Any(x => issueReportStatusParams.IssueTypeAccess.Contains(x.IssueType.Name)));
            // }

            // if(issueReportStatusParams.DistrictAccess.Length > 0){
            //     query = query.Where(i => i.District != null ? issueReportStatusParams.DistrictAccess.Contains(i.District.Name) : i.IssueReports.Any(x => issueReportStatusParams.DistrictAccess.Contains(x.District.Name)));
            // }

            // var pinned = currentUser == null ? null : currentUser.UserPins.Select(i => i.IssueStatusId).ToList();
            // var hidden = currentUser == null ? null : currentUser.UserHiddenItems.Select(i => i.IssueStatusId).ToList();
            
            // if(!issueReportStatusParams.ShowHidden) {
            //     query = query.Where(i => !hidden.Contains(i.Id));
            // }

            var issueStatuses = query.ProjectTo<IssueReportStatusDto>(_mapper.ConfigurationProvider);

            // string searchKey = null;
            // int? keyInt = null;
            
            // if(issueReportStatusParams.Key != null)
            // {
            //     searchKey = issueReportStatusParams.Key.Trim().ToUpper();

            //     //Check if the search key could be converted to an integer to be compared to the Id

            //     try
            //     {
            //         keyInt = Int32.Parse(searchKey);
            //     }
            //     catch (FormatException){ }

            //     if(keyInt != null){
            //         issueStatuses  = issueStatuses.Where(i => i.Id == keyInt ||
            //             i.LocationDescription.ToUpper().Contains(searchKey) || 
            //             i.Description.ToUpper().Contains(searchKey) ||
            //             i.IssueType.ToUpper().Contains(searchKey) ||
            //             i.District.ToUpper().Contains(searchKey) );
            //     }else{
            //         issueStatuses  = issueStatuses.Where(i => i.LocationDescription.ToUpper().Contains(searchKey) || 
            //             i.Description.ToUpper().Contains(searchKey) ||
            //             i.IssueType.ToUpper().Contains(searchKey) ||
            //             i.District.ToUpper().Contains(searchKey));
            //     }

            //     // if(keyInt != null){
            //     //     issueStatuses  = issueStatuses.Where(i => i.LocationDescription.ToUpper().Contains(searchKey) || 
            //     //         i.Description.ToUpper().Contains(searchKey) || 
            //     //         i.Id == keyInt || 
            //     //         i.IssueTypes.Select(i => i.ToUpper().Contains(searchKey)).Any() ||
            //     //         i.Districts.Select(i => i.ToUpper().Contains(searchKey)).Any() );
            //     // }else{
            //     //     issueStatuses  = issueStatuses.Where(i => i.LocationDescription.ToUpper().Contains(searchKey) || 
            //     //         i.Description.ToUpper().Contains(searchKey) ||
            //     //         i.IssueTypes.Select(i => i.ToUpper().Contains(searchKey)).Any() ||
            //     //         i.Districts.Select(i => i.ToUpper().Contains(searchKey)).Any() );
            //     // }

            //     //Test
            //     // var valuesAtThisPoint2 = await query.ToListAsync();
            //     // query  = query.Where(i => i.LocationDescription.ToUpper().Contains(searchKey));
            //     // var valuesAtThisPoint3 = await query.ToListAsync();
            // }


            // if(issueReportStatusParams.Order == "Ascending")
            // {
            //     if(issueReportStatusParams.PinnedOnTop)
            //     {
            //         issueStatuses = issueReportStatusParams.SortBy switch {
            //             "Type" => issueStatuses.OrderByDescending(i => i.Pinned).ThenBy(i => i.IssueType),
            //             "Reports" => issueStatuses.OrderByDescending(i => i.Pinned).ThenBy(i => i.IssueReportCount),
            //             "Status" => issueStatuses.OrderByDescending(i => i.Pinned).ThenBy(i => i.Status),
            //             "DateUpdated" => issueStatuses.OrderByDescending(i => i.Pinned).ThenBy(i => i.DateUpdated),
            //             "DateReported" => issueStatuses.OrderByDescending(i => i.Pinned).ThenBy(i => i.DateReported),
            //             _ => issueStatuses.OrderByDescending(i => i.Pinned).ThenBy(i => i.DateReported)
            //         };
            //     }
            //     else
            //     {
            //         issueStatuses = issueReportStatusParams.SortBy switch {
            //             "Type" => issueStatuses.OrderBy(i => i.IssueType),
            //             "Reports" => issueStatuses.OrderBy(i => i.IssueReportCount),
            //             "Status" => issueStatuses.OrderBy(i => i.Status),
            //             "DateUpdated" => issueStatuses.OrderBy(i => i.DateUpdated),
            //             "DateReported" => issueStatuses.OrderBy(i => i.DateReported),
            //             _ => issueStatuses.OrderBy(i => i.DateReported)
            //         };
            //     }
            // }
            // else
            // {
            //     if(issueReportStatusParams.PinnedOnTop)
            //     {
            //         issueStatuses = issueReportStatusParams.SortBy switch {
            //             "Type" => issueStatuses.OrderByDescending(i => i.Pinned).ThenByDescending(i => i.IssueType),
            //             "Reports" => issueStatuses.OrderByDescending(i => i.Pinned).ThenByDescending(i => i.IssueReportCount),
            //             "Status" => issueStatuses.OrderByDescending(i => i.Pinned).ThenByDescending(i => i.Status),
            //             "DateUpdated" => issueStatuses.OrderByDescending(i => i.Pinned).ThenByDescending(i => i.DateUpdated),
            //             "DateReported" => issueStatuses.OrderByDescending(i => i.Pinned).ThenByDescending(i => i.DateReported),
            //             _ => issueStatuses.OrderByDescending(i => i.Pinned).ThenByDescending(i => i.DateReported)
            //         };
            //     }
            //     else
            //     {
            //         issueStatuses = issueReportStatusParams.SortBy switch {
            //             "Type" => issueStatuses.OrderByDescending(i => i.IssueType),
            //             "Reports" => issueStatuses.OrderByDescending(i => i.IssueReportCount),
            //             "Status" => issueStatuses.OrderByDescending(i => i.Status),
            //             "DateUpdated" => issueStatuses.OrderByDescending(i => i.DateUpdated),
            //             "DateReported" => issueStatuses.OrderByDescending(i => i.DateReported),
            //             _ => issueStatuses.OrderByDescending(i => i.DateReported)
            //         };
            //     }
                
            // }

            // if(issueReportStatusParams.PinnedOnTop){
            //     issueStatuses = issueStatuses.OrderByDescending(i => i.Pinned).ThenBy;
            // }

            if(issueReportStatusParams.IssueTypeAccess.Length > 0)
                issueStatuses = issueStatuses.Where(i => issueReportStatusParams.IssueTypeAccess.Contains(i.IssueType));

            if(issueReportStatusParams.DistrictAccess.Length > 0)
                issueStatuses = issueStatuses.Where(i => issueReportStatusParams.DistrictAccess.Contains(i.District));

            // if(issueReportStatusParams.IssueTypeAccess.Length > 0){
            //     issueStatuses = issueStatuses.Where(i => i.IssueTypes.AsEnumerable().Contains("Flooding"));
            // }
                // issueStatuses = issueStatuses.Where(i => issueReportStatusParams.IssueTypeAccess.Any(x => i.IssueTypes.Any(y => y == x)));
                // issueStatuses = issueStatuses.Where(i => issueReportStatusParams.IssueTypeAccess.Intersect(i.IssueTypes).Count() > 0);

            // if(issueReportStatusParams.DistrictAccess.Length > 0)
            //     issueStatuses = issueStatuses.Where(i => issueReportStatusParams.DistrictAccess.Any(x => i.Districts.Any(y => y == x)));
            //     // issueStatuses = issueStatuses.Where(i => issueReportStatusParams.DistrictAccess.Intersect(i.Districts).Count() > 0);

            if(issueReportStatusParams.dateLower != null)
            {
                issueStatuses = issueStatuses.Where(i => i.DateReported >= issueReportStatusParams.dateLower);
            }

            if(issueReportStatusParams.dateUpper != null)
            {
                issueStatuses = issueStatuses.Where(i => i.DateReported <= issueReportStatusParams.dateUpper);
            }

            var results = await issueStatuses.ToListAsync();
            
            return results;
        }

        public async Task<StatusUpdate> AsyncGetIssueStatusUpdateById(int statusUpdateId)
        {
            var result = await _context.StatusUpdates
                .Include(i => i.ApprovalItems)
                .Include(i => i.PreviousStatus)
                .Include(i => i.AppUser)
                .Include(i => i.Images)
                .FirstOrDefaultAsync(i => i.Id == statusUpdateId);

            return result;
        }
    }
}