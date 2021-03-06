using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebStore.DAL.Context;
using WebStore.Domain.DTO.Products;
using WebStore.Domain.Entities;
using WebStore.Interfaces.Services;
using WebStore.Services.Mapping;

namespace WebStore.Services.Products.InSQL
{
    public class SqlProductData : IProductData
    {
        private readonly WebStoreDB _db;

        public SqlProductData(WebStoreDB db) => _db = db;

        public IEnumerable<Section> GetSections() => _db.Sections;

        public Section GetSection(int Id) => _db.Sections.Include(s => s.ParentSection).FirstOrDefault(s => s.Id == Id);

        public IEnumerable<Brand> GetBrands() => _db.Brands;

        public Brand GetBrand(int Id) => _db.Brands.Find(Id);
        
        public PageProductsDTO GetProducts(ProductFilter Filter = null)
        {
            IQueryable<Product> query = _db.Products;

            if (Filter?.Ids?.Length > 0)
                query = query.Where(product => Filter.Ids.Contains(product.Id));
            else
            {
                if (Filter?.BrandId != null)
                    query = query.Where(product => product.BrandId == Filter.BrandId);

                if (Filter?.SectionId != null)
                    query = query.Where(product => product.SectionId == Filter.SectionId);
            }

            var total_count = query.Count();

            if (Filter?.PageSize > 0)
                query = query
                   .Skip((Filter.Page - 1) * (int) Filter.PageSize)
                   .Take((int) Filter.PageSize);

            return new PageProductsDTO
            {
                Products = query.Select(p => p.ToDTO()),
                TotalCount = total_count
            };
        }

        public ProductDTO GetProductById(int id) => _db.Products
           .Include(p => p.Section)
           .Include(p => p.Brand)
           .FirstOrDefault(p => p.Id == id)
           .ToDTO();
    }
}
