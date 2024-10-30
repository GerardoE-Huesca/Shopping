using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shopping.Data;
using Shopping.Data.Entities;
using Shopping.Data.Enums;
using Shopping.Helpers;
using Shopping.Models;

namespace Shopping.Controllers
{
    //[Authorize(Roles = "Admin")]
	public class UsersController : Controller
	{
		private readonly DataContext _context;
        private readonly IUserHelper _userHelper;
        private readonly IBlogHelper _blogHelper;
        private readonly ICombosHelper _combosHelper;

        public UsersController(DataContext context, IUserHelper userHelper, IBlogHelper blogHelper, ICombosHelper combosHelper )
        {
			_context = context;
            _userHelper = userHelper;
            _blogHelper = blogHelper;
            _combosHelper = combosHelper;
        }

		public async Task<IActionResult> Index()
		{
		return View(await _context.Users
			.Include(u => u.City)
			.ThenInclude(c => c.State)
			.ThenInclude(s => s.Country)
			.ToListAsync());
		}

        public async Task<IActionResult> Create()
        {
            AddUserViewModel model = new()
            {
                Id = Guid.Empty.ToString(),
                Countries = await _combosHelper.GetComboCountriesAsync(),
                States = await _combosHelper.GetComboStatesAsync(0),
                Cities = await _combosHelper.GetComboCitiesAsync(0),
                UserType = UserType.Admin,
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                Guid imageId = Guid.Empty;

                if (model.ImageFile != null)
                {
                    imageId = await _blogHelper.UploadBlobAsync(model.ImageFile, "users");
                }

                model.ImageId = imageId;
                User user = await _userHelper.AddUserAsync(model);
                if (user == null) // Si no se puede agregar, el correo ya existe
                {
                    ModelState.AddModelError(string.Empty, "Este correo ya está siendo usado.");
                    model.Countries = await _combosHelper.GetComboCountriesAsync();
                    model.States = await _combosHelper.GetComboStatesAsync(model.CountryId);
                    model.Cities = await _combosHelper.GetComboCitiesAsync(model.StateId);
                    return View(model);
                }

                return RedirectToAction("Index", "Home"); // Redirige al Index si el login es exitoso
            }

            model.Countries = await _combosHelper.GetComboCountriesAsync();
            model.States = await _combosHelper.GetComboStatesAsync(model.CountryId);
            model.Cities = await _combosHelper.GetComboCitiesAsync(model.StateId);
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

    }
}
