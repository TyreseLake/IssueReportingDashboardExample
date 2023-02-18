using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Helpers.DBSCANHelpers;
using API.Helpers.Pagination;
using API.Interfaces;
using AutoMapper;
using Ganss.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using NPOI.XSSF.UserModel;
using NPOI.SS.Util;

using NPOI.SS.UserModel;
using NPOI.OpenXmlFormats.Spreadsheet;
using System.Net.Http;

namespace API.Controllers
{
    /// <summary>
    /// This controller is responsible for carrying out issue reporting requests/commands
    /// </summary>
    [Authorize]
    public class IssueReportingController : BaseApiContoller
    {
        private const float EPS = 0.0003f; //Around a 30(37.04) meter radius after it is squared
        private const int MINPTS = 1; //Minimum points per cluster is 1
        private readonly IUserRepository _userRepository;
        private readonly IIssueTypeRepository _issueTypeRepository;
        private readonly IDistrictRepository _districtRepository;
        private readonly IMapper _mapper;
        private readonly IIssueReportingRepository _issueReportingRepository;
        private readonly IStatusRepository _statusRepository;
        private readonly IActivityRepository _activityRepository;
        public const string CONTROLLER_NAME = "Issue Reporting";
        public readonly IFileService _fileService;

        /// <summary>
        /// Constructor for controller
        /// </summary>
        /// <param name="userRepository">Access to the user repository service</param>
        /// <param name="issueTypeRepository">Access to the issue type repository service</param>
        /// <param name="districtRepository">Access to the district repository service</param>
        /// <param name="mapper">Access to the auto mapper service</param>
        /// <param name="issueReportingRepository">Access to the issue reporting repository service</param>
        /// <param name="statusRepository">Access to the status updates repository service</param>
        /// <param name="activityRepository">Access to the activity logging repository service</param>
        /// <param name="fileService">Access to the file service</param>
        public IssueReportingController(IUserRepository userRepository,
                                        IIssueTypeRepository issueTypeRepository,
                                        IDistrictRepository districtRepository,
                                        IMapper mapper,
                                        IIssueReportingRepository issueReportingRepository,
                                        IStatusRepository statusRepository,
                                        IActivityRepository activityRepository,
                                        IFileService fileService)
        {
            _districtRepository = districtRepository;
            _mapper = mapper;
            _issueReportingRepository = issueReportingRepository;
            _statusRepository = statusRepository;
            _activityRepository = activityRepository;
            _fileService = fileService;
            _userRepository = userRepository;
            _issueTypeRepository = issueTypeRepository;
        }

        /// <summary>
        /// This route creates an issue report based on the specified information in the issue report DTO
        /// </summary>
        /// <param name="issueReportDto">This object stores information for a new issue report</param>
        /// <returns>The created issue report and status information </returns>
        [Authorize(Policy = "RequireIssueManagementPrivileges")]
        [HttpPost("add-issue-report")]
        [ActionName("Add Issue Report")]
        public async Task<ActionResult> AddIssueReport([FromForm] IssueReportDto issueReportDto)
        {
            //Get the User
            var userName = User.GetUserName();
            var userId = User.GetUserId();
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user == null ){
                return Unauthorized("This user does not exist");
            }
            var userRole = User.GetUserRole();

            //Start Logging Activity
            var activityId = await _activityRepository.LogActivity("Add Issue Report", CONTROLLER_NAME, issueReportDto, user);

            try
            {
                //Get Additional Files (Images)
                IFormFileCollection files = Request.Form.Files;

                //Check if the platform is valid
                if(!(_issueReportingRepository.getPlatforms().Contains(issueReportDto.Platform)))
                {
                    var erroredResponse = BadRequest("Invalid Platform");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Check if issue type is valid
                var issueTypes = await _issueTypeRepository.GetIssueTypeNames();
                var reportIssueType = issueReportDto.IssueType;
                if(!issueTypes.Contains(reportIssueType))
                {
                    var erroredResponse = BadRequest("Unknown Issue Type Entry");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Check if the district is valid
                var reportDistrict = issueReportDto.District;
                if(reportDistrict != null && reportDistrict != "")
                {
                    var districts = await _districtRepository.GetDistrictNames();
                    if(!districts.Contains(reportDistrict))
                    {
                        var erroredResponse = BadRequest("Unknown District Entry");
                        await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        return erroredResponse;
                    }
                }

                //Get Issue Status
                IssueStatus newIssueStatus = null;
                if(issueReportDto.StatusId != null){
                    newIssueStatus = await _issueReportingRepository.GetIssueStatus((int)issueReportDto.StatusId);
                    if(newIssueStatus == null){
                        var erroredResponse = NotFound("Issue Status Not Found");
                        await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        return erroredResponse;
                    }

                    var resultError = await CheckUserAccess(newIssueStatus);
                    if(resultError != null){
                        return Unauthorized(resultError);
                    }
                }

                //For Issue Manager Users Only
                if(RolesHelper.RoleAllowsRestriction(userRole))
                {
                    //Check if the user has access to the issue type
                    var userIssueTypes = user.UserIssueTypes.Select(ut => ut.IssueType.Name);
                    if(userIssueTypes.Count() > 0)
                    {
                        if(!userIssueTypes.Contains(reportIssueType)){
                            var erroredResponse = Unauthorized("Not authorized to access this issue type");
                            await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                            return erroredResponse;
                        }
                    }

                    if(reportDistrict == null || reportDistrict == ""){
                        var erroredResponse = BadRequest("Please specify issue report district");
                        await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        return erroredResponse;
                    }
                    
                    var userDisticts = user.UserDistricts.Select(ud => ud.District.Name);
                    if(userDisticts.Count() > 0)
                    {
                        

                        if(!userDisticts.Contains(reportDistrict)){
                            var erroredResponse = Unauthorized("Not authorized to access this district");
                            await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                            return erroredResponse;
                        }
                    }
                }

                //Create new issue report
                var issueReport = new IssueReport
                {
                    Subject = issueReportDto.Subject?.Trim(),
                    Description = issueReportDto.Description?.Trim(),
                    LocationDescription = issueReportDto.LocationDescription?.Trim(),
                    Platform = issueReportDto.Platform?.Trim()
                };

                //Add issue to database
                _issueReportingRepository.AddIssueReport(issueReport);

                //Upload Images Submitted
                if(files != null)
                {
                    foreach (var image in files)
                    {
                        if(image != null)
                        {
                            byte[] fileBytes;

                            var fileExtension = Path.GetExtension(image.FileName);
                            var fileTitle = Path.GetFileName(image.FileName);

                            using (var memoryStream = new MemoryStream(new byte[image.Length])){
                                await image.CopyToAsync(memoryStream); 
                                fileBytes = memoryStream.ToArray();
                            }

                            var currentDateTime = DateTime.Now;
                            
                            var folderName = "Images/";

                            var fileName = fileTitle + currentDateTime.ToString("yyyy'-'MM'-'dd'T'HH'-'mm'-'ss");
                            var fullPath = folderName + fileName + fileExtension.ToString();

                            var result = await _fileService.Overwrite(fileBytes, folderName, fileName + fileExtension.ToString());
                        
                            if(!result){
                                var erroredResponse = Unauthorized("Error saving image");
                                await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                                return erroredResponse;
                            }

                            var newImage = new Image{
                                Path = fullPath
                            };

                            if(issueReport.Images == null)
                                issueReport.Images = new List<Image>();

                            issueReport.Images.Add(newImage);
                        }
                    }
                }

                //Set the date the issue report was made to now
                issueReport.DateReported = DateTime.Now;

                //Set the creator (App User) of the issue report
                //issueReport.AppUser = userEntity;
                var userEntity = await _userRepository.GetUserEntityAsync(user.Id);
                issueReport.AppUserId = userEntity.Id;

                //Set the issue report type of the issue report
                var issueType = await _issueTypeRepository.GetIssueTypeByName(issueReportDto.IssueType);
                issueReport.IssueType = issueType;
                issueReport.IssueTypeId = issueType.Id;

                if(issueReportDto.PhoneNumber != null && issueReportDto.PhoneNumber.Trim() != ""){
                    issueReport.ReporterEmail = issueReportDto.PhoneNumber.Trim();                            
                }

                if(issueReportDto.Address != null && issueReportDto.Address.Trim() != ""){
                    issueReport.ReporterAddress = issueReportDto.Address.Trim();                        
                }

                if(issueReportDto.Email != null && issueReportDto.Email.Trim() != ""){
                    issueReport.ReporterPhoneNumber = issueReportDto.Email.Trim();                      
                }

                //Set the issue report district if any
                if(issueReportDto.District != null && issueReportDto.District.Trim() != "")
                {
                    var district = await _districtRepository.GetDistrictByName(issueReportDto.District.Trim());
                    issueReport.District = district;
                    issueReport.DistrictId = district.Id;
                }

                // var pendingStatus = await _statusRepository.AsyncGetStatusByName("Pending");
                var pendingStatus = await _statusRepository.AsyncGetDefaultStatus();

                IssueStatus issueStatus = null;
                if(issueReportDto.StatusId == null)
                {
                    issueStatus = new IssueStatus();
                    
                    //Save the issue report to the database
                    _issueReportingRepository.AddIssueStatus(issueStatus);

                    issueStatus.CurrentStatusId = pendingStatus.Id;
                    issueStatus.CurrentStatus = pendingStatus;
                }
                else
                {
                    issueStatus = newIssueStatus;
                }

                if(issueReportDto.LocationLatitude != null && issueReportDto.LocationLongitude != null)
                {
                    issueReport.LocationLatitude = (float)issueReportDto.LocationLatitude;
                    issueReport.LocationLongitude = (float)issueReportDto.LocationLongitude;
                }

                issueReport.IssueStatus = issueStatus;
                issueReport.OrignalIssueSource = issueStatus;
                

                if(await _issueReportingRepository.SaveAllAsync())
                {
                    var newIssueReport = new {
                        Id = issueReport.Id,
                        AppUserId = user.Id,
                        AppUserName = user.UserName,
                        Subject = issueReport.Subject,
                        IssueType = issueReport.IssueType?.Name,
                        Description = issueReport.Description,
                        LocationDescription = issueReport.LocationDescription,
                        District = issueReport.District?.Name,
                        DateReported = issueReport.DateReported,
                        Platform = issueReport.Platform,
                        Status = issueReport.IssueStatus?.CurrentStatus?.Name,
                        StatusId = issueReport.IssueStatus?.Id,
                        issueReport.ReporterAddress,
                        issueReport.ReporterEmail,
                        issueReport.ReporterPhoneNumber
                    };
                    // Status = issueReport.IssueStatus?.Status,
                    var response = Ok(newIssueReport);
                    await _activityRepository.SetActivityStatus(activityId, SUCCESS_CODE, response);
                    return response;
                }
        
                var errored = BadRequest("Failed to create issue report");
                await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, errored);
                return errored;
            }
            catch (System.Exception ex)
            {
                await _activityRepository.SetActivityStatus(activityId, FAILURE_CODE, ex);
                throw;
            }
        }

        /// <summary>
        /// This route get all the issue report statuses based on filters/orders.
        /// </summary>
        /// <param name="issueStatusParams">These are the </param>
        /// <returns>A paginated list of issue reports</returns>
        [HttpGet("issue-report-statuses")]
        public async Task<ActionResult<IEnumerable<IssueReportStatusDto>>> GetIssueReportStatuses([FromQuery] IssueReportStatusParams issueStatusParams)
        {
            var userName = User.GetUserName();
            var userId = User.GetUserId();
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user == null ){
                return Unauthorized("This user does not exist");
            }
            var userRole = User.GetUserRole();
            string[] issueTypeAccess = null;
            string[] districtAccess = null;

            //Check if the user entered issue types to filter by
            if(issueStatusParams.IssueTypes != null)
            {
                //Check if they are valid
                issueTypeAccess = issueStatusParams.IssueTypes.Split(",").Select(i => i.Trim()).ToArray();
                var allIssueTypes = await _issueTypeRepository.GetIssueTypeNames();
                if(allIssueTypes.Intersect(issueTypeAccess).Count() != issueTypeAccess.Count()){
                    return NotFound("Invalid issue types");
                }
            }

            //Check if the user entered districts to filter by
            if(issueStatusParams.Districts != null)
            {
                //Check if they are valid
                districtAccess = issueStatusParams.Districts.Split(",").Select(i => i.Trim()).ToArray();
                var allDistricts = await _districtRepository.GetDistrictNames();
                if(allDistricts.Intersect(districtAccess).Count() != districtAccess.Count()){
                    return NotFound("Invalid districts");
                }
            }

            //Check if the user entered districts to filter by
            if(issueStatusParams.Status != null)
            {
                //Check if they are valid
                var status = issueStatusParams.Status.Split(",").Select(i => i.Trim()).ToArray();
                var allStatuses = await _statusRepository.AsyncGetAllStatusNames();
                if(allStatuses.Intersect(status).Count() != status.Count()){
                    return NotFound("Invalid Status");
                }
                issueStatusParams.StatusList = status;
            }

            //If the user is a default/IM user, filter issue types that they dont have access to
            if(RolesHelper.RoleAllowsRestriction(userRole))
            {
                //If the user issue types is empty, no filterering by issue types is done
                var userIssueTypes = user.UserIssueTypes.Select(ut => ut.IssueType.Name).ToArray();
                if(userIssueTypes.Length > 0)
                {
                    if(issueTypeAccess != null)
                    {
                        if(userIssueTypes.Intersect(issueTypeAccess).Count() != issueTypeAccess.Count()){
                            return Unauthorized("Not authorized to access selected issue type(s)");
                        }
                        issueStatusParams.IssueTypeAccess = issueTypeAccess;
                    }
                    else
                    {
                        issueStatusParams.IssueTypeAccess = userIssueTypes;
                    }
                }
                else
                {
                    if(issueTypeAccess != null)
                    {
                        issueStatusParams.IssueTypeAccess = issueTypeAccess;
                    }
                    else
                    {
                        issueStatusParams.IssueTypeAccess = new string[] {};
                    }
                }

                //If the user diustricts is empty, no filterering by districts is done
                var userDistricts = user.UserDistricts.Select(ut => ut.District.Name).ToArray();
                if(userDistricts.Length > 0)
                {
                    if(districtAccess != null)
                    {
                        if(userDistricts.Intersect(districtAccess).Count() != districtAccess.Count()){
                            return Unauthorized("Not authorized to access selected district(s)");
                        }
                        issueStatusParams.DistrictAccess = districtAccess;
                    }
                    else
                    {
                        issueStatusParams.DistrictAccess = userDistricts;
                    }
                }
                else
                {
                    if(districtAccess != null)
                    {
                        issueStatusParams.DistrictAccess = districtAccess;
                    }
                    else
                    {
                        issueStatusParams.DistrictAccess = new string[] {};
                    }
                }
            }
            else
            {
                if(issueTypeAccess != null)
                {
                    issueStatusParams.IssueTypeAccess = issueTypeAccess;
                }
                else
                {
                    issueStatusParams.IssueTypeAccess = new string[] {};
                }

                if(districtAccess != null)
                {
                    issueStatusParams.DistrictAccess = districtAccess;
                }
                else
                {
                    issueStatusParams.DistrictAccess = new string[] {};
                }
            }

