//The following code belongs to an online shopping cart
//Your job is to make the code able to handle the following business rules:

//add a promotional 2X1 discount for every product but any snack
//for each bundle of 1 shampoo and 1 soap you get another free soap
//when you buy 2 bags of nachos or more you get 1 dip for free
//for each bundle of 1 2 lts soda and 1 bag of chips, you get another bag of chips for free

//restrictions: 
//no more than 5 lines per method

//Please apply the OOP tenets: Encapsulation, Polymorphism, 
//Abstraction and Inheritance as you see fit to make this code
//object oriented


using System;
using System.Collections.Generic;
using System.Text;

namespace unsolved
{
    public class Test
    {
        public static void Main()
        {
            
            var shampoo = new Item { Name = "Shampoo", Price = 12.95m };
            var soap    = new Item { Name = "Soap", Price = 8m };
            var nachos  = new Item { Name = "Nachos", Price = 7m };
            var soda    = new Item { Name = "Soda (2 lts)", Price = 13.50m };
            var chips   = new Item { Name = "Potato chips", Price = 10m };
            var dip     = new Item { Name = "Dip", Price = 10m };
            
            var order = new Order();
            
            AddPromotionals(order, shampoo, soap, nachos, dip, soda, chips);

            order.Add(new OrderLine { Item = shampoo, Quantity = 2 })
                 .Add(new OrderLine { Item = shampoo, Quantity = 2 })
                 .Add(new OrderLine { Item = soap,    Quantity = 5 })
                 .Add(new OrderLine { Item = nachos,  Quantity = 2 })
                 .Add(new OrderLine { Item = soda,    Quantity = 1 })
                 .Add(new OrderLine { Item = chips,   Quantity = 1 });

            Console.WriteLine(order.PrintTicket());
            Console.WriteLine($"the expected cost is 101.3840. The actual cost is { order.CalcCost().ToString("F4") }");
            
        }

        public static void Add2x1Promotionals(Order order, string shampooUID, string soapUID, string nachosUID, string dipUID, string sodaUID, string chipsUID ){
            order.AddPromotional(new DiscountsNxMPromotial{ Name="Shampoo 2x1 Discount",
                                                            ProductUID=shampooUID,
                                                            N = 2,
                                                            M = 1  })
                 .AddPromotional(new DiscountsNxMPromotial{ Name="Soap 2x1 Discount",
                                                            ProductUID=soapUID,
                                                            N = 2,
                                                            M = 1  })
                 .AddPromotional(new DiscountsNxMPromotial{ Name="Soda (2 lts) 2x1 Discount",
                                                            ProductUID=sodaUID,
                                                            N = 2,
                                                            M = 1  });
        }

        public static void AddBundlePromotionals(Order order, Item shampoo, Item soap, Item nachos, Item dip, Item soda, Item chips ){
            order.AddPromotional( new BundlePromotionalFactory("PaqueteBañes Bundle")
                                     .AddProductsBundle(shampoo.genUID(),1).AddProductsBundle(soap.genUID(),1)
                                     .AddFreeProductsBundle(new OrderLine { Item = soap,    Quantity = 1 }).build())
                 .AddPromotional( new BundlePromotionalFactory("PaqueDisfrutes Bundle")
                                     .AddProductsBundle(nachos.genUID(),2)
                                     .AddFreeProductsBundle(new OrderLine { Item = dip,    Quantity = 1 }).build())
                 .AddPromotional( new BundlePromotionalFactory("PaqueCompartas Bundle")
                                     .AddProductsBundle(soda.genUID(),1).AddProductsBundle(chips.genUID(),1)
                                     .AddFreeProductsBundle(new OrderLine { Item = chips,    Quantity = 1 }).build());
        }

        public static void AddPromotionals(Order order, Item shampoo, Item soap, Item nachos, Item dip, Item soda, Item chips ){
            Add2x1Promotionals(order,shampoo.genUID(), soap.genUID(), nachos.genUID(), dip.genUID(), soda.genUID(), chips.genUID());
            AddBundlePromotionals(order, shampoo, soap, nachos, dip, soda, chips);
        }
    }

    public class Order
    {
        public decimal Tax {get; set;}
        private Dictionary<string, OrderLine> Lines;
        private List<IPromotional> Promotionals;
        private List<OrderLine> PromotionalsApplied;
        
        public Order()
        {
            Lines = new Dictionary<string, OrderLine>();
            Tax = 1.16m;
            Promotionals = new List<IPromotional>();
            PromotionalsApplied = new List<OrderLine>();
        }

        public Order Add(OrderLine orderLine){ 
            string uID = orderLine.Item.genUID();
            if (Lines.ContainsKey(uID)){
                Lines[uID].Quantity += orderLine.Quantity;
            } else {
                Lines.Add(uID, orderLine);
            }
            return this;
        }

