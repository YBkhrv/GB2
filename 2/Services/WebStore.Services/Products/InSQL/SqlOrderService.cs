using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebStore.DAL.Context;
using WebStore.Domain.DTO.Order;
using WebStore.Domain.Entities.Identity;
using WebStore.Domain.Entities.Orders;
using WebStore.Interfaces.Services;
using WebStore.Services.Mapping;

namespace WebStore.Services.Products.InSQL
{
    public class SqlOrderService : IOrderService
    {
        private readonly WebStoreDB _db;
        private readonly UserManager<User> _UserManager;

        public SqlOrderService(WebStoreDB db, UserManager<User> UserManager)
        {
            _db = db;
            _UserManager = UserManager;
        }

        public async Task<OrderDTO> CreateOrder(string UserName, CreateOrderModel OrderModel)
        {
            var user = await _UserManager.FindByNameAsync(UserName);
            if(user is null) throw new InvalidOperationException($"Пользователь {UserName} не найден в БД");

            await using var transaction = await _db.Database.BeginTransactionAsync();

            var order = new Order
            {
                Name = OrderModel.Order.Name,
                Address = OrderModel.Order.Address,
                Phone = OrderModel.Order.Phone,
                User = user,
                Date = DateTime.Now,
                Items = new List<OrderItem>()
            };

            foreach (var item in OrderModel.Items)
            {
                var product = await _db.Products.FindAsync(item.Id);
                if(product is null) throw new InvalidOperationException($"Товар id:{item.Id} не найден в БД");

                var order_item = new OrderItem
                {
                    Order = order,
                    Price = product.Price,
                    Quantity = item.Quantity,
                    Product = product
                };
                order.Items.Add(order_item);
            }

            await _db.Orders.AddAsync(order);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return order.ToDTO();
        }

        public async Task<IEnumerable<OrderDTO>> GetUserOrders(string UserName) => (await _db.Orders
           .Include(order => order.User)
           .Include(order => order.Items)
           .Where(order => order.User.UserName == UserName)
           .ToArrayAsync())
           .Select(p => p.ToDTO());

        public async Task<OrderDTO> GetOrderById(int id) => (await _db.Orders
           .Include(order => order.Items)
           .FirstOrDefaultAsync(order => order.Id == id))
           .ToDTO();
    }
}
