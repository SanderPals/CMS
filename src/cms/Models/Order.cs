using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Site.Data;
using Site.Helper.Pagination;
using Site.Models.Product;
using Site.Models.Setting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;

namespace Site.Models
{
    public partial class Order : Controller
    {
        private readonly SiteContext _context;
        private readonly IDataProtectionProvider _provider;

        public Order(SiteContext context, IDataProtectionProvider provider = null)
        {
            _context = context;
            _provider = provider;
        }

        private decimal _total;

        public Commerce Commerce;
        public string Currency;
        public int DigitsAfterDecimal;
        public decimal Tax = 0.00m;
        public Dictionary<string, object> TaxClasses = new Dictionary<string, object>();
        public decimal ShippingTax;

        public class OrderBundle
        {
            public Orders Order { get; set; }
            public IEnumerable<OrderCoupons> OrderCoupons { get; set; }
            public IEnumerable<OrderFees> OrderFees { get; set; }
            public IEnumerable<OrderLines> OrderLines { get; set; }
            public IEnumerable<OrderRefundLines> OrderRefundLines { get; set; }
            public IEnumerable<OrderRefunds> OrderRefunds { get; set; }
            public IEnumerable<OrderShippingZoneMethods> OrderShippingZoneMethods { get; set; }
        }

        public async Task<Dictionary<string, object>> GetOrderAsync(int id, int websiteId)
        {
            if (id == 0)
            {
                SettingClient setting = new SettingClient(_context);
                id = InsertOrder(EncryptOrder(new Orders()
                {
                    CreatedDate = DateTime.Now,
                    Currency = setting.GetSettingValueByKey("currency", "website", websiteId),
                    //OrderNumber = setting.GetSettingValueByKey("orderPrefix", "website", websiteId) + _setting.Value + setting.GetSettingValueByKey("orderSuffix", "website", websiteId),
                    Status = "concept",
                    WebsiteId = websiteId
                })).Id;
            }

            return ConvertOrderToJson(GetOrderBundleByWebsiteId(id, websiteId), websiteId);
        }

        public List<Orders> GetOrdersByWebsiteId(int websiteId)
        {
            return _context.Orders.Where(Order => Order.WebsiteId == websiteId && Order.Status.ToLower() != "reserved")
                                  .OrderByDescending(Order => Order.Id)
                                  .ToList();
        }

        public OrderBundle GetOrderBundleByWebsiteId(int id, int websiteId)
        {
            return _context.Orders.GroupJoin(_context.OrderCoupons.OrderBy(OrderCoupon => OrderCoupon.Name), Order => Order.Id, OrderCoupon => OrderCoupon.OrderId, (Order, OrderCoupon) => new { Order, OrderCoupon })
                                  .GroupJoin(_context.OrderFees.OrderBy(OrderFee => OrderFee.Name), x => x.Order.Id, OrderFee => OrderFee.OrderId, (x, OrderFee) => new { x.Order, x.OrderCoupon, OrderFee })
                                  .GroupJoin(_context.OrderLines.OrderBy(OrderLine => OrderLine.Name), x => x.Order.Id, OrderLine => OrderLine.OrderId, (x, OrderLine) => new { x.Order, x.OrderCoupon, x.OrderFee, OrderLine })
                                  .GroupJoin(_context.OrderRefundLines.Join(_context.OrderLines, OrderRefundLine => OrderRefundLine.OrderLineId, OrderLine => OrderLine.Id, (OrderRefundLine, OrderLine) => new { OrderRefundLine, OrderLine }).Select(x => x.OrderRefundLine), x => x.Order.Id, OrderRefundLine => OrderRefundLine.OrderLineId, (x, OrderRefundLine) => new { x.Order, x.OrderCoupon, x.OrderFee, x.OrderLine, OrderRefundLine })
                                  .GroupJoin(_context.OrderRefunds, x => x.Order.Id, OrderRefund => OrderRefund.OrderId, (x, OrderRefund) => new { x.Order, x.OrderCoupon, x.OrderFee, x.OrderLine, x.OrderRefundLine, OrderRefund })
                                  .GroupJoin(_context.OrderShippingZoneMethods.OrderBy(OrderShippingZoneMethod => OrderShippingZoneMethod.Name), x => x.Order.Id, OrderShippingZoneMethod => OrderShippingZoneMethod.OrderId, (x, OrderShippingZoneMethod) => new { x.Order, x.OrderCoupon, x.OrderFee, x.OrderLine, x.OrderRefundLine, x.OrderRefund, OrderShippingZoneMethod })
                                  .Select(x => new OrderBundle()
                                  {
                                      Order = x.Order,
                                      OrderCoupons = x.OrderCoupon,
                                      OrderFees = x.OrderFee,
                                      OrderLines = x.OrderLine,
                                      OrderRefundLines = x.OrderRefundLine,
                                      OrderRefunds = x.OrderRefund,
                                      OrderShippingZoneMethods = x.OrderShippingZoneMethod,
                                  })
                                  .FirstOrDefault(OrderBundle => OrderBundle.Order.WebsiteId == websiteId && OrderBundle.Order.Id == id && OrderBundle.Order.Status.ToLower() != "reserved");
        }

