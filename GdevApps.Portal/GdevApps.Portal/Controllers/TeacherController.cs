using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GdevApps.Portal.Data;
using GdevApps.Portal.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Google.Apis.Classroom.v1;
using Google.Apis.Classroom.v1.Data;
using System.Collections.Generic;
using GdevApps.Portal.Models.TeacherViewModels;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using GdevApps.BLL.Contracts;
using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using GdevApps.BLL.Models.GDevClassroomService;

namespace GdevApps.Portal.Controllers
{
    [Authorize]
    //[AllowAnonymous]
    public class TeacherController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IGdevClassroomService _classroomService;
        private readonly IMapper _mapper;

        private readonly HttpContext _context;

        private Singleton Singleton;

        private readonly IAspNetUserService _aspUserService;

        private readonly IHttpContextAccessor _contextAccessor;

        private readonly IGdevSpreadsheetService _spreadSheetService;

        public TeacherController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ILogger<TeacherController> logger,
            IConfiguration configuration,
            IGdevClassroomService classroomService,
            IMapper mapper,
            IHttpContextAccessor httpContext,
            IAspNetUserService aspUserService,
            IHttpContextAccessor contextAccessor,
            IGdevSpreadsheetService spreadSheetService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _configuration = configuration;
            _classroomService = classroomService;
            _mapper = mapper;
            _context = httpContext.HttpContext;
            _aspUserService = aspUserService;
            _contextAccessor = contextAccessor;
            _spreadSheetService = spreadSheetService;
        }

        [HttpGet]
        public async Task<IActionResult> Test()
        {
            try
            {
                var gradeBookLink = "https://docs.google.com/spreadsheets/d/1RUoDCarKOkr2I1iSs9hEuGUTny8kJuOKm-vnvFDFTLg/edit?usp=drive_web&ouid=106890447120707259670";
                var gradebookId = "1RUoDCarKOkr2I1iSs9hEuGUTny8kJuOKm-vnvFDFTLg";
                //var gradebookId = "";
                var userId = _userManager.GetUserId(User);
                // var students = await _spreadSheetService.GetStudentsFromGradebookAsync(
                //     await GetAccessTokenAsync(),
                //     gradebookId,
                //     await GetRefreshTokenAsync(),
                //     userId
                // );


                var student = await _spreadSheetService.GetStudentByEmailFromGradebookAsync("paulivankbs@gmail.com", 
                await GetAccessTokenAsync(),
                gradebookId,
                await GetRefreshTokenAsync(),
                _userManager.GetUserId(User));
                var result = await _spreadSheetService.SaveStudentIntoParentGradebookAsync(student.ResultObject,
                 "",
                await GetAccessTokenAsync(),
                 await GetRefreshTokenAsync(),
                _userManager.GetUserId(User));
                return Ok(student);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        public IActionResult ClassesAsync()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetClasses()
        {
            try
            {
                 List<ClassesViewModel> classes = await GetClassesAsync();
                return Ok(new {data = classes});
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetStudents(string classId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var googleClassroomStudentResult = await _classroomService.GetStudentsByClassIdAndGradebookIdAsync(await GetAccessTokenAsync(),
                                                                                    classId,
                                                                                    "1RUoDCarKOkr2I1iSs9hEuGUTny8kJuOKm-vnvFDFTLg",
                                                                                    await GetRefreshTokenAsync(),
                                                                                    userId);
                var googleStudents = googleClassroomStudentResult.ResultObject.ToList();
                //Get students from Gradebook
                if (!string.IsNullOrEmpty(classId))
                {
                    var studentsTaskResult = await _spreadSheetService.GetStudentsFromGradebookAsync(googleClassroomStudentResult.Credentials,
                     classId,
                      await GetRefreshTokenAsync(),
                       userId);
                    var gradebookStudents = _mapper.Map<IEnumerable<GoogleStudent>>(studentsTaskResult.ResultObject);
                    var gradebookStudentsEmails = gradebookStudents.Select(s => s.Email).ToList();
                    foreach (var student in googleStudents.Where(s => gradebookStudents.Select(g => g.Email).Contains(s.Email)).ToList())
                    {
                        student.IsInClassroom = true;
                    }

                    googleStudents.AddRange(gradebookStudents.Where(g => !googleStudents.Select(s => s.Email).Contains(g.Email)).ToList());
                }

                var studentsList = _mapper.Map<List<StudentsViewModel>>(googleStudents);

                return Ok(new { data = studentsList });
            }
            catch (Exception ex)
            {
                 return BadRequest(ex);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetClassesForStudents()
        {
            try
            {
                var classes = await GetClassesAsync();
                var classesList = new SelectList(classes, "Id", "Name");

                return View("ClassesForStudents",new ClassesForStudentsViewModel{Classes = classesList});
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpGet]
        public ActionResult AddGradebook(string classroomId)
        {
            if(!string.IsNullOrEmpty(classroomId)){
                return PartialView("_AddGradebook", new ClassSheetsViewModel() { ClassroomId = classroomId });
            }

            return BadRequest(classroomId);
        }

        [HttpPost]
        public async Task<ActionResult> AddGradebook(ClassSheetsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var sheet = Singleton.Instance.Sheets.Where(s => s.ClassroomId == model.ClassroomId && s.Id == model.Id).FirstOrDefault();
                if (sheet != null)
                {
                    ModelState.AddModelError("Id", $"Gradebook with suck id already exists");
                    return PartialView("_AddGradebook", model);
                }

                var isValidLink = await CheckLink(model.Link);
                if (!isValidLink)
                {
                    ModelState.AddModelError("Link", $"Link is not valid. Provide a valid link");
                    return PartialView("_AddGradebook", model);
                }

                Singleton.Instance.Sheets.Add(model);
                return PartialView("_AddGradebook", model);
            }

            return PartialView("_AddGradebook", model);
        }

        [HttpPost]
        public async Task<ActionResult> EditGradebook(ClassSheetsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var sheet = Singleton.Instance.Sheets.Where(s => s.Id == model.Id).FirstOrDefault();
                if (sheet != null)
                {
                    var isValidLink = await CheckLink(model.Link);
                    if(!isValidLink){
                        ModelState.AddModelError("Link", $"Link is not valid. Provide a valid link");
                        return PartialView("_EditGradebook", model);
                    }

                    sheet.Id = model.Id;
                    sheet.Link = model.Link;
                    sheet.Name = model.Name;
                    return Ok();
                }
                else
                {
                    return BadRequest($"Gradebook with id {model.Id} was not found");
                }

            }

            return PartialView("_EditGradebook", model);
        }

        [HttpGet]
        public ActionResult GetGradebookById(string classroomId, string gradebookId)
        {
            try
            {
                var sheet = Singleton.Instance.Sheets.Where(s => s.ClassroomId == classroomId && s.Id == gradebookId).FirstOrDefault();
                if (sheet != null)
                {
                    return PartialView("_EditGradebook", sheet);
                }
                else
                {
                    return BadRequest($"Gradebook with id {gradebookId} was not found");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public ActionResult RemoveGradebook(string classroomId, string gradebookId){
            try
            {
                if (!string.IsNullOrEmpty(classroomId) && !string.IsNullOrEmpty(gradebookId))
                {
                    var sheet = Singleton.Instance.Sheets.Where(s => s.ClassroomId == classroomId && s.Id == gradebookId).FirstOrDefault();
                    if (sheet != null)
                    {
                        Singleton.Instance.Sheets.Remove(sheet);
                        return Ok();
                    }
                    else
                    {
                        return BadRequest($"Gradebook with id {gradebookId} was not found");
                    }
                }
                return BadRequest(classroomId);
            }
            catch (Exception err)
            {
                return BadRequest(err.Message);
            }
        }

        private async Task<List<ClassesViewModel>> GetClassesAsync()
        {
            var classes = _mapper.Map<List<ClassesViewModel>>(
                await _classroomService.GetAllClassesAsync(await GetAccessTokenAsync(), await GetRefreshTokenAsync(), _userManager.GetUserId(User))
                );
                
            return classes;
        }

        private async Task<bool> CheckLink(string link)
        {
            if (link.StartsWith("https://docs.google.com"))
            {
                var isGradeBookAsyncResult = await _spreadSheetService.IsGradeBookAsync("", await GetAccessTokenAsync(), await GetRefreshTokenAsync(), _userManager.GetUserId(User), link);
                return isGradeBookAsyncResult.ResultObject.Result;
            }
            return false;
        }

        private async Task<string> GetAccessTokenAsync()
        {
            // Include the access token in the properties
            var accessToken = await _context.GetTokenAsync("access_token");
            var tokensInfo = await GetTokensInfoAsync();
            if (string.IsNullOrEmpty(accessToken) || tokensInfo.IsUpdated)
            {
                return tokensInfo.Tokens.Where(t=>t.Name == "access_token").Select(t=>t.Value).FirstOrDefault() ;
            }

            return accessToken;
        }

        private async Task<string> GetRefreshTokenAsync()
        {
            // Include the access token in the properties
            var refreshToken = await _context.GetTokenAsync("refresh_token");
            var tokensInfo = await GetTokensInfoAsync();
            if (string.IsNullOrEmpty(refreshToken) || tokensInfo.IsUpdated)
            {
                return tokensInfo.Tokens.Where(t=>t.Name == "refresh_token").Select(t=>t.Value).FirstOrDefault() ;
            }

            return refreshToken;
        }

        private async Task<string> GetTockenUpdatedTimeAsync()
        {
            // Include the access token in the properties
            var tockenUpdatedTime = await _context.GetTokenAsync("token_updated_time");
            var tokensInfo = await GetTokensInfoAsync();
            if (string.IsNullOrEmpty(tockenUpdatedTime) || tokensInfo.IsUpdated)
            {
                return tokensInfo.Tokens.Where(t=>t.Name == "token_updated_time").Select(t=>t.Value).FirstOrDefault() ;
            }

            return tockenUpdatedTime;
        }

        private async Task<IEnumerable<GdevApps.BLL.Models.AspNetUsers.AspNetUserToken>> GetAllUserTokens()
        {
            if (User.Identity.IsAuthenticated)
            {
               return  await _aspUserService.GetAllTokensByUserIdAsync(_userManager.GetUserId(User));
            }

            return new List<GdevApps.BLL.Models.AspNetUsers.AspNetUserToken>();
        }

        private async Task<TokenResponse> GetTokensInfoAsync()
        {
            var allTokens = await GetAllUserTokens();
            var isUpdated = allTokens.Where(t=>t.Name == "token_updated").Select(t=>t.Value).FirstOrDefault();
            bool isUpdatedParsed;
            Boolean.TryParse(isUpdated, out isUpdatedParsed);
            if (!string.IsNullOrEmpty(isUpdated) && isUpdatedParsed)
            {
                DateTime createdDate;
                DateTime.TryParse(allTokens.Where(t => t.Name == "created").Select(t => t.Value).FirstOrDefault(), out createdDate);

                DateTime updatedDate;
                DateTime.TryParse(allTokens.Where(t => t.Name == "token_updated_time").Select(t => t.Value).FirstOrDefault(), out updatedDate);
                if (updatedDate > createdDate)
                {
                    return new TokenResponse(isUpdatedParsed, allTokens);
                }
            }

            return new TokenResponse(false, allTokens);
        }
    }

    internal sealed class Singleton
    {
        private static Singleton instance = null;
        private static readonly object padlock = new object();
        public readonly List<ClassSheetsViewModel> Sheets;
        public string ExternalAccessToken;
        Singleton()
        {
            Sheets = new List<ClassSheetsViewModel>();
            ExternalAccessToken = "";
        }

        public static Singleton Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new Singleton();
                    }
                    return instance;
                }
            }
        }
    }

    internal sealed class TokenResponse
    {
        public TokenResponse(bool isUpdated, IEnumerable<GdevApps.BLL.Models.AspNetUsers.AspNetUserToken> tokens)
        {
            this.IsUpdated = isUpdated;
            this.Tokens = tokens;
        }
        public bool IsUpdated { get; set; }

        public IEnumerable<GdevApps.BLL.Models.AspNetUsers.AspNetUserToken> Tokens { get; set; }
    } 
}