        public Order Add(List<OrderLine> orderLines){ 
            foreach (var item in orderLines){
                Add(item);
            }
            return this;
        }

        public Order Remove(OrderLine orderLine){ return Remove( orderLine.Item, orderLine.Quantity ); }
        
        public Order Remove(Item item, int quantity = 1){
            string uID = item.genUID();

            if (Lines.ContainsKey(uID)){
                if (Lines[uID].Quantity > quantity){
                    Lines[uID].Quantity -= quantity;
                } else {
                    Lines.Remove(uID);
                }
            }
            
            return this;
        }

        public int Quantity(Item item){
            string uID = item.genUID();
            return Lines.ContainsKey(uID)? Lines[uID].Quantity : 0;
        }
        
        public Order AddPromotional(IPromotional promotional){
            Promotionals.Add(promotional);
            return this;
        }

        public decimal CalcCost(){
            decimal total = 0;

            ApplyPromotionals();
            
            total = NormalLinesCost() + PromotionalsDiscount();

            return total*Tax;
        }

        public string PrintTicket(){
            StringBuilder sb = new StringBuilder();
            decimal total = CalcCost();
            BuildOrderLine(sb);
            BuildPromotionals(sb);
            BuildShowTotalCost(sb,total);
            return sb.ToString();
        }
        public void BuildOrderLine(StringBuilder sb){
            sb.AppendFormat("{0} Order {0}\n", new string('-',37));
            foreach (var item in Lines){
                sb.Append(item.Value.ToString());
                sb.Append("\n");
            }
        }

        public void BuildPromotionals(StringBuilder sb){
            sb.AppendFormat("{0} Promotional {0}\n", new string('-',34));
            foreach (var item in PromotionalsApplied){
                sb.Append(item.ToString());
                sb.Append("\n");
            }
        }

        public void BuildShowTotalCost(StringBuilder sb, decimal total){
            sb.AppendFormat("{0}\n",new string('-',81)); 
            sb.AppendFormat("{0,50}{1}>${2,5}\n","Total", new string('-',22), total.ToString("F4")); 
        }
        private void ApplyPromotionals(){
            PromotionalsApplied.Clear();
            foreach (IPromotional promotional in Promotionals){
                List<OrderLine> prom = promotional.Apply(Lines);
                if (prom != null){
                    PromotionalsApplied.AddRange(prom);
                } 
            }
        }

        private decimal NormalLinesCost(){
            decimal total = 0;

            foreach (var line in Lines){
                total += line.Value.Quantity * line.Value.Item.Price;
            }

            return total;
        }
        private decimal PromotionalsDiscount(){
            decimal total = 0;

            foreach (var line in PromotionalsApplied){
                total += line.Quantity * line.Item.Price;
            }

            return total;
        }
    }

    public class OrderLine{
        public Item Item { get; set; }
        public int Quantity { get; set; }

        public override string ToString(){
            // 77 columns
            return String.Format("{0,50} (${1, 9}) x {2, 3} = ${3,8}",Item.Name, Item.Price.ToString("F4"), Quantity.ToString(),  (Item.Price*Quantity).ToString("F4"));
        }
    }


