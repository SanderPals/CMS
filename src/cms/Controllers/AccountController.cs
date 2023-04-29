using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Site.Data;
using Site.Models;
using Site.Models.Account.ViewModels;
using Site.Services;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static Site.Models.User;

namespace Site.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IHostingEnvironment _env;
        private readonly SiteContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly ILogger _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ISession _session => _httpContextAccessor.HttpContext.Session;
        private IStringLocalizer<AccountController> _localizer;

        public AccountController(
            IHostingEnvironment env,
            SiteContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ISmsSender smsSender,
            ILoggerFactory loggerFactory,
            IHttpContextAccessor httpContextAccessor,
            IStringLocalizer<AccountController> localizer)
        {
            _env = env;
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _logger = loggerFactory.CreateLogger<AccountController>();
            _httpContextAccessor = httpContextAccessor;
            _localizer = localizer;
        }

        //
        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // Require the user to have a confirmed email before they can log on.
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    if (!await _userManager.IsEmailConfirmedAsync(user))
                    {
                        ModelState.AddModelError(string.Empty,
                                      _localizer["RequireConfirmedEmail"].Value);
                        return View(model);
                    }
                }

                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    _logger.LogInformation(1, "User logged in.");
                    return RedirectToAction("LoginRoute", new { ReturnUrl = returnUrl });
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning(2, "User account locked out.");

                    string emailTemplateHtml = System.IO.File.ReadAllText(_env.WebRootPath + "\\templates\\email\\default\\index.html");
                    emailTemplateHtml = emailTemplateHtml.Replace("<!replace:title!>", _localizer["SpineLockedOut"].Value);
                    emailTemplateHtml = emailTemplateHtml.Replace("<!panel:url!>", string.Format("{0}://{1}{2}", Request.Scheme, Request.Host, "/templates/email/default"));
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine(_localizer["SpineLockOutMail", _httpContextAccessor.HttpContext.Connection.RemoteIpAddress].Value);
                    emailTemplateHtml = emailTemplateHtml.Replace("<!replace:body!>", sb.ToString());

                    string[] emails = {
                        model.Email,
                        "info@unveil.nl"
                    };
                    await _emailSender.SendEmailAsync(emails, _localizer["SpineLockedOutSubject"].Value, emailTemplateHtml, "Spine", "info@unveil.nl");

                    return View("Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, _localizer["InvalidLoginAttempt"].Value);
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // below method is new to get the UserId and send to url
        public async Task<IActionResult> LoginRoute(string returnUrl)  //this method is new
        {
            await new Website(_context, _userManager, _httpContextAccessor, _signInManager).SetWebsiteClaimsByUserIdAsync(_userManager.GetUserId(User));

            return RedirectToLocal(returnUrl);
        }


       // GET: /Account/Register
       [HttpGet]
       [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }


       // POST: /Account/Register
       [HttpPost]
       [AllowAnonymous]
       [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713
                    // Send an email with this link
                    //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    //var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
                    //await _emailSender.SendEmailAsync(model.Email, "Confirm your account",
                    //    $"Please confirm your account by clicking this link: <a href='{callbackUrl}'>link</a>");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation(3, "User created a new account with password.");
                    return RedirectToLocal(returnUrl);
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation(4, "User logged out.");
            return RedirectToAction(nameof(AccountController.Login), "");
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        //
        // GET: /Account/ExternalLoginCallback
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return View(nameof(Login));
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (result.Succeeded)
            {
                _logger.LogInformation(5, "User logged in with {Name} provider.", info.LoginProvider);
                return RedirectToLocal(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl });
            }
            if (result.IsLockedOut)
            {
                return View("Lockout");
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["LoginProvider"] = info.LoginProvider;
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation(6, "User created an account using {Name} provider.", info.LoginProvider);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        // GET: /Account/ConfirmEmail
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return View("Error");
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("UserNotExistOrNotConfirmed");
                }

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713
                // Send an email with this link
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.RouteUrl("ResetPassword", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);

                string html = System.IO.File.ReadAllText(_env.WebRootPath + "\\templates\\html\\ForgotPassword.html");
                html = html.Replace("<!replace:email!>", model.Email);
                html = html.Replace("<!replace:url!>", callbackUrl);

                string emailTemplateHtml = System.IO.File.ReadAllText(_env.WebRootPath + "\\templates\\email\\default\\index.html");
                emailTemplateHtml = emailTemplateHtml.Replace("<!replace:title!>", _localizer["ResetPasswordEmail"].Value);
                emailTemplateHtml = emailTemplateHtml.Replace("<!panel:url!>", string.Format("{0}://{1}{2}", Request.Scheme, Request.Host, "/templates/email/default"));
                emailTemplateHtml = emailTemplateHtml.Replace("<!replace:body!>", html);

                string[] emails = {
                    model.Email
                };
                await _emailSender.SendEmailAsync(emails, _localizer["ResetPasswordSubject"].Value, emailTemplateHtml, "Spine", "info@unveil.nl");

                return View("ForgotPasswordConfirmation");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult UserNotExistOrNotConfirmed()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
            }
            //AddErrors(result);
            ModelState.AddModelError(string.Empty, _localizer["ResetPasswordFailed"].Value);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/SendCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl = null, bool rememberMe = false)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }

            //var userFactors = await _userManager.GetValidTwoFactorProvidersAsync(user);
            //var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();

            return View(new SendCodeViewModel { ReturnUrl = returnUrl, RememberMe = rememberMe, Email = user.Email.ToString() });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }

            // Generate the token and send it
            var code = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
            if (string.IsNullOrWhiteSpace(code))
            {
                return View("Error");
            }

            var message = _localizer["SecurityCodeEmail"].Value + code;
            //if (model.SelectedProvider == "Email")
            //{
            string emailTemplateHtml = System.IO.File.ReadAllText(_env.WebRootPath + "\\templates\\email\\default\\index.html");
            emailTemplateHtml = emailTemplateHtml.Replace("<!replace:title!>", message);
            emailTemplateHtml = emailTemplateHtml.Replace("<!panel:url!>", string.Format("{0}://{1}{2}", Request.Scheme, Request.Host, "/templates/email/default"));
            emailTemplateHtml = emailTemplateHtml.Replace("<!replace:body!>", "");

            string[] emails = {
                await _userManager.GetEmailAsync(user)
            };
            await _emailSender.SendEmailAsync(emails, _localizer["SecurityCodeSubject"].Value, emailTemplateHtml, "Spine", "info@unveil.nl");
            //}
            //else if (model.SelectedProvider == "Phone")
            //{
            //    await _smsSender.SendSmsAsync(await _userManager.GetPhoneNumberAsync(user), message);
            //}

            return RedirectToAction(nameof(VerifyCode), new { Provider = "Email", ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/VerifyCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyCode(string provider, bool rememberMe, string returnUrl = null)
        {
            // Require that the user has already logged in via username/password or external login
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes.
            // If a user enters incorrect codes for a specified amount of time then the user account
            // will be locked out for a specified amount of time.
            var result = await _signInManager.TwoFactorSignInAsync(model.Provider, model.Code, model.RememberMe, model.RememberBrowser);
            if (result.Succeeded)
            {
                return RedirectToLocal(model.ReturnUrl);
            }
            if (result.IsLockedOut)
            {
                var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
                if (user == null)
                {
                    return View("Error");
                }

                _logger.LogWarning(7, "User account locked out.");

                string emailTemplateHtml = System.IO.File.ReadAllText(_env.WebRootPath + "\\templates\\email\\default\\index.html");
                emailTemplateHtml = emailTemplateHtml.Replace("<!replace:title!>", _localizer["SpineLockedOut"].Value);
                emailTemplateHtml = emailTemplateHtml.Replace("<!panel:url!>", string.Format("{0}://{1}{2}", Request.Scheme, Request.Host, "/templates/email/default"));
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(_localizer["SpineLockOutMail", _httpContextAccessor.HttpContext.Connection.RemoteIpAddress].Value);
                emailTemplateHtml = emailTemplateHtml.Replace("<!replace:body!>", sb.ToString());

                string[] emails = {
                    user.Email,
                    "info@unveil.nl"
                };
                await _emailSender.SendEmailAsync(emails, _localizer["SpineLockedOutSubject"].Value, emailTemplateHtml, "Spine", "info@unveil.nl");

                return View("Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid code.");
                return View(model);
            }
        }

        //
        // GET: /Account/EditPassword
        [HttpGet]
        [Route("/account/password")]
        public IActionResult EditPassword(string code = null)
        {
            return View();
        }

        //
        // POST: /Account/EditPassword
        [HttpPost]
        [Route("/account/password")]
        public async Task<IActionResult> EditPassword(EditPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (User == null)
            {
                return View("Error");
            }

            var user = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                ViewData["succeeded"] = _localizer["PasswordChanged"].Value;
                return View();
            }

            //AddErrors(result);
            ModelState.AddModelError(string.Empty, _localizer["EditPasswordFailed"].Value);
            return View();
        }

        //
        // GET: /Account/Dashboard
        [HttpGet]
        [Route("/dashboard")]
        public IActionResult Dashboard(int id)
        {
            //Get user by id
            string userId = _userManager.GetUserId(User);
            IEnumerable<AspNetUserClaims> _aspNetUserClaims = new User(_context, _userManager, _httpContextAccessor).GetUserClaimsByUserId(userId);

            string firstName = _localizer["PersonWithoutName"].Value;
            AspNetUserClaims _aspNetUserClaim = _aspNetUserClaims.FirstOrDefault(AspNetUserClaims => AspNetUserClaims.ClaimType == "FirstName");
            if (_aspNetUserClaim != null)
            {
                firstName = _aspNetUserClaim.ClaimValue;
            }

            ViewBag.FirstName = firstName;

            return View();
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        //
        // GET: /Account/Users
        [Authorize(Roles = "Administrator, Developer")]
        [HttpGet]
        [Route("/users")]
        public IActionResult Users(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            return View();
        }

        //
        // GET: /Account/Users
        [HttpGet]
        [Route("/users/add")]
        [Route("/users/{id}")]
        [Route("/account/edit")]
        public IActionResult EditUser(string id)
        {
            switch (Request.Path)
            {
                case "/account/edit":
                    ViewBag.UserId = _userManager.GetUserId(User);
                    break;
                case "/users/add":
                    if (!User.IsInRole("Administrator") && !User.IsInRole("Developer"))
                    {
                        return Redirect("~/dashboard");
                    }
                    ViewBag.UserId = null;
                    break;
                default:
                    if (!User.IsInRole("Administrator") && !User.IsInRole("Developer"))
                    {
                        return Redirect("~/dashboard");
                    }
                    ViewBag.UserId = id;
                    break;
            }

            return View();
        }

        [Authorize(Roles = "Administrator, Developer")]
        [Route("/spine-api/users")]
        [HttpGet]
        public IActionResult GetList()
        {
            List<Dictionary<string, object>> uParentRow = new List<Dictionary<string, object>>();
            Dictionary<string, object> uChildRow;

            try
            {
                string userId = _userManager.GetUserId(User);
                User user = new User(_context, _userManager, _httpContextAccessor);
                IEnumerable<UserBundle> _userBundles = user.GetUserBundlesByUserId(userId);
                foreach (var userBundle in _userBundles)
                {
                    var developer = userBundle.AspNetRoles.FirstOrDefault(AspNetRoles => AspNetRoles.Name == "Developer");
                    if (developer == null)
                    {
                        string roles = "";
                        roles = string.Join(", ", userBundle.AspNetRoles.Select(AspNetRoles => AspNetRoles.Name));

                        string firstName = user.GetClaimFromUserBundleByType("FirstName", userBundle);
                        string lastName = user.GetClaimFromUserBundleByType("LastName", userBundle);

                        uChildRow = new Dictionary<string, object>
                        {
                            { "Id", userBundle.AspNetUser.Id},
                            { "FirstName", firstName},
                            { "LastName", lastName},
                            { "Email", userBundle.AspNetUser.Email},
                            { "PhoneNumber", userBundle.AspNetUser.PhoneNumber},
                            { "Roles", roles},
                        };
                        uParentRow.Add(uChildRow);
                    }
                }
                    
                return Ok(Json(new
                {
                    users = uParentRow
                }));
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CannotShowUsers"].Value
                }));
            }
        }

        [Route("/spine-api/user")]
        [HttpGet]
        public IActionResult GetUser(string id)
        {
            string userId = _userManager.GetUserId(User);

            //If the user is not a admin or developer, this ensures he can only edit himself
            if (!User.IsInRole("Administrator") && !User.IsInRole("Developer"))
            {
                if (id != userId)
                {
                    return StatusCode(401);
                }
            }

            string preamble = "";
            string firstName = "";
            string lastName = "";
            string email = "";
            string phoneNumber = "";
            string concurrencyStamp = "";
            string roleId = "";
            bool options = false;

            if (id != null)
            {
                User user = new User(_context, _userManager, _httpContextAccessor);

                //Get user by id
                UserBundle _userBundle = user.GetUserBundleByUserId(id);
                if (_userBundle == null)
                {
                    return StatusCode(400, Json(new
                    {
                        messageType = "warning",
                        message = _localizer["CouldNotFindUser"].Value
                    }));
                }
                else
                {
                    preamble = user.GetClaimFromUserBundleByType("Preamble", _userBundle);
                    firstName = user.GetClaimFromUserBundleByType("FirstName", _userBundle);
                    lastName = user.GetClaimFromUserBundleByType("LastName", _userBundle);

                    //If the user change his own account he can also edit his email and phonenumber
                    if (id == userId)
                    {
                        email = _userBundle.AspNetUser.Email;
                        phoneNumber = _userBundle.AspNetUser.PhoneNumber;

                        options = false;
                    }
                    else
                    {
                        options = true;

                        roleId = user.GetFirstRoleIdFromUserBundle(_userBundle);
                    }
                }

                concurrencyStamp = _userBundle.AspNetUser.ConcurrencyStamp;
            }

            List<Dictionary<string, object>> roles = new List<Dictionary<string, object>>();
            if (id != userId)
            {
                Dictionary<string, object> rChildRow;
                var _aspNetRoles = _context.AspNetRoles.OrderBy(AspNetRoles => AspNetRoles.Name);
                foreach (var aspNetRole in _aspNetRoles)
                {
                    if (aspNetRole.Name != "Developer")
                    {
                        rChildRow = new Dictionary<string, object>()
                        {
                            { "Id", aspNetRole.Id},
                            { "Name", aspNetRole.Name}
                        };
                        roles.Add(rChildRow);
                    }
                }
            }

            return Ok(Json(new
            {
                preamble,
                firstName,
                lastName,
                email,
                phoneNumber,
                concurrencyStamp,
                roleId,
                roles,
                options
            }));
        }

        [Route("/spine-api/user")]
        [HttpPost]
        public async Task<IActionResult> PostAsync(string id, string preamble, string firstName, string lastName, string email, string phoneNumber, string concurrencyStamp, string role)
        {
            string userId = _userManager.GetUserId(User);

            //If the user is not a admin or developer, this ensures he can only edit himself
            if (!User.IsInRole("Administrator") && !User.IsInRole("Developer"))
            {
                if (id != userId)
                {
                    return StatusCode(401);
                }
            }

            User user = new User(_context, _userManager, _httpContextAccessor);
            if (id != null)
            {
                try
                {
                    //Get user from database by id
                    UserBundle _userBundle = user.GetUserBundleByUserId(id);

                    //Check if concurrency stamp is still the same
                    if (concurrencyStamp != _userBundle.AspNetUser.ConcurrencyStamp)
                    {
                        return StatusCode(400, Json(new
                        {
                            messageType = "modelAlert",
                            preamble = user.GetClaimFromUserBundleByType("Preamble", _userBundle),
                            firstName = user.GetClaimFromUserBundleByType("FirstName", _userBundle),
                            lastName = user.GetClaimFromUserBundleByType("LastName", _userBundle),
                            email = _userBundle.AspNetUser.Email,
                            phoneNumber = _userBundle.AspNetUser.PhoneNumber,
                            concurrencyStamp = _userBundle.AspNetUser.ConcurrencyStamp,
                            role
                        }));
                    }

                    //Get application user by id
                    ApplicationUser _applicationUser = await _userManager.FindByIdAsync(id);
                    if (id == userId)
                    {
                        //Check if phone number and email are different then the one in the database
                        bool phoneNumberConfirmed = _userBundle.AspNetUser.PhoneNumberConfirmed;
                        if (phoneNumber != _userBundle.AspNetUser.PhoneNumber) { phoneNumberConfirmed = false; }
                        bool emailConfirmed = _userBundle.AspNetUser.EmailConfirmed;
                        //if (email != _userBundle.AspNetUser.Email) {
                        //    emailConfirmed = false;
                        //}

                        //Update the user with the new values
                        _applicationUser.Email = email.ToLower();
                        _applicationUser.EmailConfirmed = emailConfirmed;
                        _applicationUser.PhoneNumber = phoneNumber;
                        _applicationUser.PhoneNumberConfirmed = phoneNumberConfirmed;
                        _applicationUser.NormalizedEmail = email.ToUpper();
                        _applicationUser.NormalizedUserName = email.ToUpper(); //custom property
                        _applicationUser.UserName = email.ToLower();

                        //Apply the changes if any to the db
                        IdentityResult result = await _userManager.UpdateAsync(_applicationUser);
                        if (result.Succeeded)
                        {
                            //if (email != _userBundle.AspNetUser.Email)
                            //{
                            //    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713
                            //    // Send an email with this link
                            //    //var code = await _userManager.GenerateEmailConfirmationTokenAsync(_applicationUser);
                            //    var callbackUrl = Url.RouteUrl("Login", new { }, protocol: HttpContext.Request.Scheme);
                            //
                            //    string html = System.IO.File.ReadAllText(_env.WebRootPath + "\\templates\\html\\ConfirmEmail.html");
                            //    html = html.Replace("<!replace:preamble!>", preamble);
                            //    html = html.Replace("<!replace:firstName!>", firstName);
                            //    html = html.Replace("<!replace:lastName!>", lastName);
                            //    html = html.Replace("<!replace:email!>", email);
                            //    html = html.Replace("<!replace:url!>", callbackUrl);
                            //
                            //    string emailTemplateHtml = System.IO.File.ReadAllText(_env.WebRootPath + "\\templates\\email\\default\\index.html");
                            //    emailTemplateHtml = emailTemplateHtml.Replace("<!replace:title!>", _localizer["NewEmail"].Value);
                            //    emailTemplateHtml = emailTemplateHtml.Replace("<!panel:url!>", string.Format("{0}://{1}{2}", Request.Scheme, Request.Host, "/templates/email/default"));
                            //    emailTemplateHtml = emailTemplateHtml.Replace("<!replace:body!>", html);
                            //
                            //    string[] emails = {
                            //        email
                            //    };
                            //    await _emailSender.SendEmailAsync(emails, _localizer["NewEmailSubject"].Value, emailTemplateHtml, "Spine", "info@unveil.nl");
                            //}

                            //Reset user cookie
                            await _signInManager.RefreshSignInAsync(await _userManager.FindByIdAsync(userId));
                        }
                        else
                        {
                            return StatusCode(400, Json(new
                            {
                                messageType = "warning",
                                message = _localizer["CouldNotUpdateUser"].Value
                            }));
                        }
                    }
                    else
                    {
                        //Remove old role if the user has one
                        AspNetRoles _aspNetRole = _userBundle.AspNetRoles.FirstOrDefault();
                        if (_aspNetRole != null)
                        {
                            IdentityResult deletionResult = await _userManager.RemoveFromRoleAsync(_applicationUser, _aspNetRole.Name);
                        }
                        IdentityResult addResult = await _userManager.AddToRoleAsync(_applicationUser, role);
                    }

                    var userClaims = await _userManager.GetClaimsAsync(_applicationUser);
                    Claim preambleClaim = userClaims.Where(x => x.Type.Equals("Preamble")).FirstOrDefault();
                    await _userManager.ReplaceClaimAsync(_applicationUser, preambleClaim, new Claim("Preamble", preamble));
                    Claim firstNameClaim = userClaims.Where(x => x.Type.Equals("FirstName")).FirstOrDefault();
                    await _userManager.ReplaceClaimAsync(_applicationUser, firstNameClaim, new Claim("FirstName", firstName));
                    Claim lastNameClaim = userClaims.Where(x => x.Type.Equals("LastName")).FirstOrDefault();
                    await _userManager.ReplaceClaimAsync(_applicationUser, lastNameClaim, new Claim("LastName", lastName));

                    _applicationUser = await _userManager.FindByIdAsync(id);
                    return Ok(Json(new
                    {
                        messageType = "success",
                        message = _localizer["UpdatedSuccess"].Value,
                        concurrencyStamp = _applicationUser.ConcurrencyStamp
                    }));
                }
                catch
                {
                    return StatusCode(400, Json(new
                    {
                        messageType = "warning",
                        message = _localizer["CouldNotUpdate"].Value
                    }));
                }
            }
            else
            {
                var _applicationUser = new ApplicationUser { UserName = email, Email = email, PhoneNumber = phoneNumber, EmailConfirmed = true };
                string password = user.GenerateRandomPassword();

                IdentityResult result = await _userManager.CreateAsync(_applicationUser, password);
                if (result.Succeeded)
                {
                    List<Claim> claims = new List<Claim>() {
                        new Claim("Preamble", preamble),
                        new Claim("FirstName", firstName),
                        new Claim("LastName", lastName)
                    };
                    await _userManager.AddToRoleAsync(_applicationUser, role);
                    await _userManager.AddClaimsAsync(_applicationUser, claims);

                    //Link user to company
                    if (!user.LinkUserToCompanyByUserId(userId, _applicationUser.Id))
                    {
                        return StatusCode(400, Json(new
                        {
                            messageType = "warning",
                            message = _localizer["CouldNotCreateUser"].Value
                        }));
                    }

                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713
                    // Send an email with this link
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(_applicationUser);
                    var callbackUrl = Url.RouteUrl("Login", new { }, protocol: HttpContext.Request.Scheme);

                    string html = System.IO.File.ReadAllText(_env.WebRootPath + "\\templates\\html\\RegisterConfirmEmail.html");
                    html = html.Replace("<!replace:preamble!>", preamble);
                    html = html.Replace("<!replace:firstName!>", firstName);
                    html = html.Replace("<!replace:lastName!>", lastName);
                    html = html.Replace("<!replace:email!>", email);
                    html = html.Replace("<!replace:password!>", password);
                    html = html.Replace("<!replace:url!>", callbackUrl);

                    string emailTemplateHtml = System.IO.File.ReadAllText(_env.WebRootPath + "\\templates\\email\\default\\index.html");
                    emailTemplateHtml = emailTemplateHtml.Replace("<!replace:title!>", _localizer["AccountCreated"].Value);
                    emailTemplateHtml = emailTemplateHtml.Replace("<!panel:url!>", string.Format("{0}://{1}{2}", Request.Scheme, Request.Host, "/templates/email/default"));
                    emailTemplateHtml = emailTemplateHtml.Replace("<!replace:body!>", html);

                    string[] emails = {
                        email
                    };
                    await _emailSender.SendEmailAsync(emails, _localizer["AccountCreatedSubject"].Value, emailTemplateHtml, "Spine", "info@unveil.nl");

                    return Ok(Json(new
                    {
                        messageType = "success",
                        message = _localizer["UserIsCreated"].Value,
                        redirect = "/users"
                    }));
                }
                else
                {
                    return StatusCode(400, Json(new
                    {
                        messageType = "warning",
                        message = _localizer["CouldNotCreateeUserEmail"].Value
                    }));
                }
            }
        }

        [Authorize(Roles = "Administrator, Developer")]
        [Route("/spine-api/user/delete")]
        [HttpPost]
        public IActionResult DeleteUser(string id)
        {
            try
            {
                //User can't delete himself.
                string userId = _userManager.GetUserId(User);
                if (userId == id)
                {
                    return StatusCode(400, Json(new
                    {
                        messageType = "warning",
                        message = _localizer["CannotDeleteYourself"].Value
                    }));
                }

                new User(_context, _userManager, _httpContextAccessor).DeleteUserById(id);
            }
            catch
            {
                return StatusCode(400, Json(new
                {
                    messageType = "warning",
                    message = _localizer["CouldNotDeleteUser"].Value
                }));
            }

            return Ok(Json(new
            {
                messageType = "success",
                message = _localizer["UserSuccessDeleted"].Value,
                redirect = "/users"
            }));
        }


        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private Task<ApplicationUser> GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(HttpContext.User);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return Redirect("~/dashboard");
            }
        }

        #endregion
    }
}
