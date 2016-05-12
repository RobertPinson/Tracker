using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Homeworld.Tracker.Web.Models;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using System.Web;
using Microsoft.AspNet.Http;
using System.Linq;
using System.Net.Mime;
using ImageProcessor;
using ImageProcessor.Imaging;
using Microsoft.AspNet.Hosting;
using Microsoft.Net.Http.Headers;
using System.Drawing;
using ImageProcessor.Imaging.Formats;
using ImageProcessor.Processors;

namespace Homeworld.Tracker.Web.Controllers
{
    public class PeopleController : Controller
    {
        private readonly TrackerDbContext _context;
        private readonly IHostingEnvironment _environment;

        public PeopleController(TrackerDbContext context, IHostingEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        #region ---IMAGE UPLOAD ------

        private const int AvatarStoredWidth = 200;  // ToDo - Change the size of the stored avatar image
        private const int AvatarStoredHeight = 200; // ToDo - Change the size of the stored avatar image
        private const int AvatarScreenWidth = 400;  // ToDo - Change the value of the width of the image on the screen

        private const string TempFolder = "Temp";
        private readonly string[] _imageFileExtensions = { ".jpg", ".png", ".gif", ".jpeg" };

        [HttpGet]
        public ActionResult Upload()
        {
            return PartialView("_Upload");
        }

        [HttpPost]
        public ActionResult Upload(IFormFile files)
        {
            if (files == null) return Json(new { success = false, errorMessage = "No file uploaded." });

            var file = files;

            if (!IsImage(file)) return Json(new { success = false, errorMessage = "File is of wrong format." });

            if (file.Length <= 0) return Json(new { success = false, errorMessage = "File cannot be zero length." });

            var webPath = GetTempSavedFilePath(file).Replace("/", "\\");

            return Json(new { success = true, fileName = webPath }); // success
        }

        [HttpPost]
        public ActionResult Save(int id, string cropPointY, string cropPointX, string imageCropHeight, string imageCropWidth, string imagePath)
        {
            try
            {
                //Get person record to save image to
                var person = _context.Person.FirstOrDefault(p => p.Id == id);

                if (person == null)
                {
                    return Json(new { success = false, errorMessage = "Unable to upload file.\nERRORINFO: Person not found" });
                }

                // Calculate dimensions
                var top = Convert.ToInt32(cropPointY);
                var left = Convert.ToInt32(cropPointX);
                var height = Convert.ToInt32(imageCropHeight);
                var width = Convert.ToInt32(imageCropWidth);

                // Get file from temporary folder
                var tempPath = Path.Combine(_environment.WebRootPath, "temp");
                var fileName = Path.GetFileName(imagePath);

                if (fileName == null)
                {
                    return Json(new { success = false, errorMessage = "Unable to upload file.\nERRORINFO: Image File name null" });
                }

                var fn = Path.Combine(tempPath, fileName);
                // ...get image and resize it, ...
                using (var imageFactory = new ImageFactory())
                using (var fileStream = new FileStream(fn, FileMode.Open))
                using (var cropedStream = new MemoryStream())
                {
                    imageFactory.FixGamma = false;
                    imageFactory.Load(fileStream)
                        .Crop(new Rectangle(left, top, width, height))
                         .Resize(new Size(AvatarStoredWidth, AvatarStoredHeight))
                        .Format(new JpegFormat { Quality = 100 })
                        .Quality(100)
                        .Save(cropedStream);

                    var image = cropedStream.ToArray();

                    person.Image = image;

                    _context.SaveChanges();
                }

                return Json(new { success = true, avatar = Convert.ToBase64String(person.Image) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, errorMessage = "Unable to upload file.\nERRORINFO: " + ex.Message });
            }
        }

        private bool IsImage(IFormFile file)
        {
            if (file == null) return false;
            return file.ContentType.Contains("image") ||
                _imageFileExtensions.Any(item =>
                ContentDispositionHeaderValue.Parse(file.ContentDisposition)
                .FileName.EndsWith(item, StringComparison.OrdinalIgnoreCase));
        }

        private string GetTempSavedFilePath(IFormFile file)
        {
            // Define destination
            var tempPath = Path.Combine(_environment.WebRootPath, "temp");
            if (Directory.Exists(tempPath) == false)
            {
                Directory.CreateDirectory(tempPath);
            }

            // Generate unique file name
            var filePath = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
            var fileName = Path.GetFileName(filePath);

            fileName = SaveTemporaryAvatarFileImage(file, tempPath, fileName);

            // Clean up old files after every save
            CleanUpTempFolder(1);
            return Path.Combine($"\\{TempFolder}", fileName.Replace("\\", "/"));
        }

        private static string SaveTemporaryAvatarFileImage(IFormFile file, string serverPath, string fileName)
        {
            var fullFileName = Path.Combine(serverPath, fileName);
            if (System.IO.File.Exists(fullFileName))
            {
                System.IO.File.Delete(fullFileName);
            }

            var imageFactory = new ImageFactory();

            var imgFactory = imageFactory.Load(file.OpenReadStream());
            var image = imgFactory.Image;
            var ratio = image.Height / (double)image.Width;
            imgFactory.Resize(new Size(AvatarScreenWidth, (int)(AvatarScreenWidth * ratio))).Save(fullFileName);

            return fileName;
        }

        private void CleanUpTempFolder(int hoursOld)
        {
            try
            {
                var currentUtcNow = DateTime.UtcNow;
                var tempPath = Path.Combine(_environment.WebRootPath, "temp");
                if (!Directory.Exists(tempPath)) return;
                var fileEntries = Directory.GetFiles(tempPath);

                foreach (var fileEntry in fileEntries)
                {
                    var fileCreationTime = System.IO.File.GetCreationTimeUtc(fileEntry);
                    var res = currentUtcNow - fileCreationTime;
                    if (res.TotalHours > hoursOld)
                    {
                        System.IO.File.Delete(fileEntry);
                    }
                }
            }
            catch
            {
                // Deliberately empty.
            }
        }



        #endregion

        // GET: People
        [Produces("application/json")]
        [Route("api/people")]
        public async Task<IActionResult> Get(string excludeIds, string deviceId)
        {

            var data = await _context.Person.Include(p => p.PersonCards).ThenInclude(c => c.Card).ToListAsync();

            dynamic res = data.Select(MapToResponse);

            return Ok(new {People = res});
        }

        private object MapToResponse(Person person)
        {
            var personCard = person.PersonCards.FirstOrDefault(c => c.Card.IsDeleted == false);
            var cardId = personCard == null ? string.Empty : personCard.Card.Uid;

            return new
            {
                Id = person.Id,
                Name = $"{person.FirstName} {person.LastName}",
                Image = person.Image,
                CardId = cardId
            };
        }

        // GET: People
        public async Task<IActionResult> Index()
        {
            return View(await _context.Person.ToListAsync());
        }

        // GET: People/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

            var person = await _context.Person.SingleAsync(m => m.Id == id);
            if (person == null)
            {
                return HttpNotFound();
            }

            return View(person);
        }

