using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shopping.Common;
using Shopping.Data.Entities;
using Shopping.Data.Enums;
using Shopping.Helpers;
using Shopping.Models;
using Vereyon.Web;

namespace Shopping.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserHelper _userHelper;
        private readonly DataContext _context;
        private readonly ICombosHelper _combosHelper;
        private readonly IBlogHelper _blobHelper;
        private readonly IMailHelper _mailHelper;
        private readonly IFlashMessage _flashMessage;

        public AccountController(IUserHelper userHelper, DataContext context, ICombosHelper combosHelper, IBlogHelper blobHelper, IMailHelper mailHelper, IFlashMessage flashMessage)
        {
            _userHelper = userHelper;
            _context = context;
            _combosHelper = combosHelper;
            _blobHelper = blobHelper;
            _mailHelper = mailHelper;
            _flashMessage = flashMessage;
        }

        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated) //Me va devolver el usuario siempre logueado
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                SignInResult result = await _userHelper.LoginAsync(model);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                if (result.IsLockedOut)
                {
                    _flashMessage.Danger("Ha superado el máximo número de intentos, su cuenta esta bloqueada, intente de nuevo en 5 minutos.");
                }
                else if(result.IsNotAllowed)
                {
                    _flashMessage.Danger("El usuario no ha sido habilitado, debes de seguir las instrucciones de correo enviado para poder habilitarte en el sistema.");
                }
                else
                {
                    _flashMessage.Danger("Email o contraseña incorrectos.");
                }
            }

            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await _userHelper.LogoutAsync();
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Register()
        {
            AddUserViewModel model = new()
            {
                Id = Guid.Empty.ToString(),
                Countries = await _combosHelper.GetComboCountriesAsync(),
                States = await _combosHelper.GetComboStatesAsync(0),
                Cities = await _combosHelper.GetComboCitiesAsync(0),
                UserType = UserType.User,
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(AddUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
            Guid imageId = Guid.Empty;

            if (model.ImageFile != null)
            {
                imageId = await _blobHelper.UploadBlobAsync(model.ImageFile, "users");
            }

            model.ImageId = imageId;
            User user = await _userHelper.AddUserAsync(model);
            if (user == null) // Si no se puede agregar, el correo ya existe
            {
                _flashMessage.Danger("Este correo ya está siendo usado.");
                model.Countries = await _combosHelper.GetComboCountriesAsync();
                model.States = await _combosHelper.GetComboStatesAsync(model.CountryId);
                model.Cities = await _combosHelper.GetComboCitiesAsync(model.StateId);
                return View(model);
            }

            string myToken = await _userHelper.GenerateEmailConfirmationTokenAsync(user);
            string tokenLink = Url.Action("ConfirmEmail", "Account", new
            {
                userid = user.Id,
                token = myToken
            }, protocol: HttpContext.Request.Scheme);

            Response response = _mailHelper.SendMail(
                $"{model.FirstName} {model.LastName}",
                model.UserName,
                "Shopping - Confirmación de Email",
                $"<h1>Shopping - Confirmación de Email</h1>" +
                    $"Para habilitar el usuario por favor hacer click en el siguiente link:, " +
                    $"<hr><br><p><a href = \"{tokenLink}\">Confirmar Email<a/></p></br></hr>");
            if (response.IsSuccess)
            {
                _flashMessage.Info("Usuario registrado. Para poder ingresar al sistema, siga las instrucciones que han sido enviadas a su correo.");
                return RedirectToAction(nameof(Login));
            }
            
            ModelState.AddModelError(string.Empty, response.Message);

            return View(model);
        }


        public JsonResult GetStates(int countryId)
        {
            Country country = _context.Countries
                .Include(c => c.States)
                .FirstOrDefault(c => c.Id == countryId);
            if (country == null)
            {
                return null;
            }

            return Json(country.States.OrderBy(d => d.Name));
        }

        public JsonResult GetCities(int stateId)
        {
            State State = _context.States
                .Include(s => s.Cities)
                .FirstOrDefault(s => s.Id == stateId);
            if (State == null)
            {
                return null;
            }

            return Json(State.Cities.OrderBy(c => c.Name));
        }


        public async Task<IActionResult> ChangeUser()
        {
            User user = await _userHelper.GetUserAsync(User.Identity.Name);
            if (user == null)
            {
                return NotFound();
            }

            EditUserViewModel model = new()
            {
                Address = user.Address,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                ImageId = user.ImageId,
                Cities = await _combosHelper.GetComboCitiesAsync(user.City.State.Id),
                CityId = user.City.Id,
                Countries = await _combosHelper.GetComboCountriesAsync(),
                CountryId = user.City.State.Country.Id,
                StateId = user.City.State.Id,
                States = await _combosHelper.GetComboStatesAsync(user.City.State.Country.Id),
                Id = user.Id,
                Document = user.Document
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUser(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                Guid imageId = model.ImageId;

                if (model.ImageFile != null)
                {
                    imageId = await _blobHelper.UploadBlobAsync(model.ImageFile, "users");
                }

                User user = await _userHelper.GetUserAsync(User.Identity.Name);

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Address = model.Address;
                user.PhoneNumber = model.PhoneNumber;
                user.ImageId = imageId;
                user.City = await _context.Cities.FindAsync(model.CityId);
                user.Document = model.Document;

                await _userHelper.UpdateUserAsync(user);
                return RedirectToAction("Index", "Home");
            }

            model.Countries = await _combosHelper.GetComboCountriesAsync();
            model.States = await _combosHelper.GetComboStatesAsync(model.CountryId);
            model.Cities = await _combosHelper.GetComboCitiesAsync(model.StateId);
            return View(model);

        }

        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.OldPassword == model.NewPassword)
                {
                    ModelState.AddModelError(string.Empty, "Debes ingresar una contraseña diferente.");
                    return View(model);
                }

                User? user = await _userHelper.GetUserAsync(User.Identity.Name);
                if (user != null)
                {
                    IdentityResult? result = await _userHelper.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("ChangeUser");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, result.Errors.FirstOrDefault().Description);
                    }

                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Usuario no encontrado.");
                }
            }

            return View(model);
        }

        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return NotFound();
            }

            User user = await _userHelper.GetUserAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            IdentityResult result = await _userHelper.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                return NotFound();
            }

            return View();
        }

        public IActionResult RecoverPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RecoverPassword(RecoverPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                User user = await _userHelper.GetUserAsync(model.Email);
                if (user == null)
                {
                    _flashMessage.Danger("El email no corresponde a ningun usuario registrado.");
                    return View();
                }

                string myToken = await _userHelper.GeneratePasswordResetTokenAsync(user);
                string link = Url.Action(
                    "ResetPassword",
                    "Account",
                    new { token = myToken }, protocol: HttpContext.Request.Scheme);
                _mailHelper.SendMail(
                    $"{user.FullName}",
                    model.Email,
                    "Shopping - Recuperación de contraseña",
                    $"<h1>Shopping - Recuperación de contraseña</h1>" +
                    $"Para recuperar la contraseña haga click en el siguiente enlace:" +
                    $"<p><a href=\"{link}\">Reset Password</a></p>");
                _flashMessage.Info("Las instrucciones para recuperar la contraseña han sido enviadas a su correo.");
                return RedirectToAction(nameof(Login));
            }
            return View(model);
        }

		public IActionResult ResetPassword(string token)
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
		{
			User user = await _userHelper.GetUserAsync(model.UserName);
			if (user != null)
			{
				IdentityResult result = await _userHelper.ResetPasswordAsync(user, model.Token, model.Password);
				if (result.Succeeded)
				{
					_flashMessage.Info("Contraseña cambiada con éxito.");
					return RedirectToAction(nameof(Login));
				}

                _flashMessage.Danger("Error cambiando la contraseña.");
				return View(model);
			}

            _flashMessage.Danger("Usuario no encontrado.");
			return View(model);
		}
	}
}