        public ObjectResult UpdateOrInsertOrderShippingZoneMethod(OrderShippingZoneMethods orderShippingZoneMethod, int id)
        {
            ResourceManager resourceManager = new ResourceManager("Site.Resources.Models.Order", Assembly.GetExecutingAssembly());

            if (id != 0)
            {
                UpdateOrderShippingZoneMethod(orderShippingZoneMethod);
            }
            else
            {
                orderShippingZoneMethod = InsertOrderShippingZoneMethod(orderShippingZoneMethod.Name, orderShippingZoneMethod.OrderId, orderShippingZoneMethod.Price, orderShippingZoneMethod.ShippingZoneMethodId);
            }

            return Ok(Json(new
            {
                item = orderShippingZoneMethod,
                messageType = "success",
                message = (id == 0) 
                ? resourceManager.GetString("ShippingAdded") 
                : resourceManager.GetString("ShippingUpdated")
            }));
        }

        public ObjectResult UpdateOrInsertOrderFee(OrderFees orderFee, int id)
        {
            ResourceManager resourceManager = new ResourceManager("Site.Resources.Models.Order", Assembly.GetExecutingAssembly());

            if (id != 0)
            {
                UpdateOrderFee(orderFee);
            }
            else
            {
                orderFee = InsertOrderFee(orderFee.Name, orderFee.OrderId, orderFee.Price);
            }

            return Ok(Json(new
            {
                item = orderFee,
                messageType = "success",
                message = (id == 0) 
                ? resourceManager.GetString("FeeAdded")
                : resourceManager.GetString("FeeUpdated")
            }));
        }

        public ObjectResult UpdateOrInsertOrderLine(OrderLines orderLine, int id)
        {
            ResourceManager resourceManager = new ResourceManager("Site.Resources.Models.Order", Assembly.GetExecutingAssembly());

            if (id != 0)
            {
                UpdateOrderLine(orderLine);
            }
            else
            {
                orderLine = InsertOrderLine(orderLine.Name, orderLine.Discount, orderLine.OrderId, orderLine.Price, orderLine.ProductId, orderLine.Quantity, orderLine.TaxRate);
            }

            return Ok(Json(new
            {
                item = orderLine,
                messageType = "success",
                message = (id == 0) 
                ? resourceManager.GetString("OrderLineAdded")
                : resourceManager.GetString("OrderLineUpdated")
            }));
        }

