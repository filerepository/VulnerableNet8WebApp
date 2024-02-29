using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VulnerableAppNet8.Data;
using VulnerableAppNet8.Models;

namespace VulnerableAppNet8.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            var products = from m in _context.Products
                           select m;

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(s => s.Name.Contains(searchString) || s.Description.Contains(searchString));
            }

            ViewBag.SearchFilter = searchString?.Replace("<", "").Replace(">", "").Replace("javascript", "", StringComparison.OrdinalIgnoreCase);
            ViewBag.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return View(await products.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CreatedByUserId,Name,Price,Description")] Product product) // Binding too many properties (http://go.microsoft.com/fwlink/?LinkId=317598)
        {
            product.CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            if (product.CreatedByUserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return View("Forbidden");
            }

            return View(product);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Price,Description,CreatedByUserId")] Product product) // Binding too many properties (http://go.microsoft.com/fwlink/?LinkId=317598)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            //no check that the product is created by the current user :(

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Entry(product).Property(x => x.CreatedByUserId).IsModified = false;
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            if (product.CreatedByUserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return View("Forbidden");
            }

            return View(product);
        }

        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product.CreatedByUserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return View("Forbidden");
            }

            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        [Authorize]
        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return View("UploadXml", "File not selected or is empty.");
            }

            if (!file.ContentType.Equals("text/xml", StringComparison.OrdinalIgnoreCase))
            {
                return View("UploadXml", "Please upload a valid XML file.");
            }

            XDocument xmlDoc;
            try
            {
                using (var stream = file.OpenReadStream())
                {
                    // Create an XmlReaderSettings object with dangerous DTD processing and XmlUrlResolver enabled
                    XmlReaderSettings settings = new XmlReaderSettings
                    {
                        DtdProcessing = DtdProcessing.Parse, 
                        XmlResolver = new XmlUrlResolver()                        
                };

                    using (XmlReader reader = XmlReader.Create(stream, settings))
                    {
                        xmlDoc = XDocument.Load(reader);
                    }
                }


                var products = from p in xmlDoc.Descendants("Product")
                               select new Product
                               {
                                   Name = p.Element("Name").Value,
                                   Price = decimal.Parse(p.Element("Price").Value),
                                   Description = p.Element("Description").Value
                               };

                foreach (var product in products)
                {
                    _context.Products.Add(product);
                }
                await _context.SaveChangesAsync();


                return RedirectToAction("Index"); 
            }
            catch (XmlException ex)
            {
                return View("UploadXml", $"Error processing XML file: {ex.Message}");
            }


        }
    }
}