    public class Item
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string genUID(){ return Name.Replace(" ", ""); }
    }

    public interface IPromotional{
        public string Name { get; set; }

        public List<OrderLine> Apply(Dictionary<string, OrderLine> Lines);
    }

    public class DiscountsNxMPromotial: IPromotional{

        public string Name { get; set; }

        public string ProductUID { get; set; }

        public int N { get; set; }

        public int M { get; set; }

        public List<OrderLine> Apply(Dictionary<string, OrderLine> lines){
            return lines.ContainsKey(ProductUID)? CalculateDiscount(lines[ProductUID].Quantity, lines[ProductUID].Item.Price): null;
        }

        private List<OrderLine> CalculateDiscount(int lineQuantity, decimal linePrice){
            List<OrderLine> output = null;
            if (lineQuantity/N >= 1){
                output = new List<OrderLine>();
                output.Add(new OrderLine { Item= new Item { Name=this.Name,
                                                            Price= ((M-N))*linePrice
                                                          },
                                           Quantity=lineQuantity/N
                                         });
            }
            return output;
        }

    }

    public class BundlePromotional: IPromotional{
        public string Name { get; set; }
        private List<BundleProduct> ProductsBundle { get; set; }
        private List<OrderLine> FreeProductsBundle { get; set; }
        public BundlePromotional(){
            ProductsBundle = new List<BundleProduct>();
            FreeProductsBundle = new List<OrderLine>();
        }
        public BundlePromotional AddProductsBundle( BundleProduct bundleProduct){
            ProductsBundle.Add(bundleProduct);
            return this;
        }
        public BundlePromotional AddProductsBundle( List<BundleProduct> bundleProducts){
            ProductsBundle.AddRange(bundleProducts);
            return this;
        }

        public BundlePromotional AddFreeProductsBundle( OrderLine order ){
            FreeProductsBundle.Add(order);
            return this;
        }
        public BundlePromotional AddFreeProductsBundle( List<OrderLine> orders ){
            FreeProductsBundle.AddRange(orders);
            return this;
        }

        public List<OrderLine> Apply(Dictionary<string, OrderLine> lines){
            int bundleCount = CalculateBundleQuantity(lines);
            return bundleCount > 0 ?  createOutput(lines,bundleCount)
                                    : null;
        }

        private int CalculateBundleQuantity(Dictionary<string, OrderLine> lines){
            int minQuantity=ProductsBundle.Count > 0? lines[ProductsBundle[0].ProductUID].Quantity/ProductsBundle[0].Quantity: 0; 
            foreach (BundleProduct item in ProductsBundle){
                minQuantity = lines.ContainsKey(item.ProductUID) && lines[item.ProductUID].Quantity/item.Quantity < minQuantity? lines[item.ProductUID].Quantity/item.Quantity : minQuantity;
            }
            return minQuantity;
        }

        private decimal CalculateDiscount(Dictionary<string, OrderLine> lines, int bundleCount){
            decimal output=0;
            foreach (OrderLine order in FreeProductsBundle){
                output += lines.ContainsKey(order.Item.genUID())? CalculateDiscountItem(lines[order.Item.genUID()].Quantity, bundleCount*order.Quantity, lines[order.Item.genUID()].Item.Price )
                                                                 : 0;
            }
            return output;
        }

        private decimal CalculateDiscountItem(int linesQuantity, int itemQuantity, decimal itemPrice){
            return linesQuantity > itemQuantity?  -itemQuantity*itemPrice 
                                                : -linesQuantity*itemPrice;
        }

        private void CalculateDiscountDetails(Dictionary<string, OrderLine> lines, int bundleCount, List<OrderLine> oLines){
            foreach (OrderLine order in FreeProductsBundle){ 
                if (lines.ContainsKey(order.Item.genUID())){
                    BuildDiscountItemDescription(lines[order.Item.genUID()].Quantity, bundleCount*order.Quantity, order.Item, oLines);
                }else {
                    oLines.Add(createOrderLine(order.Item.Name, 0, bundleCount*order.Quantity));
                }
            }
        }
        private OrderLine createOrderLine(string name, decimal price, int quantity){
            return new OrderLine { Item= new Item { Name=name, Price=price}, Quantity= quantity };
        }
        private void BuildDiscountItemDescription(int linesQuantity, int itemQuantity, Item item, List<OrderLine> oLines){
            oLines.Add(createOrderLine(item.Name, item.Price, -(linesQuantity < itemQuantity? linesQuantity: itemQuantity) ));
            if (linesQuantity < itemQuantity){
                oLines.Add(createOrderLine(item.Name, 0, itemQuantity-linesQuantity ));
            }
        }

        private List<OrderLine> createOutput(Dictionary<string, OrderLine> lines, int bundleCount){
            List<OrderLine> output = new List<OrderLine>();
            output.Add(new OrderLine { Item= new Item { Name= String.Format("{0} (Total: ${1})", Name , CalculateDiscount(lines,bundleCount).ToString("F4")),
                                                        Price= 0
                                                      },
                                        Quantity= bundleCount
                                     });
            CalculateDiscountDetails(lines,bundleCount,output);
            return output;
        }

    }

    public class BundleProduct {
        public string ProductUID { get; set; }
        public int Quantity { get; set; }
    }

    public class BundlePromotionalFactory{
        private string BundleName { get; set; }
        private List<BundleProduct> ProductsBundle { get; set; }
        private List<OrderLine> FreeProductsBundle { get; set; }
        public BundlePromotionalFactory( string BundleName){
            this.BundleName = BundleName;
            ProductsBundle = new List<BundleProduct>();
            FreeProductsBundle = new List<OrderLine>();
        }
        public BundlePromotionalFactory AddProductsBundle( string productUID, int quantity){
            ProductsBundle.Add(new BundleProduct {ProductUID= productUID, Quantity = quantity });
            return this;
        }
        public BundlePromotionalFactory AddFreeProductsBundle( OrderLine line ) {
            FreeProductsBundle.Add(line);
            return this;
        }
        public BundlePromotional build(){
            BundlePromotional bundle = new BundlePromotional{ Name= BundleName};
            bundle.AddProductsBundle(ProductsBundle);
            bundle.AddFreeProductsBundle(FreeProductsBundle);
            return bundle;
        }
        
    }
}