        public async Task<ObjectResult> UpdateOrInsertOrderAsync(Orders order, int id, int websiteId)
        {
            ResourceManager resourceManager = new ResourceManager("Site.Resources.Models.Order", Assembly.GetExecutingAssembly());

            if (id != 0)
            {
                await new Commerce(_context, _provider).RegisterOrderToAsync(order, websiteId);

                if (order.Status.ToLower() == "completed")
                {
                    if (string.IsNullOrWhiteSpace(order.InvoiceNumber))
                    {
                        SettingClient setting = new SettingClient(_context);
                        Settings _setting = await setting.IncrementSettingValueByKeyAsync("invoiceCurrent", "website", websiteId);
                        order.InvoiceNumber = setting.GetSettingValueByKey("invoicePrefix", "website", websiteId) + _setting.Value + setting.GetSettingValueByKey("invoiceSuffix", "website", websiteId);
                    }
                }

                if (order.Status.ToLower() == "credit")
                {
                    if (string.IsNullOrWhiteSpace(order.InvoiceNumber))
                    {
                        SettingClient setting = new SettingClient(_context);
                        Settings _setting = await setting.IncrementSettingValueByKeyAsync("invoiceCurrent", "website", websiteId);
                        order.InvoiceNumber = setting.GetSettingValueByKey("creditPrefix", "website", websiteId) + _setting.Value + setting.GetSettingValueByKey("creditSuffix", "website", websiteId);
                    }
                }

                if (order.Status.ToLower() != "credit" && order.Status.ToLower() != "concept")
                {
                    if (string.IsNullOrWhiteSpace(order.OrderNumber))
                    {
                        SettingClient setting = new SettingClient(_context);
                        Settings _setting = await setting.IncrementSettingValueByKeyAsync("orderCurrent", "website", websiteId);
                        order.OrderNumber = setting.GetSettingValueByKey("orderPrefix", "website", websiteId) + _setting.Value + setting.GetSettingValueByKey("orderSuffix", "website", websiteId);
                    }
                }

                UpdateOrder(EncryptOrder(order));              
            }
            else
            {
                order = InsertOrder(EncryptOrder(order));
            }

            return Ok(Json(new
            {
                item = DecryptOrder(order),
                messageType = "success",
                message = (id == 0) 
                ? resourceManager.GetString("OrderAdded")
                : resourceManager.GetString("OrderUpdated") 
            }));
        }

        public void UpdateOrderShippingZoneMethod(OrderShippingZoneMethods orderShippingZoneMethod)
        {
            _context.OrderShippingZoneMethods.Update(orderShippingZoneMethod);
            _context.SaveChanges();
        }

        public void UpdateOrderFee(OrderFees orderFee)
        {
            _context.OrderFees.Update(orderFee);
            _context.SaveChanges();
        }

        public void UpdateOrderLine(OrderLines orderLine)
        {
            _context.OrderLines.Update(orderLine);
            _context.SaveChanges();
        }

        public void UpdateOrder(Orders order)
        {
            _context.Orders.Update(order);
            _context.SaveChanges();
        }

        public OrderShippingZoneMethods InsertOrderShippingZoneMethod(string name, int orderId, decimal price, int shippingZoneMethodId)
        {
            OrderShippingZoneMethods _orderShippingZoneMethod = new OrderShippingZoneMethods { Name = name, OrderId = orderId, Price = price, ShippingZoneMethodId = shippingZoneMethodId };
            _context.OrderShippingZoneMethods.Add(_orderShippingZoneMethod);
            _context.SaveChanges();

            return _orderShippingZoneMethod;
        }

        public OrderFees InsertOrderFee(string name, int orderId, decimal price)
        {
            OrderFees _orderFee = new OrderFees { Name = name, OrderId = orderId, Price = price};
            _context.OrderFees.Add(_orderFee);
            _context.SaveChanges();

            return _orderFee;
        }

        public OrderLines InsertOrderLine(string name, decimal discount, int orderId, decimal price, int productId, int quantity, decimal taxRate)
        {
            OrderLines _orderLine = new OrderLines { Name = name, Discount = discount, OrderId = orderId, Price = price, ProductId = productId, Quantity = quantity, TaxRate = taxRate };
            _context.OrderLines.Add(_orderLine);
            _context.SaveChanges();

            return _orderLine;
        }


        public Orders InsertOrder(Orders order)
        {
            _context.Orders.Add(order);
            _context.SaveChanges();

            return order;
        }

        public void DeleteOrderShippingZoneMethodById(int id)
        {
            OrderShippingZoneMethods _orderShippingZoneMethod = _context.OrderShippingZoneMethods.FirstOrDefault(OrderShippingZoneMethod => OrderShippingZoneMethod.Id == id);
            _context.OrderShippingZoneMethods.Remove(_orderShippingZoneMethod);
        }

        public void DeleteOrderFeeById(int id)
        {
            OrderFees _orderFee = _context.OrderFees.FirstOrDefault(OrderFee => OrderFee.Id == id);
            _context.OrderFees.Remove(_orderFee);
        }

