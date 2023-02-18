using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller responsible for handling data retrieval requests
/// </summary>
namespace API.Controllers
{
    [Authorize]
    public class DataController : BaseApiContoller
    {
        private readonly IIssueTypeRepository _issueTypeRepository;
        private readonly IUserRepository _userRepository;
        private readonly IStatusRepository _statusRepository;
        private readonly IDistrictRepository _districtRepository;
        private readonly IIssueReportingRepository _issueReportingRepository;
        
        /// <summary>
        /// Constructor for Data controller
        /// </summary>
        /// <param name="issueTypeRepository">Access to the issue type repository service</param>
        /// <param name="districtRepository">Access to the district repository service</param>
        /// <param name="issueReportingRepository">Access to the issue report repository service</param>
        /// <param name="userRepository">Access to the user repository service</param>
        /// <param name="statusRepository">Access to the status repository service</param>
        public DataController(IIssueTypeRepository issueTypeRepository,
                              IDistrictRepository districtRepository,
                              IIssueReportingRepository issueReportingRepository,
                              IUserRepository userRepository,
                              IStatusRepository statusRepository)
        {
            _userRepository = userRepository;
            _statusRepository = statusRepository;
            _districtRepository = districtRepository;
            _issueReportingRepository = issueReportingRepository;
            _issueTypeRepository = issueTypeRepository;
        }

        /// <summary>
        /// Route to get all the issue types
        /// </summary>
        /// <param name="filterByRole">Boolean parameter to specify wether to restrict the issue types by user access or not</param>
        /// <returns>All issue types</returns>
        [HttpGet("issue-types")]
        public async Task<ActionResult<IEnumerable<String>>> GetIssueTypes([FromQuery]bool filterByRole = false)
        {
            var currentUser = await _userRepository.GetUserByIdAsync(User.GetUserId());
            if(currentUser == null ){
                return Unauthorized("This user does not exist");
            }

            var userRole = User.GetUserRole();
            if(RolesHelper.RoleAllowsRestriction(userRole))
            {
                if(filterByRole)
                {
                    var user = await _userRepository.GetUserByUsernameAsync(User.GetUserName());
                    var userIssueTypes = user.UserIssueTypes.Select(ut => ut.IssueType).OrderBy(d => d.Name).Select(d => d.Name).ToList();
                    if(userIssueTypes.Count() > 0)
                        return userIssueTypes; 
                }
            }

            var issueTypes = await _issueTypeRepository.GetIssueTypes();
            return issueTypes.OrderBy(d => d.Name).Select(d => d.Name).ToList();
        }

        /// <summary>
        /// Route to get all districts
        /// </summary>
        /// <param name="filterByRole">Boolean parameter to specify wether to restrict the districts by user access or not</param>
        /// <returns>All districts</returns>
        [HttpGet("districts")]
        public async Task<ActionResult<IEnumerable<String>>> GetDistricts([FromQuery]bool filterByRole = false)
        {
            var userId = User.GetUserId();
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user == null ){
                return Unauthorized("This user does not exist");
            }

            var userRole = User.GetUserRole();
            if(RolesHelper.RoleAllowsRestriction(userRole))
            {
                if(filterByRole)
                {
                    var userDistricts = user.UserDistricts.Select(ut => ut.District).OrderBy(d => d.Name).Select(d => d.Name).ToList();
                    if(userDistricts.Count() > 0)
                        return userDistricts;
                }
            }   

            var districts = await _districtRepository.GetDistricts();
            return districts.OrderBy(d => d.Name).Select(d => d.Name).ToList();
        }

        /// <summary>
        /// Route to get all platforms
        /// </summary>
        /// <returns>All platforms</returns>
        [HttpGet("platforms")]
        public ActionResult<IEnumerable<String>> GetPlatforms()
        {
            var platforms = _issueReportingRepository.getPlatforms();
            return platforms;
        }

        /// <summary>
        /// Route to get all status types
        /// </summary>
        /// <returns>All status types</returns>
        [HttpGet("statuses")]
        public async Task<ActionResult<IEnumerable<String>>> GetStatuses()
        {
            var statuses = await _statusRepository.AsyncGetAllStatuses();
            return statuses.Select(s => s.Name).ToList();
        }

        [HttpGet("roles")]
        public async Task<ActionResult<IEnumerable<string>>> GetRoles([FromQuery] string type = "userRole")
        {
            var userId = User.GetUserId();
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user == null ){
                return Unauthorized("This user does not exist");
            }
            
            var roles = await _userRepository.GetUserRolesAsync();
            var roleNames = roles.Select(r => r.Name).ToList();

            switch (type)
            {
                case("admin"):
                    roleNames = roleNames.Where(i => RolesHelper.RoleIsAdmin(i)).ToList();
                    break;
                case("data"):
                    roleNames = roleNames.Where(i => RolesHelper.RoleIsDataManagement(i)).ToList();
                    break;
                case("user"):
                    roleNames = roleNames.Where(i => RolesHelper.RoleIsUserManagement(i)).ToList();
                    break;
                case("issue"):
                    roleNames = roleNames.Where(i => RolesHelper.RoleIsIssueManagement(i)).ToList();
                    break; 
                case("status"):
                    roleNames = roleNames.Where(i => RolesHelper.RoleIsIssueStatusUpdater(i)).ToList();
                    break;
                case("restrict"):
                    roleNames = roleNames.Where(i => RolesHelper.RoleAllowsRestriction(i)).ToList();
                    break;
                default:
                    var userRole = User.GetUserRole();
                    if(userRole != RolesHelper.SystemAdminRole)
                        roleNames.Remove(RolesHelper.SystemAdminRole);
                    if(RolesHelper.RoleAllowsRestriction(userRole))
                    {
                        roleNames = roleNames.Where(i => !RolesHelper.RoleIsAdminAccessOnly(i)).ToList();
                    }
                    break;
            }

            return roleNames;
        }
    }
}