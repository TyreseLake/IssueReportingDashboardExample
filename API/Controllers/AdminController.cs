using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Extensions;
using API.Helpers.Pagination;
using API.Helpers;

/// <summary>
/// Controller responsible for handling Admin requests/commands
/// </summary>
namespace API.Controllers
{
    public class AdminController : BaseApiContoller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IMapper _mapper;
        private readonly IIssueTypeRepository _issueTypeRepository;
        private readonly IDistrictRepository _districtRepository;
        private readonly IUserRepository _userRepository;
        private readonly IActivityRepository _activityRepository;
        public static string CONTROLLER_NAME = "Administration";

        /// <summary>
        /// Contructor for Admin Controller
        /// </summary>
        /// <param name="userManager">Access to the user management service</param>
        /// <param name="tokenService">Access to the token service</param>
        /// <param name="signInManager">Access to the sign in manager serivice</param>
        /// <param name="mapper">Access to the auto mapper service</param>
        /// <param name="config">Access to the service config for app settings</param>
        /// <param name="issueTypeRepository">Access to the issue type repository service</param>
        /// <param name="districtRepository">Access to the district repository service</param>
        /// <param name="userRepository">Access to the user repository service</param>
        /// <param name="activityRepository">Access to the activity log repository service</param>
        public AdminController(UserManager<AppUser> userManager,
                               ITokenService tokenService,
                               SignInManager<AppUser> signInManager,
                               IMapper mapper,
                               IConfiguration config,
                               IIssueTypeRepository issueTypeRepository,
                               IDistrictRepository districtRepository,
                               IUserRepository userRepository,
                               IActivityRepository activityRepository)
        {
            _signInManager = signInManager;
            _mapper = mapper;
            _tokenService = tokenService;
            _userManager = userManager;
            _issueTypeRepository = issueTypeRepository;
            _districtRepository = districtRepository;
            _userRepository = userRepository;
            _activityRepository = activityRepository;
        }

        
        [Authorize(Policy = "RequireUserManagementPrivileges")]
        [HttpPost("create-user")]
        [ActionName("Create New User")]
        public async Task<ActionResult<UserDto>> CreateUser(UserInfoDto userInfoDto)
        {
            var currentUser = await _userRepository.GetUserByIdAsync(User.GetUserId());
            if(currentUser == null ){
                return Unauthorized("This user does not exist");
            }
            //Start Logging Activity
            var logData = new {
                userInfoDto.UserName,
                userInfoDto.FirstName,
                userInfoDto.LastName,
                userInfoDto.Email,
                userInfoDto.AccountType,
                userInfoDto.IssueReportAccessPermissions,
                userInfoDto.DistrictAccessPermissions
            };
            var activityId = await _activityRepository.LogActivity("Create New User", CONTROLLER_NAME, logData, currentUser);

            try
            {
                if (await UserExists(userInfoDto.UserName)){
                    var erroredResponse = BadRequest("User is taken.");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Check if the user role is valid
                var role = userInfoDto.AccountType.Trim();
                if(!RolesHelper.RoleIsValid(role)){
                    var erroredResponse = BadRequest("Invalid account type");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Get the current users role
                var currentUserRole = User.GetUserRole();

                if(currentUserRole != RolesHelper.SystemAdminRole  && role == RolesHelper.SystemAdminRole){
                    var erroredResponse = BadRequest("Invalid access to this account type");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Check if the user is able to create a user of the selected role
                if(RolesHelper.RoleAllowsRestriction(currentUserRole)){
                    if(RolesHelper.RoleIsAdminAccessOnly(role)){
                        var erroredResponse = Unauthorized("You are not authorized to create users of type: '" + role + "'");
                        await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        return erroredResponse;
                    }
                }

                // if(role != "Admin" &&
                // role != "Data Manager" &&
                // role != "Issue Manager" &&
                // role != "Default"){
                //     var erroredResponse = BadRequest("Invalid Account Type");
                //     await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                //     return erroredResponse;
                // }
                

                var userIssueTypes = userInfoDto.IssueReportAccessPermissions;
                var userDistricts = userInfoDto.DistrictAccessPermissions;

                //Some users may be restricted by issue types and districts
                if(RolesHelper.RoleAllowsRestriction(role))
                {
                    if(userIssueTypes.Length > 0)
                    {
                        var issueTypes = await _issueTypeRepository.GetIssueTypeNames();

                        // if(userIssueTypes.Length == 0){
                        //     var erroredResponse = BadRequest("For " + role + " users, issue report access permissions must be specified.");
                        //     await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        //     return erroredResponse;
                        // }

                        if(userIssueTypes.Intersect(issueTypes).Count() != userIssueTypes.Count()){
                            var erroredResponse = BadRequest("'" + string.Join(", ", userIssueTypes.Except(issueTypes)) + "' is/are not valid issue types");
                            await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                            return erroredResponse;
                        }

                        var currentUserIssueTypeAccess = currentUser.UserIssueTypes.Select(i => i.IssueType.Name).ToList();
                        if(currentUserIssueTypeAccess.Count() > 0){
                            if(userIssueTypes.Intersect(currentUserIssueTypeAccess).Count() != userIssueTypes.Count()){
                                var erroredResponse = BadRequest("Unathorized access to the following issue types: " + string.Join(", ", userIssueTypes.Except(currentUserIssueTypeAccess)));
                                await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                                return erroredResponse;
                            }
                        }
                    }

                    if(userDistricts.Length > 0)
                    {
                        var districts = await _districtRepository.GetDistrictNames();

                        // if(userDistricts.Length == 0){
                        //     var erroredResponse = BadRequest("For " + role + " users, district access permissions must be specified.");
                        //     await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        //     return erroredResponse;
                        // }

                        if(userDistricts.Intersect(districts).Count() != userDistricts.Count()){
                            var erroredResponse = BadRequest("'" + string.Join(", ", userDistricts.Except(districts)) + "' is/are not valid districts in Trinidad and Tobago.");
                            await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                            return erroredResponse;
                        }

                        var currentUserDistrictAccess = currentUser.UserDistricts.Select(i => i.District.Name).ToList();
                        if(currentUserDistrictAccess.Count() > 0){
                            if(userDistricts.Intersect(currentUserDistrictAccess).Count() != userDistricts.Count()){
                                var erroredResponse = BadRequest("Unathorized access to the following districts: " + string.Join(", ", userDistricts.Except(currentUserDistrictAccess)));
                                await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                                return erroredResponse;
                            }
                        }
                    }
                }
                
                //Map the user information to an App User
                var user = _mapper.Map<AppUser>(userInfoDto);

                //Default the users name to lower case
                user.UserName = userInfoDto.UserName.ToLower();

                var results = await _userManager.CreateAsync(user, userInfoDto.Password);

                if(!results.Succeeded){
                    var erroredResponse = BadRequest(results.Errors);
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Set user password to be reset
                user.RequirePasswordReset = true;

                //Add the user to the roles
                var roleResult = await _userManager.AddToRoleAsync(user, role);

                if (!roleResult.Succeeded) {
                    var erroredResponse = BadRequest("Failed to add to account type");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                var dchangesMade = false;
                var itchangesMade = false;
                
                //Add the users issue types/districts if they are an issue manager, issue status updater or default user
                if(RolesHelper.RoleAllowsRestriction(role))
                {
                    //For users that can be restricted by issue type or district
                    if(RolesHelper.RoleAllowsRestriction(currentUserRole))
                    {
                        //if the users issue types is left blank, they will inherit the issue type restrictions of the creator
                        if(userIssueTypes.Length == 0)
                            userIssueTypes = currentUser.UserIssueTypes.Select(i => i.IssueType.Name).ToArray();

                        //if the users districts are left blank, they will inherit the district restrictions of the creator
                        if(userDistricts.Length == 0)
                            userDistricts = currentUser.UserDistricts.Select(i => i.District.Name).ToArray();
                    }

                    if(userIssueTypes.Length > 0)
                    {
                    //Add the user issue types to the user
                        foreach (var userIssueType in userIssueTypes)
                        {
                            // _issueTypeRepository.AddUserIssueTypeByName(user, userIssueType);
                            var addIssueType = await _issueTypeRepository.GetIssueTypeByName(userIssueType);
                            if(addIssueType != null){
                                _issueTypeRepository.AddUserIssueType(user, addIssueType);
                                itchangesMade = itchangesMade || true;
                            }
                        }

                        //Save the database after the issue types have been added
                        // if(!(await _issueTypeRepository.SaveAllAsync())) 
                        //     return BadRequest("There was a problem setting the new User's access to the selected issue types.");
                    }

                    if(userDistricts.Length > 0)
                    {
                        //Add the user districts to the user
                        foreach (var userDistrict in userDistricts)
                        {
                            // _districtRepository.AddUserDistrictByName(user, userDistrict);
                            var addDistrict = await _districtRepository.GetDistrictByName(userDistrict);
                            if(addDistrict != null){
                                _districtRepository.AddUserDistrict(user, addDistrict);
                                dchangesMade = dchangesMade || true;
                            }
                        }
                        
                        //Save the database after the districts have been added then create and return the user for the client
                        // if(!(await _districtRepository.SaveAllAsync()))
                        //     return BadRequest("There was a problem setting the new User's access to the selected districts.");
                    }
                }    

                //Create and return the user for the client to access (issue types)
                if(itchangesMade) {
                    if(!(await _issueTypeRepository.SaveAllAsync())){
                        await _userManager.DeleteAsync(user);
                        var erroredResponse = BadRequest("There was a problem setting the new User's access to the selected issue types.");
                        await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        return erroredResponse;
                    }
                }

                //Create and return the user for the client to access
                if(dchangesMade) {
                    if(!(await _districtRepository.SaveAllAsync())){
                        await _userManager.DeleteAsync(user);
                        var erroredResponse = BadRequest("There was a problem setting the new User's access to the selected districts.");
                        await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        return erroredResponse;
                    }
                }
                
                var response = new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName.Trim(),
                    FirstName = user.FirstName.Trim(),
                    LastName = user.LastName?.Trim(),
                    UserIssueTypes = userIssueTypes,
                    UserDistricts = userDistricts,
                    Email = user.Email?.Trim()
                };

                await _activityRepository.SetActivityStatus(activityId, SUCCESS_CODE, response);
                return response;
            }
            catch (System.Exception ex)
            {
                await _activityRepository.SetActivityStatus(activityId, FAILURE_CODE, ex);
                throw;
            }
        }

        /// <summary>
        /// Helper function to check if a user exisits
        /// </summary>
        /// <param name="username">Name of user</param>
        /// <returns>Boolean result</returns>
        private async Task<bool> UserExists(string username)
        {
            var result = await _userManager.Users.AnyAsync(x => x.UserName == username.ToLower());
            return result;
        }

        /// <summary>
        /// Route to get information on a specified user
        /// </summary>
        /// <param name="id">Specifies the id of the user</param>
        /// <returns>The user information</returns>
        [Authorize(Policy = "RequireUserManagementPrivileges")]
        [HttpGet("user-info/{id}")]
        public async Task<ActionResult> GetUserInfo(int id)
        {
            var currentUserId = User.GetUserId();
            var currentUser = await _userRepository.GetUserByIdAsync(currentUserId);
            if(currentUser == null ){
                return Unauthorized("This user does not exist");
            }
            
            //Check if the user being requested is valid
            var user = await _userRepository.GetUserByIdAsync(id);
            if(user.UserName == RolesHelper.SystemAdminName)
                return BadRequest("An unexpected error occured");
            if(user == null)
                return NotFound("User not found");
            if(user.UserName == User.GetUserName())
                return BadRequest("Unable get the information for your own profile.");

            //Users with user management privileges that can be restricted
            var currentUserRole = User.GetUserRole();
            if(RolesHelper.RoleAllowsRestriction(currentUserRole)){
                //These users do not have access to certain roles that only admins can view
                var userRole = user.UserRoles.FirstOrDefault().Role.Name;
                if(RolesHelper.RoleIsAdminAccessOnly(userRole)){
                    var erroredResponse = Unauthorized("You do not have access to users with the type: '" + userRole + "'");
                    return erroredResponse;
                }

                var currentUserIssueTypes = user.UserIssueTypes.Select(i => i.IssueType.Name).ToList();
                if(currentUserIssueTypes.Count() > 0){
                    var userIssueTypes = user.UserIssueTypes.Select(i => i.IssueType.Name).ToList();
                    if(userIssueTypes.Count() == 0 || currentUserIssueTypes.Intersect(userIssueTypes).Count() != userIssueTypes.Count())
                    {
                        var erroredResponse = Unauthorized("Invalid issue type access, you do not have access to view these users");
                        return erroredResponse;
                    }
                }

                var currentUserDistricts = user.UserDistricts.Select(i => i.District.Name).ToList();
                if(currentUserDistricts.Count() > 0){
                    var userDistricts = user.UserDistricts.Select(i => i.District.Name).ToList();
                    if(userDistricts.Count() == 0 || currentUserDistricts.Intersect(userDistricts).Count() != userDistricts.Count())
                    {
                        var erroredResponse = Unauthorized("Invalid district access, you do not have access to view these users");
                        return erroredResponse;
                    }
                }
            }

            return Ok(new {
                user.Id,
                user.UserName,
                user.FirstName,
                user.LastName,
                user.Email,
                AccountType = user.UserRoles.SingleOrDefault()?.Role.Name,
                IssueTypesAccess = user.UserIssueTypes.Select(i => i.IssueType.Name).ToList(),
                DistrictsAccess = user.UserDistricts.Select(d => d.District.Name).ToList()
            });
            // return BadRequest("An unexpected error occured when fetching this user");
        }

        /// <summary>
        /// Admin route to update a users information
        /// </summary>
        /// <param name="updateUserInfoDto">Specifies the users update information</param>
        /// <returns>Updated users information</returns>
        [Authorize(Policy = "RequireUserManagementPrivileges")]
        [HttpPost("update-user-info")]
        [ActionName("Update User Information")]
        public async Task<ActionResult> UpdateUserInformation([FromBody] UpdateUserInfoDto updateUserInfoDto)
        {
             //Start Logging Activity
            var currentUser = await _userRepository.GetUserByIdAsync(User.GetUserId());
            if(currentUser == null ){
                return Unauthorized("This user does not exist");
            }
            var activityId = await _activityRepository.LogActivity("Update User Information", CONTROLLER_NAME, updateUserInfoDto, currentUser);

            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == updateUserInfoDto.Id);
                
                //Check if the user exists
                if(user == null){
                    var erroredResponse = NotFound("User not found");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Users should not be able to edit those user with the name Admin
                if(user.UserName == RolesHelper.SystemAdminName){
                    var erroredResponse = BadRequest("This user cannot be modified");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Fetch the name of the current user and check if the user is attempting to edit themselves
                var currentUserName = User.GetUserName();
                if(user.UserName == currentUserName){
                    var erroredResponse = BadRequest("Cannot edit your own profile");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Check if the user already exists
                if(user.UserName != updateUserInfoDto.UserName.Trim()){
                    var existingUser = await _userRepository.GetUserByUsernameAsync(updateUserInfoDto.UserName.Trim());
                    if(existingUser != null){
                        var erroredResponse = BadRequest("This user name belongs to an exisiting user");
                        await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        return erroredResponse;
                    }
                }

                //Get the users current role
                var userRole = (await _userManager.GetRolesAsync(user)).SingleOrDefault();

                //Check if the user being editing is within the users access
                var currentUserRole = User.GetUserRole();
                if(RolesHelper.RoleAllowsRestriction(currentUserRole)){
                    if(RolesHelper.RoleIsAdminAccessOnly(userRole)){
                        var erroredResponse = Unauthorized("You are not authorized to modify a user with the type: '" + userRole + "'");
                        await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        return erroredResponse;
                    }
                }

                //Check if the user entered a valid account type/role
                updateUserInfoDto.AccountType = updateUserInfoDto.AccountType.Trim();
                if(!RolesHelper.RoleIsValid(updateUserInfoDto.AccountType)){
                    var erroredResponse = BadRequest("The account type '" + updateUserInfoDto.AccountType + "' is invalid");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                if(currentUserRole != RolesHelper.SystemAdminRole && updateUserInfoDto.AccountType == RolesHelper.SystemAdminRole){
                    var erroredResponse = BadRequest("Invalid access to this account type");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }
                
                //Users that can be restricted cannot change users role to an admin access only role
                if(RolesHelper.RoleAllowsRestriction(currentUserRole)){
                    if(RolesHelper.RoleIsAdminAccessOnly(updateUserInfoDto.AccountType)){
                        var erroredResponse = Unauthorized("You are not authorized to change a user to the type: '" + updateUserInfoDto.AccountType + "'");
                        await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        return erroredResponse;
                    }
                }

                List<string> currentUserIssueTypes = new List<string>();
                List<string> currentUserDistricts = new List<string>();

                //If the user can have restrictions, check if they have restrictions
                if(RolesHelper.RoleAllowsRestriction(currentUserRole))
                {
                    currentUserIssueTypes = currentUser.UserIssueTypes.Select(i => i.IssueType.Name).ToList();
                    currentUserDistricts = currentUser.UserDistricts.Select(i => i.District.Name).ToList();
                }

                //if the user is changing another users profile to one with restrictions...
                if(RolesHelper.RoleAllowsRestriction(updateUserInfoDto.AccountType))
                {
                    var currentUserCanHaveRestrictions = RolesHelper.RoleAllowsRestriction(currentUserRole);

                    if(updateUserInfoDto.IssueTypesAccess?.Count() > 0)
                    {
                        //Check if the issue types entered are valid
                        var issueTypes = (await _issueTypeRepository.GetIssueTypeNames()).ToList();
                        updateUserInfoDto.IssueTypesAccess = updateUserInfoDto.IssueTypesAccess.Distinct().ToArray();
                        if(issueTypes.Intersect(updateUserInfoDto.IssueTypesAccess).Count() != updateUserInfoDto.IssueTypesAccess.Count()){
                            var erroredResponse = NotFound("Issue Type(s) not found");
                            await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                            return erroredResponse;
                        }

                        //Check if this user is allowed to modify a user with their issue type accesses
                        if(currentUserCanHaveRestrictions){
                            if(currentUserIssueTypes.Count() > 0 && currentUserIssueTypes.Intersect(updateUserInfoDto.IssueTypesAccess).Count() != updateUserInfoDto.IssueTypesAccess.Count()){
                                var erroredResponse = Unauthorized("Not authorized to modify this user to the indicated issue type access");
                                await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                                return erroredResponse;
                            }
                        }
                    }else if(currentUserIssueTypes.Count() > 0){
                        updateUserInfoDto.IssueTypesAccess = currentUserIssueTypes.ToArray();
                    }
                    
                    if(updateUserInfoDto.DistrictsAccess?.Count() > 0)
                    {
                        //Check if the districts enterted are valid
                        var districts = (await _districtRepository.GetDistrictNames()).ToList();
                        updateUserInfoDto.DistrictsAccess = updateUserInfoDto.DistrictsAccess.Distinct().ToArray();
                        if(districts.Intersect(updateUserInfoDto.DistrictsAccess).Count() != updateUserInfoDto.DistrictsAccess.Count()){
                            var erroredResponse = NotFound("District(s) not found");
                            await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                            return erroredResponse;
                        }

                        //Check if this user is allowed to modify a user with their district accesses
                        if(currentUserCanHaveRestrictions){
                            if(currentUserDistricts.Count() > 0 && currentUserDistricts.Intersect(updateUserInfoDto.DistrictsAccess).Count() != updateUserInfoDto.DistrictsAccess.Count()){
                                var erroredResponse = Unauthorized("Not authorized to modify this user to the indicated district access");
                                await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                                return erroredResponse;
                            }
                        }
                    }else if(currentUserDistricts.Count() > 0){
                        updateUserInfoDto.DistrictsAccess = currentUserDistricts.ToArray();
                    }
                }

                //Get the selected user
                var selectedUser = await _userRepository.GetUserByIdAsync(user.Id);

                //Get the users issue types and districts
                var userIssueTypes = selectedUser.UserIssueTypes.ToList();
                var userDistricts = selectedUser.UserDistricts.ToList();
                
                //Check if the current user falls within this users restrictions 
                if(RolesHelper.RoleAllowsRestriction(currentUserRole)){
                    
                    if(currentUserIssueTypes.Count() > 0){
                        var userIssueTypesNames = userIssueTypes.Select(i => i.IssueType.Name).ToList();
                        if(userIssueTypesNames.Count() == 0 || currentUserIssueTypes.Intersect(userIssueTypesNames).Count() != userIssueTypesNames.Count()){
                            var erroredResponse = Unauthorized("Issue Type access error, not authorized to modify this user");
                            await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                            return erroredResponse;
                        }
                    }

                    if(currentUserDistricts.Count() > 0){
                        var userDistrictNames = userDistricts.Select(i => i.District.Name).ToList();
                        if(userDistrictNames.Count() == 0 || currentUserDistricts.Intersect(userDistrictNames).Count() != userDistrictNames.Count()){
                            var erroredResponse = Unauthorized("District access error, not authorized to modify this user");
                            await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                            return erroredResponse;
                        }
                    }   
                }

                //Check if the users current role is being changed
                var roleChanged = false;
                if(userRole != updateUserInfoDto.AccountType)
                {
                    //Remove the user from their previous role and add them to the new role
                    var currentRole = await _userManager.GetRolesAsync(user);
                    var roleResult = await _userManager.RemoveFromRoleAsync(user, currentRole.FirstOrDefault());

                    if (!roleResult.Succeeded){
                        var erroredResponse = BadRequest("Failed to update Account Type");
                        await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        return erroredResponse;
                    }
                    
                    roleResult = await _userManager.AddToRoleAsync(user, updateUserInfoDto.AccountType);

                    if (!roleResult.Succeeded){
                        var erroredResponse = BadRequest("Failed to update Account Type");
                        await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        return erroredResponse;
                    }

                    roleChanged = true;
                }

                var changesWereMade = false;
                // var dchangesWereMade = false;
                // var itchangesWereMade = false;

                //Get the users unchanged users issue types and districts
                var userCurrentIssueTypes = userIssueTypes.Where(uit => !updateUserInfoDto.IssueTypesAccess.Contains(uit.IssueType.Name)).ToList();
                var userCurrentDistricts = userDistricts.Where(ud => !updateUserInfoDto.DistrictsAccess.Contains(ud.District.Name)).ToList();

                // var currentUserIssueTypes = user.UserIssueTypes.Where(uit => !updateUserInfoDto.IssueTypesAccess.Contains(uit.IssueType.Name));
                foreach (var userIssueType in userCurrentIssueTypes)
                {
                    //_issueTypeRepository.RemoveUserIssueType(userIssueType);




                    selectedUser.UserIssueTypes.Remove(userIssueType);

                    // var changesResult = await _issueTypeRepository.SaveAllAsync();
                    // changesWereMade = changesWereMade || changesResult;
                    changesWereMade = changesWereMade || true;
                }
                
                foreach (var userDistrict in userCurrentDistricts)
                {
                    // _districtRepository.RemoveUserDistrict(userDistrict);
                    // var changesResult = await _districtRepository.SaveAllAsync();
                    // changesWereMade = changesWereMade || changesResult;

                    selectedUser.UserDistricts.Remove(userDistrict);
                    changesWereMade = changesWereMade || true;
                }

                // _districtRepository.RemoveAllUserDistricts(user);
                // _issueTypeRepository.RemoveAllUserIssueTypes(user);
                
                if(RolesHelper.RoleAllowsRestriction(updateUserInfoDto.AccountType))
                {
                    if(updateUserInfoDto.IssueTypesAccess.Length > 0)
                    {
                        foreach(var issueType in updateUserInfoDto.IssueTypesAccess.Except(userIssueTypes.Select(uit => uit.IssueType.Name))){
                            // _issueTypeRepository.AddUserIssueTypeByName(user, issueType);
                            var addedIssueType = await _issueTypeRepository.GetIssueTypeByName(issueType);
                            if(issueType != null){
                                // _issueTypeRepository.AddUserIssueType(user, addedIssueType);





                                var newUserIssueType = new UserIssueType{ UserId = user.Id, IssueTypeId = addedIssueType.Id };
                                selectedUser.UserIssueTypes.Add(newUserIssueType);
                                changesWereMade = changesWereMade || true;
                            }
                            // var changesResult = await _issueTypeRepository.SaveAllAsync();
                            // changesWereMade = changesWereMade || changesResult;
                        }
                    }
                    
                    if(updateUserInfoDto.DistrictsAccess.Length > 0)
                    {
                        foreach(var district in updateUserInfoDto.DistrictsAccess.Except(userDistricts.Select(ud => ud.District.Name))){
                            // _districtRepository.AddUserDistrictByName(user, district);
                            var addDistrict = await _districtRepository.GetDistrictByName(district);
                            if(addDistrict != null){
                                // _districtRepository.AddUserDistrict(user, addDistrict);



                                var newUserDistrict = new UserDistrict{ UserId = user.Id, DistrictId = addDistrict.Id };
                                selectedUser.UserDistricts.Add(newUserDistrict);
                                changesWereMade = changesWereMade || true;
                            }
                            // var changesResult = await _districtRepository.SaveAllAsync();
                            // changesWereMade = changesWereMade || changesResult;
                        }
                    }
                }

                // if(itchangesWereMade){
                //     if(!await _issueTypeRepository.SaveAllAsync()){
                //         var erroredResponse = BadRequest("There was a problem setting the specified User's access to the selected issue types.");
                //         await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                //         return erroredResponse;
                //     }
                // }

                // if(dchangesWereMade){
                //     if(!await _districtRepository.SaveAllAsync()){
                //         var erroredResponse = BadRequest("There was a problem setting the specified User's access to the selected districts.");
                //         await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                //         return erroredResponse;
                //     }
                // }

                // var changesToDetails = false;

                if(selectedUser.Email != updateUserInfoDto.Email?.Trim()){
                    selectedUser.Email = updateUserInfoDto.Email?.Trim();
                    await _userManager.UpdateNormalizedEmailAsync(user);
                    changesWereMade = true;
                }
                if(selectedUser.FirstName != updateUserInfoDto.FirstName.Trim()){
                    selectedUser.FirstName = updateUserInfoDto.FirstName.Trim();
                    changesWereMade = true;
                }
                if(selectedUser.LastName != updateUserInfoDto.LastName?.Trim()){
                    selectedUser.LastName = updateUserInfoDto.LastName?.Trim();
                    changesWereMade = true;
                }
                if(selectedUser.UserName != updateUserInfoDto.UserName.Trim()){
                    selectedUser.UserName = updateUserInfoDto.UserName.Trim();
                    await _userManager.UpdateNormalizedUserNameAsync(user);
                    changesWereMade = true;
                }

                if(!roleChanged && !changesWereMade){
                    var response = Ok( new {
                        Message = "No changes were made",
                        User =  new {
                            AccountType = (await _userManager.GetRolesAsync(user)).SingleOrDefault().ToString(),
                            DistrictAccess = updateUserInfoDto.DistrictsAccess,
                            IssueTypesAccess = updateUserInfoDto.IssueTypesAccess,
                            selectedUser.Email,
                            selectedUser.FirstName,
                            selectedUser.LastName,
                            selectedUser.UserName
                        }
                    });

                    await _activityRepository.SetActivityStatus(activityId, SUCCESS_CODE, response);
                    return response; 
                }

                var result = false;
                if(changesWereMade)
                    result = await _userRepository.SaveAllAsync();
                
                if(result || roleChanged )
                {
                    var response = Ok(new {
                        AccountType = (await _userManager.GetRolesAsync(user)).SingleOrDefault().ToString(),
                        DistrictAccess = updateUserInfoDto.DistrictsAccess,
                        IssueTypesAccess = updateUserInfoDto.IssueTypesAccess,
                        selectedUser.Email,
                        selectedUser.FirstName,
                        selectedUser.LastName,
                        selectedUser.UserName
                    });

                    await _activityRepository.SetActivityStatus(activityId, SUCCESS_CODE, response);
                    return response;
                }

                var errored = BadRequest("An unexpected error occured");
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
        /// Update a users password information
        /// </summary>
        /// <param name="updateUserPasswordDto">Specifies the users updated password information</param>
        /// <returns>Updated users password message</returns>
        [Authorize(Policy = "RequireUserManagementPrivileges")]
        [HttpPost("update-user-password")]
        [ActionName("Update User Password")]
        public async Task<ActionResult> UpdateUserPassword([FromBody] UpdateUserPasswordDto updateUserPasswordDto)
        {
            //Start Logging Activity
            var logData = new {
                updateUserPasswordDto.Id
            };
            var currentUser = await _userRepository.GetUserByIdAsync(User.GetUserId());
            if(currentUser == null ){
                return Unauthorized("This user does not exist");
            }
            var activityId = await _activityRepository.LogActivity("Update User Password", CONTROLLER_NAME, logData, currentUser);
            try
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(i => i.Id == updateUserPasswordDto.Id);
                // var user = await _userRepository.GetUserByIdAsync(updateUserPasswordDto.Id);

                //Check if the user exists
                if(user == null){
                    var erroredResponse = NotFound("User not found");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Users should not be able to edit those user with the name Admin
                if(user.UserName == RolesHelper.SystemAdminName){
                    var erroredResponse = BadRequest("This user cannot be modified");
                    
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Fetch the name of the current user and check if the user is attempting to edit themselves
                var currentUserName = User.GetUserName();
                if(user.UserName == currentUserName){
                    var erroredResponse = BadRequest("Can't edit your own profile");
                    await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                    return erroredResponse;
                }

                //Set the user to reset password
                // var requirePasswordReset = user.RequirePasswordReset;
                // if(requirePasswordReset!= true)
                // {
                //     user.RequirePasswordReset = true;
                //     var result = await _userRepository.SaveAllAsync();
                //     if (result == false){
                //         var erroredResponse = BadRequest("An unexpected error occured when setting password. Could not set password reset variable.");
                //         await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                //         return erroredResponse;
                //     }
                // }

                
                //Get the users current role
                var userRole = (await _userManager.GetRolesAsync(user)).SingleOrDefault();



                //Check if the user being editing is within the users access
                var currentUserRole = User.GetUserRole();
                if(RolesHelper.RoleAllowsRestriction(currentUserRole)){
                    if(RolesHelper.RoleIsAdminAccessOnly(userRole)){
                        var erroredResponse = Unauthorized("You are not authorized to modify a user with the type: '" + userRole + "'");
                        await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                        return erroredResponse;
                    }

                    //Check if the current user has restrictions
                    var currentUserIssueTypes = currentUser.UserIssueTypes.Select(i => i.IssueType.Name).ToList();
                    var currentUserDistricts = currentUser.UserDistricts.Select(i => i.District.Name).ToList();

                    //Get the selected user
                    var selectedUser = await _userRepository.GetUserByIdAsync(user.Id);

                    //Get the users issue types and districts
                    var userIssueTypes = selectedUser.UserIssueTypes.ToList();
                    var userDistricts = selectedUser.UserDistricts.ToList();

                    if(currentUserIssueTypes.Count() > 0){
                        var userIssueTypesNames = userIssueTypes.Select(i => i.IssueType.Name).ToList();
                        if(userIssueTypesNames.Count() == 0 || currentUserIssueTypes.Intersect(userIssueTypesNames).Count() != userIssueTypesNames.Count()){
                            var erroredResponse = Unauthorized("Issue Type access error, not authorized to modify this user");
                            await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                            return erroredResponse;
                        }
                    }

                    if(currentUserDistricts.Count() > 0){
                        var userDistrictNames = userDistricts.Select(i => i.District.Name).ToList();
                        if(userDistrictNames.Count() == 0 || currentUserDistricts.Intersect(userDistrictNames).Count() != userDistrictNames.Count()){
                            var erroredResponse = Unauthorized("District access error, not authorized to modify this user");
                            await _activityRepository.SetActivityStatus(activityId, ERROR_CODE, erroredResponse);
                            return erroredResponse;
                        }
                    }
                }

                user.RequirePasswordReset = true;
                var result = await _userRepository.SaveAllAsync();

                //Reset the users password
                string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var results = await _userManager.ResetPasswordAsync(user, resetToken, updateUserPasswordDto.NewPassword);

                if(results.Succeeded){
                    var response = Ok(new {result = "Password updated successfully"});
                    await _activityRepository.SetActivityStatus(activityId, SUCCESS_CODE, response);
                    return response;
                }

                var errored = BadRequest(results.Errors);
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
        /// Route to get all the users activity
        /// </summary>
        /// <param name="userActivityParams">Query parameters for the user activity</param>
        /// <returns>Paginated user activity</returns>
        [Authorize(Policy = "RequireAdminPrivileges")]
        [HttpGet("user-activity")]
        public async Task<ActionResult<IEnumerable<UserActivity>>> GetUserActivity([FromQuery] UserActivityParams userActivityParams)
        {
            var currentUser = await _userRepository.GetUserByIdAsync(User.GetUserId());
            if(currentUser == null ){
                return Unauthorized("This user does not exist");
            }

            var userActivity = await _activityRepository.GetUserActivity(userActivityParams);

            Response.AddPaginationHeader(userActivity.CurrentPage, userActivity.PageSize, userActivity.TotalCount, userActivity.TotalPages);

            return userActivity;
        }

        /// <summary>
        /// Route to get all users
        /// </summary>
        /// <returns>All users</returns>
        [Authorize(Policy = "RequireAdminPrivileges")]
        [HttpGet("users/all")]
        [ActionName("Retrieve All Users")]
        public async Task<ActionResult<IEnumerable<AppUser>>> GetUsers()
        {
            var currentUser = await _userRepository.GetUserByIdAsync(User.GetUserId());
            if(currentUser == null ){
                return Unauthorized("This user does not exist");
            }
            return Ok(await _userRepository.GetUsersAsync());
        }

        /// <summary>
        /// Route to get all users
        /// </summary>
        /// <param name="userParams">Specify query params</param>
        /// <returns>Paginated list of all users</returns>
        [Authorize(Policy = "RequireUserManagementPrivileges")]
        [HttpGet("users")]
        [ActionName("Retrieve Users")]
        public async Task<ActionResult<IEnumerable<UserItemDto>>> GetUsersPaginated([FromQuery] UserParams userParams)
        {
            var currentUser = await _userRepository.GetUserByIdAsync(User.GetUserId());
            if(currentUser == null ){
                return Unauthorized("This user does not exist");
            }

            var currentUserRole = User.GetUserRole();
            userParams.UserRole = currentUserRole;
            userParams.UserName = User.GetUserName();
            
            if(RolesHelper.RoleAllowsRestriction(currentUserRole)){
                var userDistrictAccessIds = currentUser.UserDistricts.Select(i => i.DistrictId).ToArray();
                var userIssueTypeAccessIds = currentUser.UserIssueTypes.Select(i => i.IssueTypeId).ToArray();
                userParams.UserDistrictAccessIds = userDistrictAccessIds;
                userParams.UserIssueTypeAccessIds = userIssueTypeAccessIds;
            }

            var users = await _userRepository.GetPaginatedUsersAsync(userParams);

            Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

            return users;
        }
    }
}