        public void DeleteOrderLineById(int id)
        {
            OrderLines _orderLine = _context.OrderLines.FirstOrDefault(OrderLine => OrderLine.Id == id);
            _context.OrderLines.Remove(_orderLine);
        }

        public ObjectResult DeleteOrder(int id)
        {
            ResourceManager resourceManager = new ResourceManager("Site.Resources.Models.Order", Assembly.GetExecutingAssembly());

            DeleteOrderById(id);

            return Ok(Json(new
            {
                messageType = "success",
                message = resourceManager.GetString("OrderDeleted")
            }));
        }

        public void DeleteOrderById(int id)
        {
            Orders _order = _context.Orders.FirstOrDefault(Order => Order.Id == id);
            _context.Orders.Remove(_order);
            _context.SaveChanges();
        }

        public ObjectResult DeleteOrderFeesAndShippingsAndLines(Dictionary<string, List<String>> selection)
        {
            ResourceManager resourceManager = new ResourceManager("Site.Resources.Models.Order", Assembly.GetExecutingAssembly());

            if (selection.ContainsKey("fees"))
            {
                foreach (string id in selection["fees"])
                {
                    DeleteOrderFeeById(Int32.Parse(id));
                }
            }

            if (selection.ContainsKey("shippings"))
            {
                foreach (string id in selection["shippings"])
                {
                    DeleteOrderShippingZoneMethodById(int.Parse(id));
                }
            }

            if (selection.ContainsKey("lines"))
            {
                foreach (string id in selection["lines"])
                {
                    DeleteOrderLineById(int.Parse(id));
                }
            }

            _context.SaveChanges();

            return Ok(Json(new
            {
                messageType = "success",
                message = resourceManager.GetString("SelectionDeleted")
            }));
        }

        public ObjectResult ConvertOrdersToJson(List<Orders> orders, int pageNumber, int pageSize)
        {
            PagedData<Orders> pagedData = Pagination.PagedResult(orders, pageNumber, pageSize);

            Encryptor encryptor = new Encryptor(_provider);

            List<Dictionary<string, object>> items = new List<Dictionary<string, object>>();
            foreach (Orders order in pagedData.Data)
            {
                Dictionary<string, object> item = new Dictionary<string, object>
                {
                    { "id", order.Id },
                    { "orderNumber", order.OrderNumber },
                    { "name", encryptor.Decrypt(order.BillingFirstName) + " " + encryptor.Decrypt(order.BillingLastName)  },
                    { "status", GetStatus(order.Status) },
                    { "statusClass", order.Status },
                    { "date", order.CreatedDate.ToString("dd MMMM yyyy, HH:mm:ss") },
                    { "transaction", order.TransactionId }
                };

                items.Add(item);
            }

            return Ok(Json(new Dictionary<string, object>() {
                { "data", items },
                { "currentPage", pagedData.CurrentPage },
                { "totalPages", pagedData.TotalPages }
            }));
        }

        public Dictionary<string, object> ConvertOrderToJson(OrderBundle orderBundle, int websiteId)
        {
            SettingClient setting = new SettingClient(_context);
            Commerce = new Commerce(_context);
            Commerce.SetPriceFormatVariables(websiteId);
            DigitsAfterDecimal = Int32.Parse(setting.GetSettingValueByKey("digitsAfterDecimal", "website", websiteId));
            decimal totalFee = GetTotalFeePrice(orderBundle.OrderFees, DigitsAfterDecimal);
            decimal totalShipping = GetTotalShippingCosts(orderBundle.OrderShippingZoneMethods, orderBundle.OrderLines, orderBundle.OrderFees, DigitsAfterDecimal);
            decimal totalLine = GetTotalLinePrice(orderBundle.OrderLines, DigitsAfterDecimal);
            //decimal totalDiscount = GetTotalCouponPrice(orderBundle.OrderCoupons, digitsAfterDecimal);
            decimal totalOrder = ((totalFee + totalShipping) + totalLine);// - totalDiscount;

            return new Dictionary<string, object>
            {
                { "digitsAfterDecimal", new SettingClient(_context).GetSettingValueByKey("digitsAfterDecimal", "website", websiteId) },
                { "priceFormat", Commerce.GetPriceFormat("price", orderBundle.Order.Currency) },
                { "totalShipping", totalShipping },
                { "totalFee", totalFee },
                { "totalTax", Tax },
                //{ "totalDiscount", GetPriceFormat(totalDiscount.ToString(), orderBundle.Order.Currency) },
                { "total", totalOrder },
                { "item", DecryptOrder(orderBundle.Order) },
                { "coupons", orderBundle.OrderCoupons },
                { "fees", orderBundle.OrderFees },
                { "lines", ConvertOrderLinesToJson(orderBundle) },
                { "refunds", orderBundle.OrderRefunds },
                { "shippings", orderBundle.OrderShippingZoneMethods }
            };
        }

