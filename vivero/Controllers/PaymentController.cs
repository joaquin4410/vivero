using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Linq;

namespace vivero.Controllers
{
    [Route("api/payment")]
    public class PaymentController : Controller
    {
        [HttpPost("create-session")]
        public IActionResult CreateSession([FromBody] List<CartItem> carrito)
        {
            StripeConfiguration.ApiKey = "sk_test_51QYJiiLWlNrMPxPrxB7gGRmY2bRV14n1UuuXDWE9nozSNuwG5uByF9lRZ53oRv326GEMtdaKBLes2B1KteklU84500TKcvVjKG"; // Clave secreta

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = carrito.Select(item => new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Precio * 100), // Centavos
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Nombre
                        },
                    },
                    Quantity = item.Cantidad,
                }).ToList(),
                Mode = "payment",
                SuccessUrl = "https://tu-sitio.com/success",
                CancelUrl = "https://tu-sitio.com/cancel",
            };

            var service = new SessionService();
            var session = service.Create(options);

            return Json(new { url = session.Url });
        }
    }

    public class CartItem
    {
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
    }
}