            var issueStatuses = await _issueReportingRepository.AsyncGetPaginatedIssueReportStatuses(issueStatusParams, user);
            Response.AddPaginationHeader(issueStatuses.CurrentPage, issueStatuses.PageSize, issueStatuses.TotalCount, issueStatuses.TotalPages);

            //Save user search/filtering params
            var userParams = new UserFilterParams {
                IssueTypeAccess = issueStatusParams.IssueTypeAccess,
                DistrictAccess = issueStatusParams.DistrictAccess,
                StatusList = issueStatusParams.StatusList,
                SortBy = issueStatusParams.SortBy,
                Order = issueStatusParams.Order,
                ShowClosed = issueStatusParams.ShowClosed,
                PinnedOnTop = issueStatusParams.PinnedOnTop,
                ShowHidden = issueStatusParams.ShowHidden
            };

            try 
            {
                var userParamsJSON = JsonConvert.SerializeObject(userParams);
                if(userParamsJSON != user.UserParams)
                {
                    user.UserParams = userParamsJSON;
                    var userSaveResult = await _userRepository.SaveAllAsync();  
                }
            }
            catch (System.Exception)
            {
                
            }
            

            return issueStatuses;
        }

        /// <summary>
        /// This route gets all the issue reports for a particular issue status.
        /// </summary>
        /// <param name="id">Specifies the ID of the issue status to fetch the issue reports for</param>
        /// <param name="issueReportParams">Specifies how the issue report should be sorted</param>
        /// <returns>Paginated list of issue reports for an issue status</returns>
        [HttpGet("issue-reports/{id}")]
        public async Task<ActionResult<IEnumerable<IssueReportItemDto>>> GetIssueReports([FromRoute] int id, [FromQuery] IssueReportParams issueReportParams)
        {
            var userId = User.GetUserId();
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user == null ){
                return Unauthorized("This user does not exist");
            }

            var issueStatus = await _issueReportingRepository.GetIssueStatus((int)id);

            if (issueStatus == null){
                return NotFound();
            }

            // var userRole = User.GetUserRole();
            // if(userRole == "Default" || userRole == "Issue Manager")
            // {
            //     var user = await _userRepository.GetUserByUsernameAsync(User.GetUserName());
            //     var statusIssueTypes = issueStatus.IssueReports.Select(ir => ir.IssueType.Name);
            //     if(!CheckUserIssueTypeAccess(user, statusIssueTypes))
            //         return Unauthorized("Not authorized to access this issue. Issue type requirements were not met");
                
            //     IEnumerable<string> statusDistricts;
            //     if(issueStatus.District != null)
            //     {
            //         statusDistricts = new List<string>();
            //         statusDistricts = statusDistricts.Append(issueStatus.District.Name);
            //     }
            //     else
            //         statusDistricts = issueStatus.IssueReports.Select(ir => ir.District.Name);
            //     if(!CheckUserDistrictAccess(user, statusDistricts))
            //         return Unauthorized("Not authorized to access this issue. District requirements were not met");
            // }

            var resultError = await CheckUserAccess(issueStatus);
            if(resultError != null){
                return Unauthorized(resultError);
            }

            issueReportParams.IssueStatusId = id;

            var issueReports = await _issueReportingRepository.GetIssueReportsForStatus(issueReportParams);

            Response.AddPaginationHeader(issueReports.CurrentPage, issueReports.PageSize, issueReports.TotalCount, issueReports.TotalPages);