        public Orders DecryptOrder(Orders order)
        {
            Encryptor encryptor = new Encryptor(_provider);
            order.BillingAddressLine1 = encryptor.Decrypt(order.BillingAddressLine1);
            order.BillingAddressLine2 = encryptor.Decrypt(order.BillingAddressLine2);
            order.BillingCity = encryptor.Decrypt(order.BillingCity);
            order.BillingCompany = encryptor.Decrypt(order.BillingCompany);
            order.BillingCountry = encryptor.Decrypt(order.BillingCountry);
            order.BillingEmail = encryptor.Decrypt(order.BillingEmail);
            order.BillingFirstName = encryptor.Decrypt(order.BillingFirstName);
            order.BillingLastName = encryptor.Decrypt(order.BillingLastName);
            order.BillingPhoneNumber = encryptor.Decrypt(order.BillingPhoneNumber);
            order.BillingState = encryptor.Decrypt(order.BillingState);
            order.BillingVatNumber = encryptor.Decrypt(order.BillingVatNumber);
            order.BillingZipCode = encryptor.Decrypt(order.BillingZipCode);
            order.ShippingAddressLine1 = encryptor.Decrypt(order.ShippingAddressLine1);
            order.ShippingAddressLine2 = encryptor.Decrypt(order.ShippingAddressLine2);
            order.ShippingCity = encryptor.Decrypt(order.ShippingCity);
            order.ShippingCompany = encryptor.Decrypt(order.ShippingCompany);
            order.ShippingCountry = encryptor.Decrypt(order.ShippingCountry);
            order.ShippingFirstName = encryptor.Decrypt(order.ShippingFirstName);
            order.ShippingLastName = encryptor.Decrypt(order.ShippingLastName);
            order.ShippingState = encryptor.Decrypt(order.ShippingState);
            order.ShippingZipCode = encryptor.Decrypt(order.ShippingZipCode);

            return order;
        }

        public Orders EncryptOrder(Orders order)
        {
            Encryptor encryptor = new Encryptor(_provider);
            order.BillingAddressLine1 = encryptor.Encrypt(order.BillingAddressLine1);
            order.BillingAddressLine2 = encryptor.Encrypt(order.BillingAddressLine2);
            order.BillingCity = encryptor.Encrypt(order.BillingCity);
            order.BillingCompany = encryptor.Encrypt(order.BillingCompany);
            order.BillingCountry = encryptor.Encrypt(order.BillingCountry);
            order.BillingEmail = encryptor.Encrypt(order.BillingEmail);
            order.BillingFirstName = encryptor.Encrypt(order.BillingFirstName);
            order.BillingLastName = encryptor.Encrypt(order.BillingLastName);
            order.BillingPhoneNumber = encryptor.Encrypt(order.BillingPhoneNumber);
            order.BillingState = encryptor.Encrypt(order.BillingState);
            order.BillingVatNumber = encryptor.Encrypt(order.BillingVatNumber);
            order.BillingZipCode = encryptor.Encrypt(order.BillingZipCode);
            order.ShippingAddressLine1 = encryptor.Encrypt(order.ShippingAddressLine1);
            order.ShippingAddressLine2 = encryptor.Encrypt(order.ShippingAddressLine2);
            order.ShippingCity = encryptor.Encrypt(order.ShippingCity);
            order.ShippingCompany = encryptor.Encrypt(order.ShippingCompany);
            order.ShippingCountry = encryptor.Encrypt(order.ShippingCountry);
            order.ShippingFirstName = encryptor.Encrypt(order.ShippingFirstName);
            order.ShippingLastName = encryptor.Encrypt(order.ShippingLastName);
            order.ShippingState = encryptor.Encrypt(order.ShippingState);
            order.ShippingZipCode = encryptor.Encrypt(order.ShippingZipCode);


            return order;
        }