        // GET: People/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: People/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Person person)
        {
            if (ModelState.IsValid)
            {
                _context.Person.Add(person);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(person);
        }

        // GET: People/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

            var person = await _context.Person.SingleAsync(m => m.Id == id);
            if (person == null)
            {
                return HttpNotFound();
            }
            return View(person);
        }

        // POST: People/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Person person)
        {
            if (ModelState.IsValid)
            {
                var existPerson = await _context.Person.SingleAsync(p => p.Id == person.Id);

                if (existPerson == null)
                {
                    return HttpNotFound();
                }

                existPerson.FirstName = person.FirstName;
                existPerson.LastName = person.LastName;
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(person);
        }

        // GET: People/Delete/5
        [ActionName("Delete")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

            var person = await _context.Person.SingleAsync(m => m.Id == id);
            if (person == null)
            {
                return HttpNotFound();
            }

            return View(person);
        }

        // POST: People/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Person person = await _context.Person.SingleAsync(m => m.Id == id);
            _context.Person.Remove(person);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<FileContentResult> GetAvatar(int id)
        {
            var person = await _context.Person.SingleAsync(p => p.Id == id);
            var byteArray = person.Image;
            return byteArray != null
                ? new FileContentResult(byteArray, "image/jpeg")
                : null;
        }
    }
}