            return issueReports;
        }

        /// <summary>
        /// This route gets all the details for a specific issue status
        /// </summary>
        /// <param name="id">Specifies the ID of the issue status</param>
        /// <returns>Issue status details</returns>
        [HttpGet("issue-report-details/{id}")]
        public async Task<ActionResult<IssueReportDetailsDto>> GetIssueDetails(int id)
        {
            var userId = User.GetUserId();
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user == null ){
                return Unauthorized("This user does not exist");
            }

            var issueStatus = await _issueReportingRepository.GetIssueStatus((int)id);

            if (issueStatus == null){
                return NotFound("Issue Not Found");
            }

            var resultError = await CheckUserAccess(issueStatus);
            if(resultError != null){
                return Unauthorized(resultError);
            }

            var firstIssueReport = issueStatus.IssueReports.FirstOrDefault();
            var firstWithCoords = issueStatus.IssueReports.FirstOrDefault(ir => ir.LocationLatitude != 0.0f || ir.LocationLongitude != 0.0f);
            var issueStatusImages = issueStatus.IssueReports.Where(x => x.Images?.Count() > 0)?.SelectMany(y => y.Images)?.Distinct().Select(z => new IssueReportDetailsDto.Image{Id = z.Id, Path = z.Path}).ToList();

            IssueReportDetailsDto issueReportDetailsDto = new IssueReportDetailsDto{
                Id = issueStatus.Id,
                IssueType = firstIssueReport?.IssueType.Name,
                IssueLocation = (issueStatus.LocationDescription != null) ? issueStatus?.LocationDescription : issueStatus?.IssueReports.FirstOrDefault(x => x.LocationDescription != null)?.LocationDescription,
                Description = (issueStatus.Description != null) ? issueStatus.Description : issueStatus.IssueReports.FirstOrDefault(x => x.Description != null)?.Description,
                District = issueStatus.District != null ? issueStatus.District?.Name :  issueStatus.IssueReports.FirstOrDefault(x => x.DistrictId != null)?.District.Name,
                LocationLongitude = firstWithCoords == null ? 0.0f : firstWithCoords.LocationLongitude,
                LocationLatitude = firstWithCoords == null ? 0.0f : firstWithCoords.LocationLatitude,
                CurrentStatus = issueStatus.CurrentStatus?.Name,
                Images = issueStatusImages,
                DateReported = firstIssueReport?.DateReported,
                ReportCount = issueStatus.IssueReports?.Count(),
                PlatformCounts = issueStatus.IssueReports?
                    .GroupBy(ir => ir.Platform)
                    .Select(ig => new IssueReportDetailsDto.ItemCount{
                        Name = ig.FirstOrDefault().Platform,
                        Count = ig.Count()
                    })
            };

            return Ok(issueReportDetailsDto);
        }
        
        /// <summary>
        /// Move an issue report from one issue status to another or to a new issue
        /// </summary>
        /// <param name="moveIssueReportDto">Specifies the source and destination issue</param>
        /// <returns>The issue report and destination issue status inforamtion</returns>
        [Authorize(Policy = "RequireIssueManagementPrivileges")]
        [HttpPost("move-issue-reports")]
        [ActionName("Move Issue Reports")]
        public async Task<ActionResult> MoveIssueReport(MoveIssueReportDto moveIssueReportDto)
        {
             //Start Logging Activity
            var currentUser = await _userRepository.GetUserByIdAsync(User.GetUserId());
            if(currentUser == null ){
                return Unauthorized("This user does not exist");
            }
            var activityId = await _activityRepository.LogActivity("Move Issue Reports", CONTROLLER_NAME, moveIssueReportDto, currentUser);
            
            try
            {
                //Check if the issue source is valid
                var issueStatusSource = await _issueReportingRepository.GetIssueStatus(moveIssueReportDto.SourceId);
                if(issueStatusSource == null){
                    var erroredResponse = NotFound("Issue source not found");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Check if user has access to the source
                var userRole = User.GetUserRole();
                var resultError = await CheckUserAccess(issueStatusSource);
                if(resultError != null){
                    return Unauthorized(resultError);
                }

                //check if the user is attempting to move and issue back to its own issue status
                if(moveIssueReportDto.DestinationId == issueStatusSource.Id){
                    var erroredResponse = BadRequest("Attempting to move an issue to the same location.");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //If the issue destination is -1, we create a new issue 
                IssueStatus issueStatusDestination;
                if(moveIssueReportDto.DestinationId == -1)
                {
                    // var pendingStatus = await _statusRepository.AsyncGetStatusByName("Pending");
                    var pendingStatus = await _statusRepository.AsyncGetDefaultStatus();
                    // issueStatusDestination = new IssueStatus
                    // {
                    //     Status = "Pending",
                    //     // CurrentStatusId = pendingStatus.Id,
                    //     // CurrentStatus = pendingStatus
                    // };

                    issueStatusDestination = new IssueStatus();

                    //Save the issue report to the database
                    _issueReportingRepository.AddIssueStatus(issueStatusDestination);

                    issueStatusDestination.CurrentStatusId = pendingStatus.Id;
                    issueStatusDestination.CurrentStatus = pendingStatus;
                }
                else
                {   
                    //If it is not -1, we get the destination and check if its valid
                    issueStatusDestination = await _issueReportingRepository.GetIssueStatus(moveIssueReportDto.DestinationId);
                    if(issueStatusDestination == null)
                    {
                        var erroredResponse = NotFound("Issue destination not found");
                        await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        return erroredResponse;
                    }

                    //Check if the user have access to the destination
                    var resultError2 = await CheckUserAccess(issueStatusDestination);
                    if(resultError2 != null){
                        return Unauthorized(resultError);
                    }

                }

                //Get the issue reports required to be moved from the issue report source
                var issueReports = new List<IssueReport>();
                for (int i = 0; i < moveIssueReportDto.IssueReportIds.Count(); i++)
                {
                    var newIssueReport = await _issueReportingRepository.GetIssueReport(moveIssueReportDto.IssueReportIds[i]);

                    if(newIssueReport == null)
                    {
                        var erroredResponse = NotFound("Issue report not found");
                        await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        return erroredResponse;
                    }

                    if(!issueStatusSource.IssueReports.Contains(newIssueReport))
                    {
                        var erroredResponse = NotFound("Issue report not found in issue source");
                        await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        return erroredResponse;
                    }
                    
                    issueReports.Add(newIssueReport);
                }

                //Move issue reports
                var currentDate = DateTime.Now;
                foreach (var issueReport in issueReports)
                {
                    issueStatusSource.IssueReports.Remove(issueReport);

                    issueReport.IssueStatus = issueStatusDestination; 
                    issueReport.IssueStatusId = issueStatusDestination.Id;

                    // issueStatusDestination.IssueReports.Add(issueReport);
                    issueReport.Moved = true;
                    issueReport.DateMoved = currentDate;
                }

                //Check if the Location is now empty
                bool wasDeleted = deleteEmptyIssueStatus(issueStatusSource);
                // bool wasDeleted = false;
                // if(issueStatusSource.IssueReports.Count() == 0)
                // {
                //     if((issueStatusSource.LocationDescription == null || issueStatusSource.LocationDescription?.Trim() == "") &&
                //        (issueStatusSource.Description == null || issueStatusSource.Description?.Trim() == "") && 
                //        (issueStatusSource.Remarks == null || issueStatusSource.Remarks?.Count() == 0) &&
                //        (issueStatusSource.StatusUpdates == null || issueStatusSource.StatusUpdates?.Count() == 0)
                //        )
                //     {
                //         _issueReportingRepository.DeleteIssueStatus(issueStatusSource);
                //         wasDeleted = true;
                //     }
                // }

                if(await _issueReportingRepository.SaveAllAsync())
                {
                    var response = Ok(new {
                        IssueSource = !wasDeleted ? new {
                            issueStatusSource.Id,
                            IssueReports = issueStatusSource.IssueReports?.Select(ir => new {
                                ir.Id
                            }) 
                        } : null,
                        IssueDestination = new {
                            issueStatusDestination.Id,
                            IssueReports = issueStatusDestination.IssueReports?.Select(ir => new {
                                ir.Id
                            })
                        },
                        Message = !wasDeleted ? "Successfully moved" : "Successfully moved, issue source was empty."
                    });
                    await _activityRepository.SetActivityStatus(activityId, SUCCESS_CODE, response);
                    return response;
                }

                var errored = BadRequest("An unexpected error occured while attempting to move issue report to the new issue destintation");
                await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, errored);
                return errored;
            }
            catch (System.Exception ex)
            {
                await _activityRepository.SetActivityStatus(activityId, FAILURE_CODE, ex);
                throw;
            }
        }

        /// <summary>
        /// This route import issue reports from CSV from the website (Mobile Application)
        /// </summary>
        /// <param name="file">The file being uploaded</param>
        /// <returns>Success message</returns>
        [Authorize(Policy = "RequireDataManagementPrivileges")]
        [HttpPost("import-issue-reports")]
        [ActionName("Import Issue Reports")]
        public async Task<ActionResult> ImportIssueReportsFromCSV([FromForm] IFormFile file)
        {
            //Get the User making the import
            var userName = User.GetUserName();
            var userId = User.GetUserId();
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user == null ){
                return Unauthorized("This user does not exist");
            }

            //Start Logging Activity
            var activityId = await _activityRepository.LogActivity("Import Issue Reports", CONTROLLER_NAME, file.FileName.ToString(), user);
            try
            {
                //If no file is submitted
                if(file == null){
                    var erroredResponse = BadRequest("No file submitted");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Get App Issue Reports from CSV File
                IEnumerable<IssueReportAppDto> appIssueReports;
                try
                {
                    using (var memoryStream = new MemoryStream(new byte[file.Length])){
                        await file.CopyToAsync(memoryStream); 
                        memoryStream.Position = 0;
                        appIssueReports = new ExcelMapper(memoryStream).Fetch<IssueReportAppDto>();
                    }
                }
                catch (System.Exception)
                {
                    var erroredResponse = BadRequest("Invalid Import File/File Formatting");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //If there are no issue reports to attempt to add, return BadRequest
                if(appIssueReports.Count() <= 0){
                    var erroredResponse = BadRequest("No items to import");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Check for invalid entries
                var invalidEntries = appIssueReports.Where(x => x.IssueId == null || x.Category == null || x.LocationLatitude == 0.0 || x.LocationLongitude == 0.0 );
                if(invalidEntries.Count() > 0){
                    var erroredResponse = BadRequest(new { Error = "Invalid Issue Reports", Values = invalidEntries});
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Filter out any App Issue Reports that already exist in the database
                //Get Issue Reports from database from mobile application
                var issueReportMobileIssueIds = (await _issueReportingRepository.GetIssueReportsByPlatform("Mobile Application")).Select(ir => ir.MobileIssueId);
                var filteredIssueReports = appIssueReports.Where(ir => issueReportMobileIssueIds.All( mid => ir.IssueId != mid));

                //return Ok(filteredIssueReports);

                //If there are no reports after filter, return successful, no new Issue Reports added
                if(filteredIssueReports.Count() == 0){
                    // var unchangedResponse = Ok("No new Issue Reports were added");
                    var unchangedResponse = Ok(new {NoChanges = true});
                    await _activityRepository.SetActivityStatus(activityId, SUCCESS_CODE, unchangedResponse);
                    return unchangedResponse;
                }

                //Return a Bad Request if the Issue Types are invalid
                var issueTypes = (await _issueTypeRepository.GetIssueTypeNames()).Select(x => x.ToLower());
                var errorAppIssueReports = filteredIssueReports.Where(x => !issueTypes.Contains(x.Category.ToLower()));
                if(errorAppIssueReports.Count() > 0){
                    var erroredResponse = BadRequest(new { Error = "Invalid Issue Categories", Values = errorAppIssueReports});
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                List<IssueReport> issueReports = new List<IssueReport>();

                foreach (var i in filteredIssueReports)
                {
                    var newReportIssueTypeId = (await _issueTypeRepository.GetIssueTypeByName(i.Category)).Id;
                    var newReport = new IssueReport {
                        MobileIssueId = i.IssueId,
                        MobileUserId = i.UserId,
                        AppUserId = user.Id,
                        // AppUser = user,
                        Subject = i.Subject,
                        IssueTypeId = newReportIssueTypeId,
                        Description = i.Description,
                        LocationDescription = i.LocationDescription,
                        LocationLongitude = (float)i.LocationLongitude,
                        LocationLatitude = (float)i.LocationLatitude,
                        DateReported = DateTime.Now,
                        Platform = "Mobile Application",
                    };

                    issueReports.Add(newReport);
                }

                //Get the Issue Types of the New Issues
                var usedIssueTypeIds = issueReports.Select(ir => ir.IssueTypeId).Distinct();
                
                var result = new List<IssueReport>();

                // var pendingStatus = await _statusRepository.AsyncGetStatusByName("Pending");
                var pendingStatus = await _statusRepository.AsyncGetDefaultStatus();

                //For each Issue Type
                foreach (var issueTypeId in usedIssueTypeIds)
                {
                    //Get the new issue reports for that issue type
                    var newIssueReports = issueReports.Where(ir => ir.IssueTypeId == issueTypeId);

                    //Create points for all the issue reports
                    List<Point> points = newIssueReports.Select(ir => new Point(ir, ir.LocationLatitude, ir.LocationLongitude)).ToList();

                    //This indicates how far away to check for similar isse reports
                    float eps = EPS; //Around a 30(37.04) meter radius
                    int minPts = MINPTS; //Minimum points per cluster is 1

                    //Check for existing issue reports with coordinates
                    var issueReportsWithCoordinates = await _issueReportingRepository.GetUnreviewedWithCoordinatesByIssueType((int)issueTypeId);
                    
                    if(issueReportsWithCoordinates.Count() > 0)
                    {
                        var issueStatusIds = issueReportsWithCoordinates.Select(ir => ir.IssueStatusId).Distinct().ToArray();

                        //Declare an empty group of clusters
                        var clusters = new List<List<Point>>();
                        
                        var currentPoints = new List<Point>();

                        //Initial cluster count
                        var initialCount = issueStatusIds.Count();

                        //Create clusters for existing issue reports with coordinates
                        for (var i = 0; i<initialCount; i += 1 )
                        {
                            clusters.Add(new List<Point>());

                            var newPoints = issueReportsWithCoordinates
                                .Where(ir => ir.IssueStatusId == issueStatusIds[i])
                                .Select(ir => new Point(ir, ir.LocationLatitude, ir.LocationLongitude){ClusterId = i+1});

                            currentPoints.AddRange(newPoints);
                            clusters[i].AddRange(newPoints);
                        }

                        var resultClusters = DBSCAN.AddRangeToCluster(clusters, currentPoints, points, eps, minPts);

                        for (var i = 0; i < resultClusters.Count(); i++)
                        {
                            if (resultClusters[i].Count() > 0)
                            {
                                IssueStatus issueStatus;
                                var cluster = resultClusters[i];

                                //Get existing status or create new status
                                if(i < initialCount )
                                {
                                    issueStatus = await _issueReportingRepository.GetIssueStatus((int)issueStatusIds[i]);
                                }
                                else
                                {
                                    issueStatus = new IssueStatus
                                    {
                                        // Status = "Pending",
                                        CurrentStatusId = pendingStatus.Id,
                                        // CurrentStatus = pendingStatus
                                    };

                                    _issueReportingRepository.AddIssueStatus(issueStatus);
                                }

                                foreach (var point in cluster)
                                {
                                    //Get each issue report from that cluster
                                    var newIssueReport = point.IssueReport;
                                    //Add that issue report to the new status
                                    newIssueReport.IssueStatus = issueStatus;
                                    newIssueReport.OrignalIssueSource = issueStatus;
                                }

                                //Save the issue report to the database
                                _issueReportingRepository.AddIssueReports(cluster.Select(c => c.IssueReport));

                                result.AddRange(cluster.Select(c => c.IssueReport));
                            }
                        }
                    }
                    else
                    {
                        //Since there are no issues with coordinates for that issue type, there are no exisiting clusters
                        //Generate clusters for the points
                        List<List<Point>> clusters = DBSCAN.GetClusters(points, eps, minPts);

                        foreach(var cluster in clusters)
                        {
                            //For each cluster, we create a new Issue Status
                            var newIssueStatus = new IssueStatus
                            {
                                // Status = "Pending",
                                CurrentStatusId = pendingStatus.Id,
                                // CurrentStatus = pendingStatus
                            };

                            foreach (var point in cluster)
                            {
                                //Get each issue report from that cluster
                                var newIssueReport = point.IssueReport;

                                //Add that issue report to the new status
                                newIssueReport.IssueStatus = newIssueStatus;
                                newIssueReport.OrignalIssueSource = newIssueStatus;
                            }

                            //Save the issue report to the database
                            _issueReportingRepository.AddIssueStatus(newIssueStatus);
                            _issueReportingRepository.AddIssueReports(cluster.Select(c => c.IssueReport));
                            
                            result.AddRange(cluster.Select(c => c.IssueReport));
                        }
                    }
                }

                //Save the newly added Reports and Statuses to the database
                if(await _issueReportingRepository.SaveAllAsync())
                {
                    // var newIssueReports = result.Select(i => new {
                    //     Id = i.Id,
                    //     AppId = i.MobileIssueId,
                    //     AppUserId = i.AppUserId,
                    //     AppUserName = user.UserName,
                    //     Subject = i.Subject,
                    //     IssueType = i.IssueType?.Name,
                    //     Description = i.Description,
                    //     LocationDescription = i.LocationDescription,
                    //     District = "",
                    //     DateReported = i.DateReported,
                    //     Platform = i.Platform,
                    //     StatusGroupId = i.IssueStatusId,
                    //     // Status = i.IssueStatus?.Status
                    //     Status = i.IssueStatus?.CurrentStatus.Name
                    // }) ;
                    // var response = Ok(newIssueReports);

                    var response = Ok(new {Message = "Successfully import issue reports."});
                    await _activityRepository.SetActivityStatus(activityId, SUCCESS_CODE, response);
                    return response;
                }
        
                var errored = BadRequest("Failed to create issue report");
                await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, errored);
                return errored;
            }
            catch (System.Exception ex)
            {
                await _activityRepository.SetActivityStatus(activityId, FAILURE_CODE, ex);
                throw;
            }
        }
        
        /// <summary>
        /// Add a status update and remark to an issue status
        /// </summary>
        /// <param name="newStatusUpdateDto">Specifies the issue status, as well as the next status update and the update remark</param>
        /// <returns>The status </returns>
        [Authorize(Policy = "RequireStatusUpdatePrivileges")]
        [HttpPost("issue-status-update")]
        [ActionName("Add Status Update")]
        public async Task<ActionResult> AddStatusUpdate([FromForm] NewStatusUpdateDto newStatusUpdateDto)
        {
            var userName = User.GetUserName();
            var userId = User.GetUserId();
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user == null ){
                return Unauthorized("This user does not exist");
            }

             //Start Logging Activity
            var activityId = await _activityRepository.LogActivity("Add Status Update", CONTROLLER_NAME, newStatusUpdateDto, user);

            try
            {
                //Get Additional Files (Images)
                IFormFileCollection files = Request.Form.Files;

                //Check if the issue status referenced is valid
                var issueStatus = await _issueReportingRepository.GetIssueStatus(newStatusUpdateDto.IssueStatusId);
                if(issueStatus == null){
                    var erroredResponse = NotFound("Issue Not Found");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Check if the user has access to the issue status referenced
                var resultError = await CheckUserAccess(issueStatus);
                if(resultError != null){
                    var erroredResponse = Unauthorized(resultError);
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Check if the new status is valid
                Status newStatus = null;
                if(newStatusUpdateDto.NewStatus != null){
                    newStatus = await _statusRepository.AsyncGetStatusByName(newStatusUpdateDto.NewStatus);
                    if(newStatus == null){
                        var erroredResponse = NotFound("The new status specified was not found");
                        await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        return erroredResponse;
                    }
                }
                
                // var prevStatus = await _statusRepository.AsyncGetStatusByName(newStatusUpdateDto.PreviousStatus);
                // if(prevStatus == null)
                //     return NotFound("The previous status specified was not found");

                //Check approval items
                if(newStatusUpdateDto.ApprovalItems != null && newStatusUpdateDto.ApprovalItems.Trim() != "")
                { 
                    try
                    {
                        newStatusUpdateDto.Approvals = JsonConvert.DeserializeObject<IEnumerable<string>>(newStatusUpdateDto.ApprovalItems);
                    }
                    catch (System.Exception)
                    {
                        var erroredResponse = BadRequest("Invalid approval items");
                        await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        return erroredResponse;
                    }
                }

                //Create a new status update
                var newStatusUpdate = new StatusUpdate {
                    AppUserId = user.Id,
                    // AppUser = user,
                    // StatusId = newStatus != null?  newStatus.Id : issueStatus.CurrentStatus.Id,
                    // Status = newStatus != null? newStatus: issueStatus.CurrentStatus,
                    PreviousStatusId = issueStatus.CurrentStatus?.Id,
                    /// PreviousStatus = issueStatus.CurrentStatus,
                    // Remark = newStatusUpdateDto.Remark,
                    // DateRemarked = DateTime.Now,
                    IssueStatusId = issueStatus.Id,
                    /// IssueStatus = issueStatus,
                    ResponsibleUnit = newStatusUpdateDto.ResponsibleUnit,
                    NewUnit = newStatusUpdateDto.NewUnit,
                    Date = newStatusUpdateDto.Date,
                    DateReported = DateTime.Now,
                    ApprovalItems = newStatusUpdateDto.Approvals?.Count() > 0 ? newStatusUpdateDto.Approvals.Select(a => new ApprovalItem{Description = a}).ToList() : null,
                    ReasonDetails = newStatusUpdateDto.ReasonDetails,
                    StatusUpdateDetails = newStatusUpdateDto.StatusUpdateDetails,
                    WorkType = newStatusUpdateDto.WorkType
                };

                //Set the current status to the new status
                issueStatus.CurrentStatusId = newStatus.Id;
                /// issueStatus.CurrentStatus = newStatus;

                _issueReportingRepository.AddIssueStatusUpdate(newStatusUpdate);

                //Upload Images Submitted to Issue Status Udpate
                if(files != null)
                {
                    foreach (var image in files)
                    {
                        if(image != null)
                        {
                            byte[] fileBytes;

                            var fileExtension = Path.GetExtension(image.FileName);
                            var fileTitle = Path.GetFileName(image.FileName);

                            using (var memoryStream = new MemoryStream(new byte[image.Length])){
                                await image.CopyToAsync(memoryStream); 
                                fileBytes = memoryStream.ToArray();
                            }

                            var currentDateTime = DateTime.Now;
                            
                            var folderName = "Images/";

                            var fileName = fileTitle + currentDateTime.ToString("yyyy'-'MM'-'dd'T'HH'-'mm'-'ss");
                            var fullPath = folderName + fileName + fileExtension.ToString();

                            var result = await _fileService.Overwrite(fileBytes, folderName, fileName + fileExtension.ToString());
                        
                            if(!result){
                                var erroredResponse = Unauthorized("Error saving image");
                                await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                                return erroredResponse;
                            }

                            var newImage = new StatusUpdateImage{
                                Path = fullPath
                            };

                            if(newStatusUpdate.Images == null)
                                newStatusUpdate.Images = new List<StatusUpdateImage>();

                            newStatusUpdate.Images.Add(newImage);
                        }
                    }
                }

                newStatusUpdate.StatusId = newStatus != null?  newStatus.Id : issueStatus.CurrentStatus.Id;
                var newStatusName = newStatus != null?  newStatus.Name : issueStatus.CurrentStatus.Name;
                /// newStatusUpdate.Status = newStatus != null? newStatus: issueStatus.CurrentStatus;

                issueStatus.StatusUpdates.Add(newStatusUpdate);

                if(await _issueReportingRepository.SaveAllAsync()){
                    var result = new {
                        // NewStatus = newStatusUpdate.Status?.Name,
                        NewStatus = newStatusName,
                        PreviousStatus = newStatusUpdate.PreviousStatus?.Name,
                        // newStatusUpdate.Remark,
                        // newStatusUpdate.DateRemarked,
                        newStatusUpdate.ResponsibleUnit,
                        newStatusUpdate.NewUnit,
                        newStatusUpdate.Date,
                        newStatusUpdate.DateReported, 
                        ApprovalItems = newStatusUpdate.ApprovalItems?.Select(a => a.Description).ToList(),
                        newStatusUpdate.ReasonDetails,
                        newStatusUpdate.StatusUpdateDetails,
                        newStatusUpdate.WorkType,
                        IssueStatusId = newStatusUpdate.IssueStatus?.Id
                    };
                
                    var response = Ok(result);
                    await _activityRepository.SetActivityStatus(activityId, SUCCESS_CODE, response);
                    return response;
                }

                var errored = BadRequest("An unexpected error occured when making the status update");
                await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, errored);
                return errored;
            }
            catch (System.Exception ex)
            {
                await _activityRepository.SetActivityStatus(activityId, SUCCESS_CODE, ex);
                throw;
            }
        }
        
        [Authorize(Policy = "RequireStatusUpdatePrivileges")]
        [HttpPost("edit-issue-status-update")]
        [ActionName("Add Status Update")]
        public async Task<ActionResult> EditStatusUpdate([FromForm] EditStatusUpdateDto editStatusUpdateDto)
        {
            var userName = User.GetUserName();
            var userId = User.GetUserId();
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user == null ){
                return Unauthorized("This user does not exist");
            }

             //Start Logging Activity
            var activityId = await _activityRepository.LogActivity("Add Status Update", CONTROLLER_NAME, editStatusUpdateDto, user);

            try
            {
                //Check if the issue status referenced is valid
                var statusUpdate = await _issueReportingRepository.AsyncGetIssueStatusUpdateById(editStatusUpdateDto.StatusUpdateId);
                if(statusUpdate == null){
                    var erroredResponse = NotFound("Status update not found");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                var issueStatus = await _issueReportingRepository.GetIssueStatus((int)statusUpdate.IssueStatusId);

                //Check if the user has access to the issue status referenced
                var resultError = await CheckUserAccess(issueStatus);
                if(resultError != null){
                    var erroredResponse = Unauthorized(resultError);
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Check if it is the users own status update or the user has previlleges to access all status updates
                var userRole = User.GetUserRole();
                if(!RolesHelper.RoleIsIssueManagement(userRole) && statusUpdate.AppUser.Id != userId)
                {
                    var erroredResponse = Unauthorized("Not authorized to access this issue status update");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Check if the new status is valid
                Status newStatus = null;
                if(editStatusUpdateDto.NewStatus != null){
                    newStatus = await _statusRepository.AsyncGetStatusByName(editStatusUpdateDto.NewStatus);
                    if(newStatus == null){
                        var erroredResponse = NotFound("The new status specified was not found");
                        await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        return erroredResponse;
                    }
                }

                //Make sure the user is not changing the status of any other status that isn't the current issues status
                var currentIssueStatusUpdateId = issueStatus.StatusUpdates.OrderByDescending(i => i.DateReported).FirstOrDefault().Id;
                if(currentIssueStatusUpdateId == statusUpdate.Id && issueStatus.CurrentStatusId != newStatus.Id){
                    var erroredResponse = BadRequest("Unable to modify this status update's status, this is not the current status update.");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Clear approval items
                _issueReportingRepository.ClearStatusUpdateApprovalItems(statusUpdate);

                //Check approval items
                if(editStatusUpdateDto.ApprovalItems != null && editStatusUpdateDto.ApprovalItems.Trim() != "")
                { 
                    try
                    {
                        editStatusUpdateDto.Approvals = JsonConvert.DeserializeObject<IEnumerable<string>>(editStatusUpdateDto.ApprovalItems);
                    }
                    catch (System.Exception)
                    {
                        var erroredResponse = BadRequest("Invalid approval items");
                        await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        return erroredResponse;
                    }
                }

                //Update status
                statusUpdate.StatusId = newStatus.Id;
                statusUpdate.DateEdited = DateTime.Now;
                statusUpdate.Edited = true;
                statusUpdate.ResponsibleUnit = editStatusUpdateDto.ResponsibleUnit?.Trim();
                statusUpdate.NewUnit = editStatusUpdateDto.NewUnit?.Trim();
                statusUpdate.Date = editStatusUpdateDto.Date;
                statusUpdate.ReasonDetails = editStatusUpdateDto.ReasonDetails?.Trim();
                statusUpdate.StatusUpdateDetails = editStatusUpdateDto.StatusUpdateDetails?.Trim();
                statusUpdate.WorkType = editStatusUpdateDto.WorkType?.Trim();
                statusUpdate.ApprovalItems = editStatusUpdateDto.Approvals?.Count() > 0 ? editStatusUpdateDto.Approvals.Select(a => new ApprovalItem{Description = a}).ToList() : null;
                
                if(issueStatus.StatusUpdates.First().Id == statusUpdate.Id){
                    issueStatus.CurrentStatusId = newStatus.Id;
                }

                if(await _issueReportingRepository.SaveAllAsync()){
                    var result = new {
                        Id = statusUpdate.Id,
                        Status = newStatus.Name,
                        PreviousStatus = statusUpdate.PreviousStatus?.Name,
                        statusUpdate.ResponsibleUnit,
                        statusUpdate.NewUnit,
                        statusUpdate.Date,
                        statusUpdate.DateReported, 
                        ApprovalItems = statusUpdate.ApprovalItems?.Select(a => a.Description).ToList(),
                        statusUpdate.ReasonDetails,
                        statusUpdate.StatusUpdateDetails,
                        statusUpdate.WorkType,
                        StatusId = newStatus.Id,
                        Images = statusUpdate.Images.Select(d => new IssueStatusUpdateDto.Image{Id = d.Id, Path = d.Path}),
                        DateEdited = statusUpdate.DateEdited,
                        Uploader = statusUpdate.AppUser.FirstName + (statusUpdate.AppUser.LastName != null ? " " + statusUpdate.AppUser.LastName : ""),
                        UserName = statusUpdate.AppUser.UserName
                    };
                    return Ok(result);
                }

                var errored = BadRequest("An unexpected error occured when editing the status update");
                await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, errored);
                return errored;
            }
            catch (System.Exception ex)
            {
                await _activityRepository.SetActivityStatus(activityId, SUCCESS_CODE, ex);
                throw;
            }
        }


        /// <summary>
        /// Get the status updates for an issue report
        /// </summary>
        /// <param name="id">Specifies the ID of the issue status</param>
        /// <param name="issueReportParams">Specifies the issue report query parameters</param>
        /// <returns>Paginated issue reports for a status</returns>
        [HttpGet("issue-status-updates/{id}")]
        public async Task<ActionResult<ICollection<IssueStatusUpdateDto>>> GetIssueStatusUpdates([FromRoute] int id, [FromQuery] IssueReportParams issueReportParams)
        {
            var userId = User.GetUserId();
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user == null ){
                return Unauthorized("This user does not exist");
            }

            //Check if the issue status referenced is valid
            var issueStatus = await _issueReportingRepository.GetIssueStatus(id);
            if(issueStatus == null){
                return NotFound("Issue Not Found");
            }

            //Check if the user has access to the issue status referenced
            var resultError = await CheckUserAccess(issueStatus);
            if(resultError != null){
                return Unauthorized(resultError);
            }

            issueReportParams.IssueStatusId = id;

            var issueStatusReports = await _issueReportingRepository.AsyncGetIssueStatusUpdatesByIssueStatusId(issueReportParams);
            
            Response.AddPaginationHeader(issueStatusReports.CurrentPage, issueStatusReports.PageSize, issueStatusReports.TotalCount, issueStatusReports.TotalPages);

            return Ok(issueStatusReports);
        }

        /// <summary>
        /// Gets only the descriptions for all issue reports for a specified issue status
        /// </summary>
        /// <param name="id">Specify the id of the status report</param>
        /// <param name="issueReportParams">Specifies the issue report query parameters</param>
        /// <returns>Paginated descriptions of issue reports</returns>
        [HttpGet("issue-report-descriptions/{id}")]
        public async Task<ActionResult<IEnumerable<string>>> GetDescriptions([FromRoute] int id, [FromQuery] IssueReportParams issueReportParams)
        {
            var userId = User.GetUserId();
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user == null ){
                return Unauthorized("This user does not exist");
            }

            //Check if the issue report status is valid
            var issueStatus = await _issueReportingRepository.GetIssueStatus((int)id);
            if (issueStatus == null)
                return NotFound("Issue Not Found");

            //Check if the user has access to the issue status specified
            var resultError = await CheckUserAccess(issueStatus);
            if(resultError != null)
                return Unauthorized(resultError);

            //Get the paginated issue status descriptions
            issueReportParams.IssueStatusId = id;
            var issueReports = await _issueReportingRepository.GetIssueReportsForStatus(issueReportParams);
            var description = issueReports.Where(i => i.Description != null && i.Description.Trim() != "").Select(i => i.Description.Trim()).Distinct();
            Response.AddPaginationHeader(issueReports.CurrentPage, issueReports.PageSize, issueReports.TotalCount, issueReports.TotalPages);

            return Ok(description);
        }

        /// <summary>
        /// Gets only the locations for all issue reports for a specified issue status
        /// </summary>
        /// <param name="id">Specify the id of the status report</param>
        /// <param name="issueReportParams">Specifies the issue report query parameters</param>
        /// <returns>Paginated locations of issue reports</returns>
        [HttpGet("issue-report-locations/{id}")]
        public async Task<ActionResult<IEnumerable<string>>> GetLocations([FromRoute] int id, [FromQuery] IssueReportParams issueReportParams)
        {
            var userId = User.GetUserId();
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user == null ){
                return Unauthorized("This user does not exist");
            }

            //Check if the issue report status is valid
            var issueStatus = await _issueReportingRepository.GetIssueStatus((int)id);
            if (issueStatus == null)
                return NotFound("Issue Not Found");

            //Check if the user has access to the issue status specified
            var resultError = await CheckUserAccess(issueStatus);
            if(resultError != null)
                return Unauthorized(resultError);

            //Get the paginated issue status descriptions
            issueReportParams.IssueStatusId = id;
            var issueReports = await _issueReportingRepository.GetIssueReportsForStatus(issueReportParams);
            var locations = issueReports.Where(i => i.LocationDescription != null && i.LocationDescription.Trim() != "").Select(i => i.LocationDescription.Trim()).Distinct();
            Response.AddPaginationHeader(issueReports.CurrentPage, issueReports.PageSize, issueReports.TotalCount, issueReports.TotalPages);

            return Ok(locations);
        }

        

        /// <summary>
        /// This route exports issue reports to a format that can be imported into the Content Management System for the mobile application.
        /// </summary>
        /// <returns>A CSV that can be imported to the CMS containing issue reports made by mobile users.</returns>
        [Authorize(Policy = "RequireDataManagementPrivileges")]
        [HttpGet("export-issue-reports")]
        public async Task<FileResult> ExportIssueReports(){
            //Convert all the issue reports into the format of the CSV document
            var mobileIssueReports =  (await _issueReportingRepository.GetIssueReportsByPlatform("Mobile Application")).Select(i => new IssueReportAppDto{
                IssueId = i.MobileIssueId,
                Subject = i.Subject,
                Category = i.IssueType?.Name,
                Description = i.Description,
                LocationDescription = i.LocationDescription,
                LocationLatitude = i.LocationLatitude,
                LocationLongitude = i.LocationLongitude,
                Images = null,
                UserId = i.MobileUserId,
                District = i.District?.Name,
                Status = GetMobileStatus(i.IssueStatus.CurrentStatus),
                ReviewType = GetReviewType(i.IssueStatus.CurrentStatus),
                Remarks = GetRemarks(i.IssueStatus),
                Date = i.DateReported,
                ClassName = "custom.Issue",
                DocumentCheckedOutVersionHistoryID = null
            }).ToList();

            //Convert the object list into a csv
            var serializedIssueReports = JsonConvert.SerializeObject(mobileIssueReports);
            var fileName = "Issue_Export.JSON";
            //Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(serializedIssueReports);
            var content = new System.IO.MemoryStream(bytes);
            return File(content , "application/json", fileName);
        }

        private string GetMobileStatus(Status currentStatus){
            if(currentStatus != null && currentStatus.Final)
                return "Reviewed";
            else
                return "Pending Review";
        }

        private string GetReviewType(Status currentStatus){
            if(currentStatus != null && currentStatus.Final)
            {
                if(currentStatus.Name == "Not Within Remit")
                    return "Not Under Remit";
                else
                    return currentStatus.Name;
            }
            else
                return null;
        }

        private string GetRemarks(IssueStatus issueStatus){
            if(issueStatus.CurrentStatus.Final)
            {
                var recentStatus = issueStatus.StatusUpdates.FirstOrDefault();
                string remark = null;
                if(recentStatus != null)
                {
                    remark = "Date: " + recentStatus.Date.ToLongDateString();
                    if(recentStatus.ReasonDetails != null && recentStatus.ReasonDetails.Trim() != "")
                    {
                        remark += "\nReason: " + recentStatus.ReasonDetails.Trim().TrimEnd('.') + ".";
                    }
                    if(recentStatus.StatusUpdateDetails != null && recentStatus.StatusUpdateDetails.Trim() != "")
                    {
                        remark += "\nDetails: " + recentStatus.StatusUpdateDetails.Trim().TrimEnd('.') + ".";
                    }
                }
                return remark;
            }
            else
            {
                return null;
            }
        }
        
        /// <summary>
        /// Update the location of an issue status
        /// </summary>
        /// <param name="updateStatusLocationDto">Specifies the id of the issue status, the issue status location description and the issue status district</param>
        /// <returns>The updated issue status</returns>
        [Authorize(Policy = "RequireIssueManagementPrivileges")]
        [HttpPost("update-issue-status-location")]
        [ActionName("Update Issue Location")]
        public async Task<ActionResult> UpdateIssueStatusLocation(UpdateStatusLocationDto updateStatusLocationDto){
            var currentUser = await _userRepository.GetUserByIdAsync(User.GetUserId());
            if(currentUser == null ){
                return Unauthorized("This user does not exist");
            }
            //Start Logging Activity
            var activityId = await _activityRepository.LogActivity("Update Issue Location", CONTROLLER_NAME, updateStatusLocationDto, currentUser);

            try
            {
                //Check if the issue status specified is valid
                var issueStatus = await _issueReportingRepository.GetIssueStatus(updateStatusLocationDto.StatusId);
                if(issueStatus == null){
                    var erroredResponse = NotFound("Issue not found");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Check if the user has access to the issue status specified
                var resultError = await CheckUserAccess(issueStatus);
                if(resultError != null){
                    var erroredResponse = Unauthorized(resultError);
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //check if the issue status update would result in no change
                if(((issueStatus.District == null && updateStatusLocationDto.District?.Trim() == "") || issueStatus.District?.Name == updateStatusLocationDto.District) &&
                    issueStatus.LocationDescription == updateStatusLocationDto.LocationDescription){
                    var response = Ok(new {Message = "No changes were made"});
                    await _activityRepository.SetActivityStatus(activityId, SUCCESS_CODE, response);
                    return response;
                }

                if(updateStatusLocationDto.District?.Trim() != issueStatus.District?.Name){
                    if(updateStatusLocationDto.District != null && updateStatusLocationDto.District.Trim() != ""){
                        //Check if District is valid
                        var updatedDistrict = await _districtRepository.GetDistrictByName(updateStatusLocationDto.District);
                        if(updatedDistrict == null){
                            var erroredResponse = NotFound("District not found");
                            await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                            return erroredResponse;
                        }

                        //Check if the user has access to this district
                        if(currentUser.UserDistricts.Count() > 0)
                        {
                            if(!(currentUser.UserDistricts.Select(i => i.District).Contains(updatedDistrict)))
                            {
                                var erroredResponse = Unauthorized("User not authorized to access this district");
                                await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                                return erroredResponse;
                            }
                        }

                        issueStatus.DistrictId = updatedDistrict.Id;
                        issueStatus.District = updatedDistrict;
                    }else{
                        issueStatus.DistrictId = null;
                        issueStatus.District = null;
                    }
                }

                //Update the issue status location description
                if(updateStatusLocationDto.LocationDescription != null && updateStatusLocationDto.LocationDescription.Trim() != "")
                    issueStatus.LocationDescription = updateStatusLocationDto.LocationDescription.Trim();
                else{
                    issueStatus.LocationDescription = null;
                }

                //var wasDeleted = deleteEmptyIssueStatus(issueStatus);

                if(await _issueReportingRepository.SaveAllAsync()){
                    ActionResult response;
                    response = Ok(new {
                        issueStatus.Id,
                        DistrictName = issueStatus.District?.Name,
                        IssueLocation = issueStatus.LocationDescription
                    });
                    await _activityRepository.SetActivityStatus(activityId, SUCCESS_CODE, response);
                    return response;
                }

                var errored = BadRequest("An unexpected error occured when making the status update");
                await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, errored);
                return errored;
            }
            catch (System.Exception ex)
            {
                await _activityRepository.SetActivityStatus(activityId, FAILURE_CODE, ex);
                throw;
            }
        }

        /// <summary>
        /// Update the discription of an issue status
        /// </summary>
        /// <param name="updateStatusDescription">Specifies the id of the issue status and the issue status description.</param>
        /// <returns>The updated issue status</returns>
        [Authorize(Policy = "RequireIssueManagementPrivileges")]
        [HttpPost("update-issue-status-description")]
        [ActionName("Update Issue Description")]
        public async Task<ActionResult> UpdateIssueStatusDescription(UpdateStatusDescription updateStatusDescription){
            var currentUser = await _userRepository.GetUserByIdAsync(User.GetUserId());
            if(currentUser == null ){
                return Unauthorized("This user does not exist");
            }
            //Start Logging Activity
            var activityId = await _activityRepository.LogActivity("Update Issue Description", CONTROLLER_NAME, updateStatusDescription, currentUser);
            
            try
            {
                //Check if the issue status specified is valid
                var issueStatus = await _issueReportingRepository.GetIssueStatus(updateStatusDescription.StatusId);
                if(issueStatus == null){
                    var erroredResponse = NotFound("Issue not found");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Check if the user has access to the issue status specified
                var resultError = await CheckUserAccess(issueStatus);
                if(resultError != null){
                    var erroredResponse = Unauthorized(resultError);
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Check if any changes was made
                if(issueStatus.Description == updateStatusDescription.Description?.Trim()){
                    var response = Ok(new {Message = "No changes were made"});
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, response);
                    return response;
                }

                //Update issue status description
                if(updateStatusDescription.Description != null && updateStatusDescription.Description.Trim() != "")
                    issueStatus.Description = updateStatusDescription.Description.Trim();
                else{
                    issueStatus.Description = null;
                }

                if(await _issueReportingRepository.SaveAllAsync()){
                    var response = Ok(new {
                        issueStatus.Id,
                        Description = issueStatus.Description
                    });
                    await _activityRepository.SetActivityStatus(activityId, SUCCESS_CODE, response);
                    return response;
                }

                var errored = BadRequest("An unexpected error occured when making the status update");
                await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, errored);
                return errored;
            }
            catch (System.Exception ex)
            {
                await _activityRepository.SetActivityStatus(activityId, FAILURE_CODE, ex);
                throw;
            }
        }

        /// <summary>
        /// Helper function to check if the user has access to an issue status, both issue status districts and issue status issue types
        /// </summary>
        /// <param name="issueStatus">The issue status to check against</param>
        /// <returns>Returns the respecitive error message if the use does not have access to the districts/issue types of an issue status. Returns null otherwise</returns>
        private async Task<string> CheckUserAccess(IssueStatus issueStatus)
        {
            //Get and check the users role
            var userRole = User.GetUserRole();
            if(RolesHelper.RoleAllowsRestriction(userRole))
            {
                //Check if the user has issue types restriction
                var user = await _userRepository.GetUserByUsernameAsync(User.GetUserName());
                var statusIssueTypes = issueStatus.IssueReports.Select(ir => ir.IssueType.Name).ToList();

                var userIssueTypes = user.UserIssueTypes.Select(ut => ut.IssueType.Name).ToList();
                if(userIssueTypes.Count() > 0)
                {
                    if(!(userIssueTypes.Intersect(statusIssueTypes).Count() > 0)){
                        return "Not authorized to access this issue. Issue type requirements were not met";
                    }
                }
                
                //Check if the user has districts restriction
                var userDistricts = user.UserDistricts.Select(ut => ut.District.Name).ToList();
                if(userDistricts.Count() > 0)
                {
                    List<string> statusDistricts;
                    if(issueStatus.District != null)
                    {
                        statusDistricts = new List<string>();
                        statusDistricts = statusDistricts.Append(issueStatus.District.Name).ToList();
                    }
                    else
                        statusDistricts = issueStatus.IssueReports.Select(ir => ir.District.Name).ToList();

                    if(statusDistricts.Count() == 0 || userDistricts.Intersect(statusDistricts).ToList().Count() == 0){
                        return "Not authorized to access this issue. District requirements were not met";
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Searches all issue statuses based on parameters 
        /// </summary>
        /// <param name="issueStatusSearchParams">Specifies the issue type, district, subject, description and location description to search by</param>
        /// <returns>The top 10 search closes and/or most recent search results</returns>
        [Authorize(Policy = "RequireIssueManagementPrivileges")]
        [HttpGet("search-issues")]
        public async Task<ActionResult> SearchIssueStatuses([FromQuery] IssueStatusSearchParams issueStatusSearchParams)
        {
            var userId = User.GetUserId();
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user == null ){
                return Unauthorized("This user does not exist");
            }
            //Checks if the issue type and district entered is valid
            if((issueStatusSearchParams.IssueType != null && issueStatusSearchParams.IssueType.Trim() != "") ||
               (issueStatusSearchParams.District != null && issueStatusSearchParams.District.Trim() != "")){
                var userRole = User.GetUserRole(); 
                
                if(issueStatusSearchParams.IssueType != null && issueStatusSearchParams.IssueType.Trim() != ""){
                    issueStatusSearchParams.IssueType = issueStatusSearchParams.IssueType.Trim();
                    var issueType = await _issueTypeRepository.GetIssueTypeByName(issueStatusSearchParams.IssueType);
                    if(issueType == null){
                        return NotFound("Issue type not found");
                    }
                    
                    if(RolesHelper.RoleAllowsRestriction(userRole))
                    {
                        var userIssueTypeIds = user.UserIssueTypes.Select(ut => ut.IssueTypeId).ToArray();
                        
                        if(userIssueTypeIds.Length > 0)
                        {
                            if(!userIssueTypeIds.Contains(issueType.Id)){
                                return Unauthorized("Invalid access to this issue type");
                            } 

                            issueStatusSearchParams.IssueTypeIds = userIssueTypeIds.ToList();
                        }
                    }
                }

                if(issueStatusSearchParams.District != null && issueStatusSearchParams.District.Trim() != ""){
                    issueStatusSearchParams.District = issueStatusSearchParams.District.Trim();
                    var district = await _districtRepository.GetDistrictByName(issueStatusSearchParams.District);
                    if(district == null){
                        return NotFound("District not found");
                    }

                    if(RolesHelper.RoleAllowsRestriction(userRole))
                    {
                        var userDistrictIds = user.UserDistricts.Select(ut => ut.DistrictId).ToArray();
                        
                        if(userDistrictIds.Length > 0)
                        {
                            if(!userDistrictIds.Contains(district.Id)){
                                return Unauthorized("Invalid access to this issue type");
                            } 

                            issueStatusSearchParams.DistrictIds = userDistrictIds.ToList();
                        }
                    }
                }
            }

            //Perform search
            var searchResult = await _issueReportingRepository.AsyncGetIssueStatusByDetails(issueStatusSearchParams);

            return Ok(searchResult);
        }
        
        /// <summary>
        /// This route is used to get nearby issue statuses given a location latitude and logitude
        /// </summary>
        /// <param name="issueType">specify issue type to restrict by</param>
        /// <param name="locationLatitude">Specify the latitude of the point</param>
        /// <param name="locationLongitude">Specify the longitude of the point</param>
        /// <param name="completed">Specify wether or not to display completed issues or not</param>
        /// <returns></returns>
        [Authorize(Policy = "RequireIssueManagementPrivileges")]
        [HttpGet("search-issue-coordinates")]
        public async Task<ActionResult> GetNearByIssueStatuses([FromQuery] string issueType, [FromQuery] float? locationLatitude, [FromQuery] float? locationLongitude, string completed = "show")
        {
            var userId = User.GetUserId();
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user == null ){
                return Unauthorized("This user does not exist");
            }

            if(locationLatitude == null || locationLongitude == null)
            {    
                return BadRequest("Location latitude and or longitute not provided");
            }

            var issueTypeEntity = await _issueTypeRepository.GetIssueTypeByName(issueType);
            if(issueTypeEntity == null)
            {
                return NotFound();
            }

            var eps = EPS;

            var results = await _issueReportingRepository.AsyncGetIssueStatusesByCoordinates(issueTypeEntity.Id, (float)locationLatitude, (float)locationLongitude, completed, eps);

            return Ok(results);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="issueReportDto"></param>
        /// <returns></returns>
        [Authorize(Policy = "RequireIssueManagementPrivileges")]
        [HttpPost("update-issue-report")]
        public async Task<ActionResult> UpdateIssueReport(IssueReportUpdateDto issueReportUpdateDto)
        {
            var currentUser = await _userRepository.GetUserByIdAsync(User.GetUserId());
            if(currentUser == null ){
                return Unauthorized("This user does not exist");
            }

            //Start Logging Activity
            var activityId = await _activityRepository.LogActivity("Edit Issue Report", CONTROLLER_NAME, issueReportUpdateDto, currentUser);
            try
            {
                //Check if issue report exists
                IssueReport issueReport = await _issueReportingRepository.GetIssueReport(issueReportUpdateDto.IssueReportId);
                if(issueReport == null)
                {
                    var erroredResponse = NotFound("Issue report not found");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Check if the user has access to the issue's status
                var issueStatus = await _issueReportingRepository.GetIssueStatus((int)issueReport.IssueStatusId);
                var resultError = await CheckUserAccess(issueStatus);
                if(resultError != null)
                {
                    var erroredResponse = Unauthorized(resultError);
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                
                //Get user role
                var userRole = User.GetUserRole();
                
                //Check if the user is allowed to edit this issue report
                
                if(!RolesHelper.RoleIsIssueManagement(userRole) && issueReport.AppUserId != currentUser.Id){
                    var erroredResponse = Unauthorized("User not allowed to make changes to this issue report");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                    // if(issueReport.Platform == "Mobile Application" || issueReport.AppUserId != currentUser.Id)
                    // {
                        
                    // }
                }
                

                //Check if the if issue report is within the editible period
                //--To Do--
                
                //Make changes to selected issue
                var changesWereMade = false;
                
                //Check if issuetype is valid and make changes
                string newIssueTypeName = null;
                if(issueReportUpdateDto.NewIssueType != null && issueReportUpdateDto.NewIssueType?.Trim() != "")
                {
                    var issueType = await _issueTypeRepository.GetIssueTypeByName(issueReportUpdateDto.NewIssueType);
                    if(issueType == null)
                    {
                        var erroredResponse = NotFound("Issue type specified does not exist");
                        await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        return erroredResponse;
                    }

                    if(RolesHelper.RoleAllowsRestriction(userRole))
                    {
                        var userIssueTypes = currentUser.UserIssueTypes.Select(ut => ut.IssueTypeId);
                        if(userIssueTypes.Count() > 0 && !(userIssueTypes.Contains(issueType.Id)))
                        // if(!(currentUser.UserIssueTypes.Select(x => x.IssueTypeId).Contains(issueType.Id)))
                        {
                            var erroredResponse = Unauthorized("Invalid access to the issue type specified");
                            await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                            return erroredResponse;
                        }
                    }

                    if(issueReport.IssueTypeId  != issueType.Id)
                    {
                        issueReport.IssueTypeId = issueType.Id;
                        newIssueTypeName = issueType.Name;
                        changesWereMade = true;
                    }
                }

                //Check if district is valid and make changes
                string newDistrictName = null;
                if(issueReportUpdateDto.NewDistrict != null)
                {
                    //These users must specify a district
                    if(RolesHelper.RoleAllowsRestriction(userRole))
                    {
                        if(issueReportUpdateDto.NewDistrict == ""){
                            var erroredResponse = BadRequest("Please specify issue report district");
                            await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                            return erroredResponse;
                        }
                    }

                    if(issueReportUpdateDto.NewDistrict.Trim() == "" && (issueReport.District != null))
                    {
                        issueReport.DistrictId = null;
                        issueReport.District = null;
                        newDistrictName = "";
                        changesWereMade = true;
                    }
                    else
                    {
                        var district = await _districtRepository.GetDistrictByName(issueReportUpdateDto.NewDistrict);
                        if(district == null)
                        {
                            var erroredResponse = NotFound("District specified does not exist");
                            await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                            return erroredResponse;
                        }

                        if(RolesHelper.RoleAllowsRestriction(userRole))
                        {
                            var userDistricts = currentUser.UserDistricts.Select(ut => ut.DistrictId);
                            if(userDistricts.Count() > 0 && !(userDistricts.Contains(district.Id)))
                            // if(!(currentUser.UserDistricts.Select(x => x.DistrictId).Contains(district.Id)))
                            {
                                var erroredResponse = Unauthorized("Invalid access to the district specified");
                            await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                            return erroredResponse;
                            }
                        }

                        if(issueReport.DistrictId  != district.Id)
                        {
                            issueReport.DistrictId = district.Id;
                            newDistrictName = district.Name;
                            changesWereMade = true;
                        }
                    }
                }

                //Check is platform is valid and make changes
                if(issueReportUpdateDto.NewPlatform != null && issueReportUpdateDto.NewPlatform.Trim() != "")
                {
                    if(issueReportUpdateDto.NewPlatform.Trim() != issueReport.Platform?.Trim())
                    {
                        //Check if the platform is valid
                        if(!((_issueReportingRepository.getPlatforms()).Contains(issueReportUpdateDto.NewPlatform))){
                            var erroredResponse = BadRequest("Invalid Platform");
                            await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                            return erroredResponse;
                        }

                        if(issueReport.Platform == "Mobile Application"){
                            var erroredResponse = BadRequest("Unable to change the platform from 'Mobile Application'");
                            await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                            return erroredResponse;
                        }

                        issueReport.Platform = issueReportUpdateDto.NewPlatform;
                        changesWereMade = true;
                    }
                }

                //Change the reporter address
                if(issueReportUpdateDto.NewAddress != null)
                {
                    if(issueReportUpdateDto.NewAddress.Trim() == "" && (issueReport.ReporterAddress != null && issueReport.ReporterAddress?.Trim() != ""))
                    {
                        issueReport.ReporterAddress = null;
                        changesWereMade = true;
                    }
                    else if(issueReportUpdateDto.NewAddress.Trim() != issueReport.ReporterAddress?.Trim())
                    {
                        issueReport.ReporterAddress = issueReportUpdateDto.NewAddress.Trim();
                        changesWereMade = true;
                    }
                }

                //Change the report description
                if(issueReportUpdateDto.NewDescription != null)
                {
                    if(issueReportUpdateDto.NewDescription.Trim() == "" && (issueReport.Description != null && issueReport.Description?.Trim() != ""))
                    {
                        issueReport.Description = null;
                        changesWereMade = true;
                    }
                    else if(issueReportUpdateDto.NewDescription.Trim() != issueReport.Description?.Trim())
                    {
                        issueReport.Description = issueReportUpdateDto.NewDescription.Trim();
                        changesWereMade = true;
                    }
                }
                //Change the reporter email
                if(issueReportUpdateDto.NewEmail != null)
                {
                    if(issueReportUpdateDto.NewEmail.Trim() == "" && (issueReport.ReporterEmail != null && issueReport.ReporterEmail?.Trim() != ""))
                    {
                        issueReport.ReporterEmail = null;
                        changesWereMade = true;
                    }
                    else if(issueReportUpdateDto.NewEmail.Trim() != issueReport.ReporterEmail?.Trim())
                    {
                        issueReport.ReporterEmail = issueReportUpdateDto.NewEmail.Trim();
                        changesWereMade = true;
                    }
                }
                //Change the report location description
                if(issueReportUpdateDto.NewLocationDescription != null)
                {
                    if(issueReportUpdateDto.NewLocationDescription.Trim() == "" && (issueReport.LocationDescription != null && issueReport.LocationDescription?.Trim() != ""))
                    {
                        issueReport.LocationDescription = null;
                        changesWereMade = true;
                    }
                    else if(issueReportUpdateDto.NewLocationDescription.Trim() != issueReport.LocationDescription?.Trim())
                    {
                        issueReport.LocationDescription = issueReportUpdateDto.NewLocationDescription.Trim();
                        changesWereMade = true;
                    }
                }
                //Change the reporter phone number
                if(issueReportUpdateDto.NewPhoneNumber != null)
                {
                    if(issueReportUpdateDto.NewPhoneNumber.Trim() == "" && (issueReport.ReporterPhoneNumber != null && issueReport.ReporterPhoneNumber?.Trim() != ""))
                    {
                        issueReport.ReporterPhoneNumber = null;
                        changesWereMade = true;
                    }
                    else if(issueReportUpdateDto.NewPhoneNumber.Trim() != issueReport.ReporterPhoneNumber?.Trim())
                    {
                        issueReport.ReporterPhoneNumber = issueReportUpdateDto.NewPhoneNumber.Trim();
                        changesWereMade = true;
                    }
                }
                //Change the report subject
                if(issueReportUpdateDto.NewSubject != null)
                {
                    if(issueReportUpdateDto.NewSubject.Trim() == "" && (issueReport.Subject!= null && issueReport.Subject?.Trim() != ""))
                    {
                        issueReport.Subject = null;
                        changesWereMade = true;
                    }
                    else if(issueReportUpdateDto.NewSubject.Trim() != issueReport.Subject?.Trim())
                    {
                        issueReport.Subject = issueReportUpdateDto.NewSubject.Trim();
                        changesWereMade = true;
                    }
                }
                //If there was no changes made
                if(!changesWereMade)
                {
                    var response = Ok(new{Result = "No changes were made"});
                    await _activityRepository.SetActivityStatus(activityId, SUCCESS_CODE, response);
                    return response;
                }

                if(await _issueReportingRepository.SaveAllAsync())
                {
                    var newIssueReport = new {
                        Id = issueReport.Id,
                        AppUserId = currentUser.Id,
                        AppUserName = currentUser.UserName,
                        Subject = issueReport.Subject,
                        IssueType = newIssueTypeName == null ? issueReport.IssueType?.Name : newIssueTypeName,
                        Description = issueReport.Description,
                        LocationDescription = issueReport.LocationDescription,
                        District = newDistrictName == null ? issueReport.District?.Name : newDistrictName,
                        DateReported = issueReport.DateReported,
                        Platform = issueReport.Platform,
                        Status = issueReport.IssueStatus?.CurrentStatus?.Name,
                        StatusId = issueReport.IssueStatus?.Id,
                        issueReport.ReporterAddress,
                        issueReport.ReporterEmail,
                        issueReport.ReporterPhoneNumber
                    };
                    var response = Ok(newIssueReport);
                    await _activityRepository.SetActivityStatus(activityId, SUCCESS_CODE, response);
                    return response;
                }

                var errored = BadRequest("Failed to edit issue report");
                await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, errored);
                return errored;
            }
            catch (System.Exception ex)
            {
                await _activityRepository.SetActivityStatus(activityId, FAILURE_CODE, ex);
                throw;
            }
        }

        /// <summary>
        /// Helper function to delete an issue status if it is considered empty and unmodified
        /// </summary>
        /// <param name="issueStatus">The issue status to check</param>
        /// <returns>Boolean result</returns>
        private bool deleteEmptyIssueStatus(IssueStatus issueStatus)
        {
            bool wasDeleted = false;
            if(issueStatus.IssueReports.Count() == 0)
            {
                if((issueStatus.LocationDescription == null || issueStatus.LocationDescription?.Trim() == "") &&
                    (issueStatus.Description == null || issueStatus.Description?.Trim() == "") && 
                    // (issueStatus.Remarks == null || issueStatus.Remarks?.Count() == 0) &&
                    (issueStatus.StatusUpdates == null || issueStatus.StatusUpdates?.Count() == 0)
                    )
                {
                    _issueReportingRepository.DeleteIssueStatus(issueStatus);
                    wasDeleted = true;
                }
            }
            return wasDeleted;
        }

        [HttpPost("issue-status-pin")]
        [ActionName("Pin Issue Statuses")]
        public async Task<ActionResult> PinIssueStatuses(List<int> issueStatusIds)
        {
            //Get the User
            var userId = User.GetUserId();
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user == null ){
                return Unauthorized("This user does not exist");
            }
            var userRole = User.GetUserRole();

            //Start Logging Activity
            var activityId = await _activityRepository.LogActivity("Pin Issues", CONTROLLER_NAME, issueStatusIds, user);

            try
            {
                var issueStatuses = await _issueReportingRepository.AsyncGetIssueStatuses();
                var filteredIssueReportIds = issueStatuses.Where(i => issueStatusIds.Contains(i.Id)).Select(i => i.Id);

                if(filteredIssueReportIds.Count() == 0)
                {
                    var returnValue = NotFound("No issues were found.");
                    //
                    return returnValue;
                }
                
                //Add Pins
                var userPins = user.UserPins.Select(u => u.IssueStatusId);

                //Check if any of the pins
                var existingPins = userPins.Intersect(filteredIssueReportIds);

                if(existingPins.Count() == 0)
                {
                    var newPins = filteredIssueReportIds.Select(i => new UserPin{AppUserId = user.Id, IssueStatusId = i});
                    foreach (var pin in newPins)
                    {
                        user.UserPins.Add(pin);
                    }

                    var saveResult = await _userRepository.SaveAllAsync();
                    if(saveResult)
                    {
                        var successResult = Ok(new {Result = "Successfully pinned selected issues.", Unpin = false, Values = newPins.ToList()});
                        //
                        return successResult;
                    }

                    var error = BadRequest("An unexpected error occured when pinning issues.");
                    //
                    return error;
                }
                
                var pins = existingPins.ToList();
                foreach (var pin in existingPins)
                {
                    user.UserPins.Remove(user.UserPins.SingleOrDefault(i => i.IssueStatusId == pin));
                }

                var result = await _userRepository.SaveAllAsync();
                if(result)
                {
                    var returnValue = Ok(new {Result = "Successfully unpinned selected issues.", Unpin = true, Values = pins});
                    //
                    return returnValue;
                }

                var errorResult = BadRequest("An unexpected error occured when unpinning issues.");
                //
                return errorResult;
            }
            catch (System.Exception ex)
            {
                await _activityRepository.SetActivityStatus(activityId, FAILURE_CODE, ex);
                throw;
            }
        }

        [HttpPost("issue-status-hide")]
        [ActionName("Hide Issue Statuses")]
        public async Task<ActionResult> HideIssueStatus(List<int> issueStatusIds)
        {
            //Get the User
            var userId = User.GetUserId();
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user == null ){
                return Unauthorized("This user does not exist");
            }
            var userRole = User.GetUserRole();

            //Start Logging Activity - Note, only logging this function in the event of failures
            //var activityId = await _activityRepository.LogActivity("Pin Issues", CONTROLLER_NAME, issueStatusIds, user);

            try
            {
                var issueStatuses = await _issueReportingRepository.AsyncGetIssueStatuses();
                var filteredIssueReportIds = issueStatuses.Where(i => issueStatusIds.Contains(i.Id)).Select(i => i.Id);

                if(filteredIssueReportIds.Count() == 0)
                {
                    var returnValue = NotFound("No issues were found.");
                    //
                    return returnValue;
                }

                //Hide Items
                var userHidden = user.UserHiddenItems.Select(u => u.IssueStatusId);

                //Check if any of the selected Issue reports are already hidden
                var existing = userHidden.Intersect(filteredIssueReportIds);

                if(existing.Count() == 0)
                {
                    var newHides = filteredIssueReportIds.Select(i => new UserHiddenItem{AppUserId = user.Id, IssueStatusId = i}).ToList();
                    foreach (var item in newHides)
                    {
                        user.UserHiddenItems.Add(item);
                    }

                    var saveResult = await _userRepository.SaveAllAsync();
                    if(saveResult)
                    {
                        var successResult = Ok(new {Result = "Successfully hid selected issues.", Unhide = false, Values = newHides.Select(i => i.IssueStatusId).ToList()});
                        //
                        return successResult;
                    }

                    var error = BadRequest("An unexpected error occured when hiding issues.");
                    //
                    return error;
                }
                
                var hiddenList = existing.ToList();
                foreach (var item in existing)
                {
                    user.UserHiddenItems.Remove(user.UserHiddenItems.SingleOrDefault(i => i.IssueStatusId == item));
                }

                var result = await _userRepository.SaveAllAsync();
                if(result)
                {
                    var returnValue = Ok(new {Result = "Successfully unhid selected issues.", Unhide = true, Values = hiddenList});
                    //
                    return returnValue;
                }

                var errorResult = BadRequest("An unexpected error occured when unhiding issues.");
                //
                return errorResult;              
            }
            catch (System.Exception ex)
            {
                var activityId = await _activityRepository.LogActivity("Pin Issues", CONTROLLER_NAME, issueStatusIds, user);
                await _activityRepository.SetActivityStatus(activityId, FAILURE_CODE, ex);
                throw;
            }
        }

        // [Authorize(Policy = "RequireDataManagerRole")]
        [HttpGet("export-sheet")]
        public async Task<ActionResult> ExportSheet([FromQuery] ExportOptionsDto exportOptionsDto)
        {
            var currentUser = await _userRepository.GetUserByIdAsync(User.GetUserId());
            if(currentUser == null ){
                return Unauthorized("This user does not exist");
            }

            //Set params
            if(exportOptionsDto.IssueTypes != null)
                exportOptionsDto.IssueTypeList = exportOptionsDto.IssueTypes.Split(",");

            if(exportOptionsDto.Districts != null)
                exportOptionsDto.DistrictList = exportOptionsDto.Districts.Split(",");

            if(exportOptionsDto.Statuses != null)
                exportOptionsDto.StatusList = exportOptionsDto.Statuses.Split(",");

            var currentUserRole = User.GetUserRole();
            if(RolesHelper.RoleAllowsRestriction(currentUserRole)){
                var issueTypeAccess = currentUser.UserIssueTypes.Select(i => i.IssueType?.Name).ToList();
                var districtAccess = currentUser.UserDistricts.Select(i => i.District?.Name).ToList();
                if(exportOptionsDto.IssueTypeList.Length > 0){
                    if(issueTypeAccess.Count() > 0 ){
                        if(exportOptionsDto.IssueTypeList.Any(i => !exportOptionsDto.IssueTypeList.Contains(i))){
                            return Unauthorized("Unathorized access to the issue type(s) specified");
                        }
                    }
                }else{
                    exportOptionsDto.IssueTypeList = issueTypeAccess.ToArray();
                }
                if(exportOptionsDto.DistrictList.Length > 0){
                    if(issueTypeAccess.Count() > 0 ){
                        if(exportOptionsDto.DistrictList.Any(i => !exportOptionsDto.DistrictList.Contains(i))){
                            return Unauthorized("Unathorized access to the district(s) specified");
                        }
                    }
                }else{
                    exportOptionsDto.DistrictList = districtAccess.ToArray();
                }
            }

            string fileName = "";
            FileStreamResult excelFile = null;

            //Create Excel Workbook
            var workbook = new XSSFWorkbook();
            NPOI.POIXMLProperties propertiesInfo = workbook.GetProperties();
            NPOI.CoreProperties corePropertiesInfo  = propertiesInfo.CoreProperties;
            corePropertiesInfo.Creator = "Website";
            
            //Create Excel Worksheet
            var sheet = (XSSFSheet)workbook.CreateSheet("Report");
            IDataFormat dataFormatCustom = workbook.CreateDataFormat();

            //Label Font
            var font1 = workbook.CreateFont();
            font1.FontHeightInPoints = 11;
            font1.FontName = "Arial";
            font1.Color = IndexedColors.Grey40Percent.Index;
            var labelStyle = workbook.CreateCellStyle();
            labelStyle.SetFont(font1);

            //Date Font
            var font2 = workbook.CreateFont();
            font2.FontHeightInPoints = 13;
            font2.FontName = "Arial";
            font2.Color = IndexedColors.Grey40Percent.Index;
            var dateStyle = workbook.CreateCellStyle();
            dateStyle.SetFont(font2);

            //Title Font
            var font3 = workbook.CreateFont();
            font3.FontHeightInPoints = 24;
            font3.FontName = "Arial";
            font3.Color = IndexedColors.Grey50Percent.Index;
            var titleStyle = workbook.CreateCellStyle();
            titleStyle.SetFont(font3);

            //Title
            var titleRow = sheet.CreateRow(0);
            var cell = titleRow.CreateCell(0);
            cell.SetCellValue("TITLE: ");
            cell.CellStyle = labelStyle;
                
            //Date
            var row = sheet.CreateRow(1);
            cell = row.CreateCell(0);
            cell.SetCellValue("DATE: ");
            cell.CellStyle = labelStyle;

            cell = row.CreateCell(1);
            cell.SetCellValue(DateTime.Now);
            cell.CellStyle = dateStyle;
            cell.CellStyle.DataFormat = dataFormatCustom.GetFormat("dddd, d mmmm yyyy");
            cell.CellStyle.Alignment = HorizontalAlignment.Left;
            var region = sheet.AddMergedRegion(new CellRangeAddress(cell.RowIndex, cell.RowIndex, cell.ColumnIndex, cell.ColumnIndex + 2));

            //Conditional Formatting
            XSSFSheetConditionalFormatting sCF = (XSSFSheetConditionalFormatting)sheet.SheetConditionalFormatting;  

            //Fill Green if Passing Score
            XSSFConditionalFormattingRule cfOpen = 
                (XSSFConditionalFormattingRule)sCF.CreateConditionalFormattingRule(ComparisonOperator.Equal, "\"Open\"");
            XSSFPatternFormatting fillGreen = (XSSFPatternFormatting)cfOpen.CreatePatternFormatting();
            fillGreen.FillBackgroundColor = IndexedColors.LightGreen.Index;
            fillGreen.FillPattern = FillPattern.SolidForeground;

            //Fill Red if Passing Score
            XSSFConditionalFormattingRule cfClosed =
                (XSSFConditionalFormattingRule)sCF.CreateConditionalFormattingRule(ComparisonOperator.Equal, "\"Closed\"");
            XSSFPatternFormatting fillYellow = (XSSFPatternFormatting)cfClosed.CreatePatternFormatting();
            fillYellow.FillBackgroundColor = IndexedColors.Tan.Index;
            fillYellow.FillPattern = FillPattern.SolidForeground;

            //Issue Reports Report
            if(exportOptionsDto.ExportType == "issueReport"){
                var issueReports = await _issueReportingRepository.AsyncGetIssueReports(new IssueReportStatusParams {
                    IssueTypeAccess = exportOptionsDto.IssueTypeList,
                    DistrictAccess = exportOptionsDto.DistrictList,
                    StatusList = exportOptionsDto.StatusList,
                    dateUpper = exportOptionsDto.dateUpper,
                    dateLower = exportOptionsDto.dateLower
                });

                if(issueReports.Count() == 0)
                    return BadRequest("No issue reports to display for the specified options");

                var issueReportsData = issueReports.Select(i => new {
                    ReportId = i.Id,
                    IssueType = i.IssueType,
                    Subject = i.Subject,
                    Description = i.Description,
                    District = i.District != null ? i.District : "None",
                    LocationDescription = i.LocationDescription,
                    LocationLongitude = i.LocationLongitude,
                    LocationLatitude = i.LocationLatitude,
                    DateReported = i.DateReported,
                    ImageCount = i.ImageCount,
                    Platform = i.Platform,
                    MobileUserId = i.MobileUserId != null ? i.MobileUserId.ToString() : "N/A",
                    PhoneNumber = i.PhoneNumber,
                    Address = i.Address,
                    Email = i.Email,
                    SystemUserName = i.UserName,
                    IssueID = i.IssueStatusId,
                    IssueStatus = i.CurrentStatus,
                    IssueOpenClosedStatus = i.ClosedStatus ? "Closed" : "Open"
                });
                var issueReportsCount = issueReportsData.Count();

                cell = titleRow.CreateCell(1);
                cell.SetCellValue("Issue Report Details");
                cell.CellStyle = titleStyle;
                region = sheet.AddMergedRegion(new CellRangeAddress(cell.RowIndex, cell.RowIndex, cell.ColumnIndex, cell.ColumnIndex + 5));
                
                List<string> headingsList = new List<string> {"Report ID", "Issue Type", "Subject", "Description", "District", "Location Description", "Location Longitude", 
                    "Location Latitude", "Date Reported", "Image Count", "Platform", "MobileUser ID", "PhoneNumber", "Address", "Email", "SystemUserName", "Issue ID", 
                    "Issue Status", "Issue Open/Closed Status"
                };

                //Sheet Values
                foreach (var (issue, index) in issueReportsData.Select((n, i) => (n, i))){
                    row = sheet.CreateRow(4 + index);
                    cell = row.CreateCell(0);
                    cell.SetCellValue(issue.ReportId);
                    cell = row.CreateCell(1);
                    cell.SetCellValue(issue.IssueType);
                    cell = row.CreateCell(2);
                    cell.SetCellValue(issue.Subject );
                    cell = row.CreateCell(3);
                    cell.SetCellValue(issue.Description);
                    cell = row.CreateCell(4);
                    cell.SetCellValue(issue.District);
                    cell = row.CreateCell(5);
                    cell.SetCellValue(issue.LocationDescription);
                    cell = row.CreateCell(6);
                    cell.SetCellValue(issue.LocationLongitude);
                    cell = row.CreateCell(7);
                    cell.SetCellValue(issue.LocationLatitude);
                    cell = row.CreateCell(8);
                    if(issue.DateReported != null)
                    {
                        cell.SetCellValue((DateTime)issue.DateReported);
                        cell.CellStyle = workbook.CreateCellStyle();
                        cell.CellStyle.DataFormat = dataFormatCustom.GetFormat("dd/mm/yy");
                    }
                    else
                        cell.SetCellValue("");
                    cell = row.CreateCell(9);
                    cell.SetCellValue(issue.ImageCount);
                    cell = row.CreateCell(10);
                    cell.SetCellValue(issue.Platform);
                    cell = row.CreateCell(11);
                    cell.SetCellValue(issue.MobileUserId);
                    cell = row.CreateCell(12);
                    cell.SetCellValue(issue.PhoneNumber);
                    cell = row.CreateCell(13);
                    cell.SetCellValue(issue.Address);
                    cell = row.CreateCell(14);
                    cell.SetCellValue(issue.Email);
                    cell = row.CreateCell(15);
                    cell.SetCellValue(issue.SystemUserName);
                    cell = row.CreateCell(16);
                    cell.SetCellValue(issue.IssueID);
                    cell = row.CreateCell(17);
                    cell.SetCellValue(issue.IssueStatus);
                    cell = row.CreateCell(18);
                    cell.SetCellValue(issue.IssueOpenClosedStatus);
                }

                //Table
                var table = sheet.CreateTable();
                table.Name = "IssuesTable";
                var ctTable = table.GetCTTable();
                ctTable.id = 1;
                table.DisplayName = "IssuesTable";
                AreaReference tableDataRange = new AreaReference(new CellReference(3, 0), new CellReference(3 + issueReportsCount, 18));
                ctTable.@ref = tableDataRange.FormatAsString();
                ctTable.tableStyleInfo = new CT_TableStyleInfo();
                ctTable.tableStyleInfo.name = "TableStyleMedium3";
                ctTable.tableStyleInfo.showRowStripes = true;
                ctTable.tableColumns = new CT_TableColumns();
                ctTable.autoFilter = new CT_AutoFilter();
                ctTable.tableColumns.tableColumn = new List<CT_TableColumn>();
                
                //Table Header
                row = sheet.CreateRow(3);
                foreach (var (title, index) in headingsList.Select((n, i) => (n, i)))
                {
                    cell = row.CreateCell(index);
                    cell.SetCellValue(title);
                    sheet.AutoSizeColumn(index);

                    //slightly increase the column width
                    var colWidth = sheet.GetColumnWidth(index);
                    if (colWidth < 6000)
                     sheet.SetColumnWidth(index, colWidth + 700);

                    ctTable.tableColumns.tableColumn.Add(new CT_TableColumn() { id = Convert.ToUInt32(index) + 1, name = title.Replace(" ", "") });
                
                    if(title == "Issue Open/Closed Status")
                    {
                       var openClosedCol = ctTable.tableColumns.tableColumn.FirstOrDefault(i => i.id == index+1);
                       CellRangeAddress[] cfRange = { new CellRangeAddress(4, 3 + issueReportsCount, index, index) };
                       sCF.AddConditionalFormatting(cfRange, cfOpen, cfClosed);
                    }
                }

                fileName = "Issue Reports, " + DateTime.Now.ToString("yyyy'-'MM'-'dd', 'HH'-'mm'-'ss") + ".xlsx";

                // using (FileStream sw = System.IO.File.Create("C:\\Users\\tlake\\Desktop\\TextXL\\" + fileName))
                // {
                //     workbook.Write(sw);
                // }

                MemoryStream ms = new MemoryStream();
                using(MemoryStream tempStream = new MemoryStream())
                {
                    workbook.Write(tempStream);
                    var byteArray = tempStream.ToArray();
                    // ms.Write(byteArray, 0, byteArray.Length);
                    var content = new System.IO.MemoryStream(byteArray);
                    excelFile = File(content, "application/xlsx", fileName);
                }
            }
            
            //Issues Report
            if(exportOptionsDto.ExportType == "issueStatus"){
                var issueStatuses = await _issueReportingRepository.AsyncGetIssueReportStatuses(new IssueReportStatusParams {
                    IssueTypeAccess = exportOptionsDto.IssueTypeList,
                    DistrictAccess = exportOptionsDto.DistrictList,
                    StatusList = exportOptionsDto.StatusList,
                    dateUpper = exportOptionsDto.dateUpper,
                    dateLower = exportOptionsDto.dateLower
                });

                if(issueStatuses.Count() == 0)
                    return BadRequest("No issues to display for the specified options");

                var issueStatusesData = issueStatuses.Select(i => new {
                    IssueID = i.Id,
                    IssueType = i.IssueType,
                    IssueDescription = i.Description != null ? i.Description : "None",
                    District = i.District != null ? i.District : "None",
                    IssueLocation = i.LocationDescription != null ? i.LocationDescription : "None",
                    DateFirstReported = i.DateReported,
                    DateRecentlyReported = i.DateLastReported,
                    DateLastUpdated = i.DateUpdated,
                    NumberOfIssueReports = i.IssueReportCount,
                    CurrentIssueStatus = i.Status,
                    PreviousIssueStatus = i.PreviousStatus != null ? i.PreviousStatus : "N/A",
                    NumberOfStatusUpdates = i.StatusUpdateCount,
                    OpenClosedStatus = i.ClosedStatus ? "Closed" : "Open"
                });
                var issueStatusCount = issueStatusesData.Count();
                
                //Create Excel Worksheet
                // var sheet = (XSSFSheet)workbook.CreateSheet("Issues");
                // IDataFormat dataFormatCustom = workbook.CreateDataFormat();
                
                cell = titleRow.CreateCell(1);
                cell.SetCellValue("Issue Details");
                cell.CellStyle = titleStyle;
                region = sheet.AddMergedRegion(new CellRangeAddress(cell.RowIndex, cell.RowIndex, cell.ColumnIndex, cell.ColumnIndex + 5));

                List<string> headingsList = new List<string> {"Issue ID", "Issue Type", "Issue Description", "District", "Issue Location", "Date First Reported", 
                    "Date Recently Reported", "Date Last Updated", "Number of Issue Reports", "Current Issue Status", "Previous Issue Status", 
                    "Number of Status Updates", "Open/Closed Status"};

                //Sheet Values
                foreach (var (issue, index) in issueStatusesData.Select((n, i) => (n, i))){
                    row = sheet.CreateRow(4 + index);
                    cell = row.CreateCell(0);
                    cell.SetCellValue(issue.IssueID);
                    cell = row.CreateCell(1);
                    cell.SetCellValue(issue.IssueType);
                    cell = row.CreateCell(2);
                    cell.SetCellValue(issue.IssueDescription );
                    cell = row.CreateCell(3);
                    cell.SetCellValue(issue.District);
                    cell = row.CreateCell(4);
                    cell.SetCellValue(issue.IssueLocation);
                    cell = row.CreateCell(5);
                    if(issue.DateFirstReported != null)
                    {
                        cell.SetCellValue((DateTime)issue.DateFirstReported);
                        cell.CellStyle = workbook.CreateCellStyle();
                        cell.CellStyle.DataFormat = dataFormatCustom.GetFormat("dd/mm/yy");
                    }
                    else
                        cell.SetCellValue("");
                    cell = row.CreateCell(6);
                    if(issue.DateRecentlyReported != null)
                    {
                        cell.SetCellValue((DateTime)issue.DateRecentlyReported);
                        cell.CellStyle = workbook.CreateCellStyle();
                        cell.CellStyle.DataFormat = dataFormatCustom.GetFormat("dd/mm/yy");
                    }
                    cell = row.CreateCell(7);
                    if(issue.DateFirstReported != null)
                    {
                        cell.SetCellValue((DateTime)issue.DateLastUpdated);
                        cell.CellStyle = workbook.CreateCellStyle();
                        cell.CellStyle.DataFormat = dataFormatCustom.GetFormat("dd/mm/yy");
                    }
                    else
                        cell.SetCellValue("");
                    cell = row.CreateCell(8);
                    cell.SetCellValue(issue.NumberOfIssueReports);
                    cell = row.CreateCell(9);
                    cell.SetCellValue(issue.CurrentIssueStatus);
                    cell = row.CreateCell(10);
                    cell.SetCellValue(issue.PreviousIssueStatus);
                    cell = row.CreateCell(11);
                    cell.SetCellValue(issue.NumberOfStatusUpdates);
                    cell = row.CreateCell(12);
                    cell.SetCellValue(issue.OpenClosedStatus);
                }

                //Table
                var table = sheet.CreateTable();
                table.Name = "IssuesTable";
                var ctTable = table.GetCTTable();
                ctTable.id = 1;
                table.DisplayName = "IssuesTable";
                AreaReference tableDataRange = new AreaReference(new CellReference(3, 0), new CellReference(3 + issueStatusCount, 12));
                ctTable.@ref = tableDataRange.FormatAsString();
                ctTable.tableStyleInfo = new CT_TableStyleInfo();
                ctTable.tableStyleInfo.name = "TableStyleMedium2";
                ctTable.tableStyleInfo.showRowStripes = true;
                ctTable.tableColumns = new CT_TableColumns();
                ctTable.autoFilter = new CT_AutoFilter();
                ctTable.tableColumns.tableColumn = new List<CT_TableColumn>();
                
                row = sheet.CreateRow(3);
                foreach (var (title, index) in headingsList.Select((n, i) => (n, i)))
                {
                    cell = row.CreateCell(index);
                    cell.SetCellValue(title);
                    sheet.AutoSizeColumn(index);

                    //slightly increase the column width
                    var colWidth = sheet.GetColumnWidth(index);
                    if (colWidth < 6000)
                        sheet.SetColumnWidth(index, colWidth + 700);

                    ctTable.tableColumns.tableColumn.Add(new CT_TableColumn() { id = Convert.ToUInt32(index) + 1, name = title.Replace(" ", "") });
                
                    if(title == "Open/Closed Status")
                    {
                       var openClosedCol = ctTable.tableColumns.tableColumn.FirstOrDefault(i => i.id == index+1);
                       CellRangeAddress[] cfRange = { new CellRangeAddress(4, 3 + issueStatusCount, index, index) };
                       sCF.AddConditionalFormatting(cfRange, cfOpen, cfClosed);
                    }
                }

                // using (FileStream sw = System.IO.File.Create("C:\\Users\\tlake\\Desktop\\TextXL\\Issues, " + DateTime.Now.ToString("yyyy'-'MM'-'dd', 'HH'-'mm'-'ss") + ".xlsx"))
                // {
                //     workbook.Write(sw);
                // }

                fileName = "Issues, " + DateTime.Now.ToString("yyyy'-'MM'-'dd', 'HH'-'mm'-'ss") + ".xlsx";
                MemoryStream ms = new MemoryStream();
                using(MemoryStream tempStream = new MemoryStream())
                {
                    workbook.Write(tempStream);
                    var byteArray = tempStream.ToArray();
                    // ms.Write(byteArray, 0, byteArray.Length);
                    var content = new System.IO.MemoryStream(byteArray);
                    excelFile = File(content, "application/xlsx", fileName);
                }
            }
            
            //Issue Status Updates Report
            if(exportOptionsDto.ExportType == "statusUpdate"){
                if(exportOptionsDto.StatusId == null)
                {
                    return BadRequest("No status id provided");
                }
                var statusUpdates = await _issueReportingRepository.AsyncGetIssueStatusUpdates((int)exportOptionsDto.StatusId);

                if(statusUpdates.Count() == 0)
                    return BadRequest("No status updates to display for the selected issue");

                var statusUpdateData = statusUpdates.Select(i => new {
                    StatusId = i.StatusId,
                    DateReported = i.DateReported,
                    Date = i.Date,
                    Status = i.Status,
                    PreviousStatus = i.PreviousStatus == null ? "N/A" : i.PreviousStatus,
                    UpdateDetails = GetStatusUpdateDetails(i),
                    ImagesCount = i.Images.Count()
                });
                var statusUpdateCount = statusUpdateData.Count();
                
                cell = titleRow.CreateCell(1);
                cell.SetCellValue("Status Update Details (Issue ID: " + exportOptionsDto.StatusId + ")");
                cell.CellStyle = titleStyle;
                region = sheet.AddMergedRegion(new CellRangeAddress(cell.RowIndex, cell.RowIndex, cell.ColumnIndex, cell.ColumnIndex + 5));

                List<string> headingsList = new List<string> {
                    "Status ID", 
                    "Date Entered", 
                    "Actual Date of Update", 
                    "New Status", 
                    "Previous Status", 
                    "Update Details", 
                    "No. of Images", 
                };

                //Sheet Values
                foreach (var (item, index) in statusUpdateData.Select((n, i) => (n, i))){
                    row = sheet.CreateRow(4 + index);
                    cell = row.CreateCell(0);
                    cell.SetCellValue(item.StatusId);
                    cell = row.CreateCell(1);
                    cell.SetCellValue(item.DateReported);
                    cell.CellStyle = workbook.CreateCellStyle();
                    cell.CellStyle.DataFormat = dataFormatCustom.GetFormat("dd/mm/yy");
                    cell = row.CreateCell(2);
                    cell.SetCellValue(item.Date);
                    cell.CellStyle = workbook.CreateCellStyle();
                    cell.CellStyle.DataFormat = dataFormatCustom.GetFormat("dd/mm/yy");
                    cell = row.CreateCell(3);
                    cell.SetCellValue(item.Status);
                    cell = row.CreateCell(4);
                    cell.SetCellValue(item.PreviousStatus);
                    cell = row.CreateCell(5);
                    cell.SetCellValue(item.UpdateDetails);
                    cell.CellStyle = workbook.CreateCellStyle();
                    cell.CellStyle.WrapText = true;
                    cell.CellStyle.VerticalAlignment = VerticalAlignment.Top;
                    cell = row.CreateCell(6);
                    cell.SetCellValue(item.ImagesCount);
                }

                //Table
                var table = sheet.CreateTable();
                table.Name = "IssuesTable";
                var ctTable = table.GetCTTable();
                ctTable.id = 1;
                table.DisplayName = "IssuesTable";
                AreaReference tableDataRange = new AreaReference(new CellReference(3, 0), new CellReference(3 + statusUpdateCount, 6));
                ctTable.@ref = tableDataRange.FormatAsString();
                ctTable.tableStyleInfo = new CT_TableStyleInfo();
                ctTable.tableStyleInfo.name = "TableStyleMedium7";
                ctTable.tableStyleInfo.showRowStripes = true;
                ctTable.tableColumns = new CT_TableColumns();
                ctTable.autoFilter = new CT_AutoFilter();
                ctTable.tableColumns.tableColumn = new List<CT_TableColumn>();

                //Table Header
                row = sheet.CreateRow(3);
                foreach (var (title, index) in headingsList.Select((n, i) => (n, i)))
                {
                    cell = row.CreateCell(index);
                    cell.SetCellValue(title);
                    sheet.AutoSizeColumn(index);

                    //slightly increase the column width
                    var colWidth = sheet.GetColumnWidth(index);
                    if (colWidth < 6000)
                        sheet.SetColumnWidth(index, colWidth + 700);

                    ctTable.tableColumns.tableColumn.Add(new CT_TableColumn() { id = Convert.ToUInt32(index) + 1, name = title.Replace(" ", "") });
                }
            
                // using (FileStream sw = System.IO.File.Create("C:\\Users\\tlake\\Desktop\\TextXL\\Status Updates, " + DateTime.Now.ToString("yyyy'-'MM'-'dd', 'HH'-'mm'-'ss") + ".xlsx"))
                // {
                //     workbook.Write(sw);
                // }

                fileName = "Status Updates " + DateTime.Now.ToString("yyyy'-'MM'-'dd' 'HH'-'mm'-'ss") + ".xlsx";
                // MemoryStream content = new MemoryStream();
                // workbook.Write(content, true);
                // excelFile = File(content, "application/xlsx", fileName);
                // return excelFile;

                MemoryStream ms = new MemoryStream();
                using(MemoryStream tempStream = new MemoryStream())
                {
                    workbook.Write(tempStream);
                    var byteArray = tempStream.ToArray();
                    // ms.Write(byteArray, 0, byteArray.Length);
                    var content = new System.IO.MemoryStream(byteArray);
                    excelFile = File(content, "application/xlsx", fileName);
                }
            }

            return excelFile;
        }

        private string GetStatusUpdateDetails(IssueStatusUpdateDto statusUpdate)
        {
            string details = "";
            if(statusUpdate.Status == "Under Review")
            {   
                if(statusUpdate.ResponsibleUnit != null && statusUpdate.ResponsibleUnit.Trim()!= "")
                {
                    details += "Reviewer: " + statusUpdate.ResponsibleUnit + "\n";
                }
                if(statusUpdate.StatusUpdateDetails != null && statusUpdate.StatusUpdateDetails.Trim()!= "")
                {
                    details += "Additional Details: " + statusUpdate.StatusUpdateDetails + "\n";
                }
            }
            else if(statusUpdate.Status == "Under Investigation")
            {
                if(statusUpdate.ResponsibleUnit != null && statusUpdate.ResponsibleUnit.Trim()!= "")
                {
                    details += "Investigator: " + statusUpdate.ResponsibleUnit + "\n";
                }
                if(statusUpdate.StatusUpdateDetails != null && statusUpdate.StatusUpdateDetails.Trim()!= "")
                {
                    details += "Additional Details: " + statusUpdate.StatusUpdateDetails + "\n";
                }
            }
            else if(statusUpdate.Status == "Pending Approval")
            {
                if(statusUpdate.ApprovalItems != null && statusUpdate.ApprovalItems.Count() != 0)
                {
                    details += "Awaiting Approval for: \n" + String.Join("\n", statusUpdate.ApprovalItems.Select((item, index) => (index + 1).ToString() + ") " + item).ToArray());
                }
                if(statusUpdate.StatusUpdateDetails != null && statusUpdate.StatusUpdateDetails.Trim()!= "")
                {
                    details += "Approval Details: " + statusUpdate.StatusUpdateDetails + "\n";
                }
            }
            else if(statusUpdate.Status == "In Progress")
            {
                if(statusUpdate.StatusUpdateDetails != null && statusUpdate.StatusUpdateDetails.Trim()!= "")
                {
                    details += "Details: " + statusUpdate.StatusUpdateDetails + "\n";
                }
                if(statusUpdate.WorkType != null && statusUpdate.WorkType.Trim()!= "")
                {
                    details += "Contract/Work Type: " + statusUpdate.WorkType + "\n";
                }
                if(statusUpdate.ResponsibleUnit != null && statusUpdate.ResponsibleUnit.Trim() != "")
                {
                    details += "Unit/Division/Contractor: " + statusUpdate.ResponsibleUnit + "\n";
                }
            }
            else if(statusUpdate.Status == "Paused/On Hold")
            {
                if(statusUpdate.ReasonDetails != null && statusUpdate.ReasonDetails.Trim()!= "")
                {
                    details += "Reason for Pause: " + statusUpdate.ReasonDetails + "\n";
                }
                if(statusUpdate.StatusUpdateDetails != null && statusUpdate.StatusUpdateDetails.Trim()!= "")
                {
                    details += "Additional Details: " + statusUpdate.StatusUpdateDetails + "\n";
                }
            }
            else if(statusUpdate.Status == "Cancelled")
            {
                if(statusUpdate.ReasonDetails != null && statusUpdate.ReasonDetails.Trim()!= "")
                {
                    details += "Reason for Cancellation: " + statusUpdate.ReasonDetails + "\n";
                }
                if(statusUpdate.StatusUpdateDetails != null && statusUpdate.StatusUpdateDetails.Trim()!= "")
                {
                    details += "Details of Cancellation: " + statusUpdate.StatusUpdateDetails + "\n";
                }
            }
            else if(statusUpdate.Status == "Not Within Remit")
            {
                if(statusUpdate.ReasonDetails != null && statusUpdate.ReasonDetails.Trim()!= "")
                {
                    details += "Reason for Change: " + statusUpdate.ReasonDetails + "\n";
                }
                if(statusUpdate.NewUnit != null && statusUpdate.NewUnit.Trim()!= "")
                {
                    details += "Agency Responsible: " + statusUpdate.NewUnit + "\n";
                }
                if(statusUpdate.StatusUpdateDetails != null && statusUpdate.StatusUpdateDetails.Trim()!= "")
                {
                    details += "Details of Change: " + statusUpdate.StatusUpdateDetails + "\n";
                }
            }
            else if(statusUpdate.Status == "Completed")
            {
                if(statusUpdate.StatusUpdateDetails != null && statusUpdate.StatusUpdateDetails.Trim()!= "")
                {
                    details += "Details of Completion: " + statusUpdate.StatusUpdateDetails + "\n";
                }
            }
            return details != "" ? details.Trim() : "None" ;
        }

        [HttpGet("summary")]
        public async Task<ActionResult> GetSummary() 
        {
            var userId = User.GetUserId();
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user == null ){
                return Unauthorized("This user does not exist");
            }
            var userRole = User.GetUserRole();
            
            var issueStatusParams = new IssueReportStatusParams();

            //If the user is a default/IM user, filter issue types that they dont have access to
            if(RolesHelper.RoleAllowsRestriction(userRole))
            {
                //If the user issue types is empty, no filterering by issue types is done
                var userIssueTypes = user.UserIssueTypes.Select(ut => ut.IssueType.Name).ToArray();
                if(userIssueTypes.Length > 0)
                {
                    issueStatusParams.IssueTypeAccess = userIssueTypes;
                }
                else
                {
                    issueStatusParams.IssueTypeAccess = new string[] {};
                }

                //If the user diustricts is empty, no filterering by districts is done
                var userDistricts = user.UserDistricts.Select(ut => ut.District.Name).ToArray();
                if(userDistricts.Length > 0)
                {
                    issueStatusParams.DistrictAccess = userDistricts;
                }
                else
                {
                    issueStatusParams.DistrictAccess = new string[] {};
                }
            }
            else
            {
                issueStatusParams.IssueTypeAccess = new string[] {};
                issueStatusParams.DistrictAccess = new string[] {};
            }
        
            //Get Recently Added
            var recentlyAddedParams = (IssueReportStatusParams)issueStatusParams.Clone();
            recentlyAddedParams.SortBy = "DateReported";
            recentlyAddedParams.PinnedOnTop = false;
            recentlyAddedParams.OnlyNew = true;
            recentlyAddedParams.dateLower = DateTime.Now.AddDays(-14);
            var recentlyAddedIssues = await _issueReportingRepository.AsyncGetFilteredIssueStatuses(recentlyAddedParams, user);
            
            var recentlyUpdatedParams = (IssueReportStatusParams)issueStatusParams.Clone();
            recentlyUpdatedParams.SortBy = "DateUpdated";
            recentlyUpdatedParams.PinnedOnTop = false;
            recentlyUpdatedParams.ShowNew = false;
            recentlyUpdatedParams.dateLower = DateTime.Now.AddDays(-14);
            var recentlyUpdatedIssues = await _issueReportingRepository.AsyncGetFilteredIssueStatuses(recentlyUpdatedParams, user);
            
            var pinnedIssuesParams = (IssueReportStatusParams)issueStatusParams.Clone();
            pinnedIssuesParams.SortBy = "DateUpdated";
            pinnedIssuesParams.PinnedOnTop = true;
            pinnedIssuesParams.PinnedOnly = true;
            var pinnedIssues = await _issueReportingRepository.AsyncGetFilteredIssueStatuses(pinnedIssuesParams, user, 5);

            var issueTypeTotals = await _issueReportingRepository.AsyncGetIssueStatusTotals(issueStatusParams.IssueTypeAccess, issueStatusParams.DistrictAccess, "Issue Type");
            var districtTotals = await _issueReportingRepository.AsyncGetIssueStatusTotals(issueStatusParams.IssueTypeAccess, issueStatusParams.DistrictAccess, "District");

            var returnValues = new {
                RecentAdditionsCount = recentlyAddedIssues.Item2,
                RecentAdditions = recentlyAddedIssues.Item1,
                RecentUpdatesCount = recentlyUpdatedIssues.Item2,
                RecentUpdates = recentlyUpdatedIssues.Item1,
                PinnedIssuesCount = pinnedIssues.Item2,
                PinnedIssues = pinnedIssues.Item1,
                IssueTypeTotals = issueTypeTotals,
                DistrictTotals = districtTotals
            };

            return Ok(returnValues);
        }
    }
}