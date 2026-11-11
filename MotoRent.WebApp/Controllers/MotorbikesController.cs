using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MotoRent.WebApp.Models;
using MotoRent.WebApp.Services;

namespace MotoRent.WebApp.Controllers;

public class MotorbikesController : Controller
{
    private const int PageSize = 9;

    private readonly MotorbikeApiClient _api;
    private readonly ReviewApiClient _reviewApi;
    private readonly ILogger<MotorbikesController> _logger;

    public MotorbikesController(MotorbikeApiClient api, ReviewApiClient reviewApi, ILogger<MotorbikesController> logger)
    {
        _api = api;
        _reviewApi = reviewApi;
        _logger = logger;
    }

    // GET /Motorbikes?keyword=&brandId=&categoryId=&minPrice=&maxPrice=&onlyAvailable=&sort=&page=
    public async Task<IActionResult> Index([FromQuery] MotorbikeFilterViewModel filter)
    {
        var model = new MotorbikeListViewModel { Filter = filter, PageSize = PageSize };

        if (filter.MinPrice > filter.MaxPrice)
            ModelState.AddModelError(nameof(filter.MaxPrice), "Giá đến phải lớn hơn giá từ.");

        try
        {
            var brands = await _api.GetBrandsAsync();
            var categories = await _api.GetCategoriesAsync();
            model.BrandOptions = brands.Select(b => new SelectListItem(b.BrandName, b.BrandId.ToString())).ToList();
            model.CategoryOptions = categories.Select(c => new SelectListItem(c.CategoryName, c.CategoryId.ToString())).ToList();

            if (ModelState.IsValid)
            {
                (model.Items, model.TotalCount) = await _api.SearchAsync(filter, PageSize);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Không gọi được API");
            model.ErrorMessage = "Không kết nối được tới máy chủ API. Hãy chắc chắn MotoRent.API đang chạy.";
        }

        return View(model);
    }

    // GET /Motorbikes/Details/5?startDate=...&endDate=...
    public async Task<IActionResult> Details(int id, DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var bike = await _api.GetByIdAsync(id);
            if (bike == null) return NotFound();

            var reviewsResult = await _reviewApi.GetByMotorbikeAsync(id);

            var model = new MotorbikeDetailViewModel
            {
                Motorbike = bike,
                StartDate = startDate,
                EndDate = endDate,
                SimilarMotorbikes = await _api.GetSimilarAsync(bike.CategoryId, bike.MotorbikeId),
                Reviews = reviewsResult.Data ?? new()
            };

            if (startDate.HasValue && endDate.HasValue)
            {
                (var isAvailable, model.AvailabilityError) = await _api.CheckAvailabilityAsync(bike, startDate.Value, endDate.Value);
                if (model.AvailabilityError == null) model.IsAvailable = isAvailable;
            }

            return View(model);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Không gọi được API");
            TempData["Error"] = "Không kết nối được tới máy chủ API.";
            return RedirectToAction(nameof(Index));
        }
    }
}