        public List<Dictionary<string, object>> ConvertOrderLinesToJson(OrderBundle orderBundle)
        {
            List<Dictionary<string, object>> items = new List<Dictionary<string, object>>();
            foreach (OrderLines orderLine in orderBundle.OrderLines)
            {
                Dictionary<string, object> item = new Dictionary<string, object>
                {
                    { "id", orderLine.Id },
                    { "productId", orderLine.ProductId },
                    { "name", orderLine.Name },
                    { "price", orderLine.Price },
                    { "quantity", orderLine.Quantity },
                    { "total", (orderLine.Price * orderLine.Quantity) },
                    { "tax", orderLine.TaxRate.ToString("G29") },
                    { "discount", orderLine.Discount },
                    { "product", new ProductClient(_context).GetProductById(orderLine.ProductId) != null ? "/products/edit/" + orderLine.ProductId : "" },
                    { "refunds", orderBundle.OrderRefundLines.Where(OrderRefundLine => OrderRefundLine.OrderLineId == orderLine.Id) }
                };

                items.Add(item);
            }

            return items;
        }

        public decimal GetTotalLinePrice(IEnumerable<OrderLines> orderLines, int digitsAfterDecimal)
        {
            decimal productsTotal = 0;

            foreach (OrderLines orderLine in orderLines)
            {
                decimal productTax = ((decimal.Round(orderLine.Price, DigitsAfterDecimal, MidpointRounding.AwayFromZero) / (100 + orderLine.TaxRate)) * orderLine.TaxRate);
                decimal totalProductTax = (productTax * orderLine.Quantity);

                Tax = Tax + totalProductTax;

                if (TaxClasses.ContainsKey(orderLine.TaxRate.ToString()))
                {
                    TaxClasses[orderLine.TaxRate.ToString()] = decimal.Parse(TaxClasses[orderLine.TaxRate.ToString()].ToString()) + totalProductTax;
                }
                else
                {
                    TaxClasses[orderLine.TaxRate.ToString()] = totalProductTax;
                }
                

                decimal total = (decimal.Round(orderLine.Price, digitsAfterDecimal, MidpointRounding.AwayFromZero) * orderLine.Quantity);

                productsTotal = (productsTotal + decimal.Round(total, digitsAfterDecimal, MidpointRounding.AwayFromZero));
            }

            return productsTotal;
        }

        public Dictionary<decimal, decimal> DistributePricesToTaxes(IEnumerable<OrderLines> orderLines)
        {
            Dictionary<decimal, decimal> prices = new Dictionary<decimal, decimal>();
            foreach (OrderLines orderLine in orderLines)
            {
                if (orderLine.TaxShipping ?? true)
                {
                    decimal price = decimal.Round(orderLine.Price, DigitsAfterDecimal, MidpointRounding.AwayFromZero) * orderLine.Quantity;
                    _total = _total + decimal.Round(price, DigitsAfterDecimal, MidpointRounding.AwayFromZero);
                    if (prices.ContainsKey(orderLine.TaxRate))
                    {
                        prices[orderLine.TaxRate] = prices.FirstOrDefault(x => x.Key == orderLine.TaxRate).Value + decimal.Round(price, DigitsAfterDecimal, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        prices.Add(orderLine.TaxRate, decimal.Round(price, DigitsAfterDecimal, MidpointRounding.AwayFromZero));
                    }
                }
            }

            return prices;
        }

        public decimal GetTotalShippingCosts(IEnumerable<OrderShippingZoneMethods> orderShippingZoneMethods, IEnumerable<OrderLines> orderLines, IEnumerable<OrderFees> orderFees, int digitsAfterDecimal)
        {
            Dictionary<decimal, decimal> prices = DistributePricesToTaxes(orderLines);

            foreach (OrderFees orderFee in orderFees)
            {
                _total = _total + decimal.Round(orderFee.Price, digitsAfterDecimal, MidpointRounding.AwayFromZero);
                if (prices.ContainsKey(orderFee.TaxRate ?? 0))
                {
                    prices[orderFee.TaxRate ?? 0] = prices.FirstOrDefault(x => x.Key == orderFee.TaxRate).Value + decimal.Round(orderFee.Price, digitsAfterDecimal, MidpointRounding.AwayFromZero);
                }
                else
                {
                    prices.Add(orderFee.TaxRate ?? 0, decimal.Round(orderFee.Price, digitsAfterDecimal, MidpointRounding.AwayFromZero));
                }
            }

            Dictionary<decimal, decimal> percentages = Commerce.CalculatePercentageOfTaxWithPrice(prices, _total);

            decimal total = 0;
            decimal notTaxable = 0;
            foreach (OrderShippingZoneMethods orderShippingZoneMethod in orderShippingZoneMethods)
            {
                if (orderShippingZoneMethod.Taxable ?? true)
                {
                    total = total + decimal.Round(orderShippingZoneMethod.Price, digitsAfterDecimal, MidpointRounding.AwayFromZero);
                }
                else
                {
                    notTaxable = notTaxable + decimal.Round(orderShippingZoneMethod.Price, digitsAfterDecimal, MidpointRounding.AwayFromZero);
                }
            }

            decimal totalShippingPrice = 0;
            foreach (KeyValuePair<decimal, decimal> percentage in percentages)
            {
                decimal price = (total / 100) * percentage.Value;
                decimal scTax = ((price / (100 + percentage.Key)) * percentage.Key);
                Tax = Tax + scTax;

                if (TaxClasses.ContainsKey(percentage.Key.ToString()))
                {
                    TaxClasses[percentage.Key.ToString()] = decimal.Parse(TaxClasses[percentage.Key.ToString()].ToString()) + scTax;
                }
                else
                {
                    TaxClasses[percentage.Key.ToString()] = scTax;
                }

                totalShippingPrice = totalShippingPrice + price;
            }

            return decimal.Round(totalShippingPrice + notTaxable, digitsAfterDecimal, MidpointRounding.AwayFromZero);
        }

        public decimal GetTotalFeePrice(IEnumerable<OrderFees> orderFees, int digitsAfterDecimal)
        {
            decimal total = 0;

            foreach (OrderFees orderFee in orderFees)
            {
                decimal orderFeeTaxRate = (orderFee.TaxRate ?? 0.00M);
                decimal feeTax = ((decimal.Round(orderFee.Price, DigitsAfterDecimal, MidpointRounding.AwayFromZero) / (100 + orderFeeTaxRate)) * orderFeeTaxRate);
                Tax = Tax + feeTax;

                if (TaxClasses.ContainsKey(orderFee.TaxRate.ToString()))
                {
                    TaxClasses[orderFee.TaxRate.ToString()] = decimal.Parse(TaxClasses[orderFee.TaxRate.ToString()].ToString()) + feeTax;
                }
                else
                {
                    TaxClasses[orderFee.TaxRate.ToString()] = feeTax;
                }

                total = total + decimal.Round(orderFee.Price, DigitsAfterDecimal, MidpointRounding.AwayFromZero) ;
            }

            return total;
        }

        //public decimal GetTotalCouponPrice(IEnumerable<OrderCoupons> orderCoupons, int digitsAfterDecimal)
        //{
        //    decimal total = 0;
        //
        //    foreach (OrderCoupons orderCoupon in orderCoupons)
        //    {
        //        total = total + orderCoupon.Discount;
        //    }
        //
        //    return decimal.Round(total, digitsAfterDecimal, MidpointRounding.AwayFromZero);
        //}

        public string GetStatus(string status)
        {
            switch (status.ToLower())
            {
                case "concept":
                    return "Concept";
                case "pending":
                    return "Pending";
                case "processing":
                    return "Processing";
                case "hold":
                    return "On hold";
                case "completed":
                    return "Completed";
                case "credit":
                    return "Credit";
                case "cancelled":
                    return "Cancelled";
                case "refunded":
                    return "Refunded";
                case "failed":
                    return "Failed";
                case "reserved":
                    return "Reserved";
                default:
                    return "Failed";
            }
        }
    }
}