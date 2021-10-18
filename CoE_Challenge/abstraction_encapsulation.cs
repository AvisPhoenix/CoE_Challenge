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
        public static void Main(){
            var db = BuildDataBase();
            var order = new Order();
            
            AddPromotionals(order, db["shampoo"], db["soap"], db["nachos"], db["dip"], db["soda"], db["chips"]);

            order.Add(new OrderLine { Item = db["shampoo"], Quantity = 2 })
                 .Add(new OrderLine { Item = db["shampoo"], Quantity = 2 })
                 .Add(new OrderLine { Item = db["soap"],    Quantity = 5 })
                 .Add(new OrderLine { Item = db["nachos"],  Quantity = 2 })
                 .Add(new OrderLine { Item = db["soda"],    Quantity = 1 })
                 .Add(new OrderLine { Item = db["chips"],   Quantity = 1 });

            PrintData(order);
        }

        private static Dictionary<string, Item> BuildDataBase(){
            var dict = new Dictionary<string, Item>();
            BuildDataBasePart1(dict);
            BuildDataBasePart2(dict);
            return dict;
        }

        private static void BuildDataBasePart1(Dictionary<string, Item> dict){
            dict["shampoo"] = new Item { Name = "Shampoo", Price = 12.95m };
            dict["soap"]    = new Item { Name = "Soap", Price = 8m };
            dict["nachos"]  = new Item { Name = "Nachos", Price = 7m };
        }

        private static void BuildDataBasePart2(Dictionary<string, Item> dict){
            dict["soda"]  = new Item { Name = "Soda (2 lts)", Price = 13.50m };
            dict["chips"] = new Item { Name = "Potato chips", Price = 10m };
            dict["dip"]   = new Item { Name = "Dip", Price = 10m };
        }

        private static void PrintData(Order order){
            Console.WriteLine(order.PrintTicket());
            Console.WriteLine($"the expected cost is 101.3840. The actual cost is { order.CalcCost().ToString("F4") }");
        }

        private static void Add2x1Promotionals(Order order, string shampooUID, string soapUID, string nachosUID, string dipUID, string sodaUID, string chipsUID ){
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

        private static void AddBundlePromotionals(Order order, Item shampoo, Item soap, Item nachos, Item dip, Item soda, Item chips ){
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

        private static void AddPromotionals(Order order, Item shampoo, Item soap, Item nachos, Item dip, Item soda, Item chips ){
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
        
        public Order(){
            Lines = new Dictionary<string, OrderLine>();
            Tax = 1.16m;
            Promotionals = new List<IPromotional>();
            PromotionalsApplied = new List<OrderLine>();
        }

        public Order Add(OrderLine orderLine){ 
            string uID = orderLine.Item.genUID();
            if (Lines.ContainsKey(uID)) Lines[uID].Quantity += orderLine.Quantity;
            else Lines.Add(uID, orderLine);
            return this;
        }

        public Order Add(List<OrderLine> orderLines){ 
            foreach (var item in orderLines) Add(item);
            return this;
        }

        public Order Remove(OrderLine orderLine){ return Remove( orderLine.Item, orderLine.Quantity ); }
        
        public Order Remove(Item item, int quantity = 1){
            string uID = item.genUID();

            if (Lines.ContainsKey(uID)) ItemRemove( uID, quantity );
            
            return this;
        }

        private void ItemRemove(string uID, int quantity){
            if (Lines[uID].Quantity > quantity) Lines[uID].Quantity -= quantity;
            else Lines.Remove(uID);
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
            ApplyPromotionals();
            decimal total = NormalLinesCost() + PromotionalsDiscount();
            return total*Tax;
        }

        public string PrintTicket(){
            StringBuilder sb = new StringBuilder();
            BuildSections(sb, CalcCost());
            return sb.ToString();
        }

        private void BuildSections(StringBuilder sb, decimal total){
            BuildOrderLine(sb);
            BuildPromotionals(sb);
            BuildShowTotalCost(sb,total);
        }
        private void BuildOrderLine(StringBuilder sb){
            sb.AppendFormat("{0} Order {0}\n", new string('-',38));
            foreach (var item in Lines){
                sb.Append(item.Value.ToString());
                sb.Append("\n");
            }
        }

        private void BuildPromotionals(StringBuilder sb){
            sb.AppendFormat("{0} Promotional {0}\n", new string('-',35));
            foreach (var item in PromotionalsApplied){
                sb.Append(item.ToString());
                sb.Append("\n");
            }
        }

        private void BuildShowTotalCost(StringBuilder sb, decimal total){
            sb.AppendFormat("{0}\n",new string('-',82)); 
            sb.AppendFormat("{0,50}{1}>${2,9}\n","Cost", new string('-',21), (total/Tax).ToString("F4")); 
            sb.AppendFormat("{0,50}{1}>${2,9}\n","Taxes", new string('-',21), (total-(total/Tax)).ToString("F4")); 
            sb.AppendFormat("{0,50}{1}>${2,9}\n","Total", new string('-',21), total.ToString("F4")); 
        }

        private void ApplyPromotionals(){
            clearPromotionalsApplied();
            foreach (IPromotional promotional in Promotionals){
                List<OrderLine> prom = promotional.Apply(Lines);
                if (prom != null) PromotionalsApplied.AddRange(prom);
            }
        }

        private void clearPromotionalsApplied(){
            PromotionalsApplied.Clear();
            foreach (var item in Lines) item.Value.OnBundle =  0;
        }

        private decimal NormalLinesCost(){
            decimal total = 0;
            foreach (var line in Lines) total += line.Value.Quantity * line.Value.Item.Price;
            return total;
        }
        private decimal PromotionalsDiscount(){
            decimal total = 0;
            foreach (var line in PromotionalsApplied) total += line.Quantity * line.Item.Price;
            return total;
        }
    }

    public class OrderLine{
        public Item Item { get; set; }
        public int Quantity { get; set; }
        public int OnBundle { get; set; }

        public OrderLine(){ OnBundle = 0; }
        public int AvailableItems(){ return Quantity-OnBundle; }
        public override string ToString(){
            // 77 columns
            return String.Format("{0,50} (${1, 9}) x {2, 3} = ${3,9}",
                                 Item.Name, Item.Price.ToString("F4"), 
                                 Quantity.ToString(), 
                                 (Item.Price*Quantity).ToString("F4"));
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
            return lines.ContainsKey(ProductUID)? CalculateDiscount(lines[ProductUID]): null;
        }

        private List<OrderLine> CalculateDiscount(OrderLine order){
            List<OrderLine> output = order.AvailableItems()/N >= 1? new List<OrderLine>() : null;
            if (order.AvailableItems()/N >= 1){
                output.Add( CreateOrder(order.Item.Price, order.AvailableItems()) );
                order.OnBundle += (order.AvailableItems()/N)*N; //Remember: (A/B)*B != A when A and B are Integers
            }
            return output;
        }

        private Item CreateItem(decimal price){
            return new Item { Name=this.Name,
                              Price= ((M-N))*price
                            };
        }

        private OrderLine CreateOrder(decimal price, int quantity){
            return new OrderLine { Item= CreateItem(price),
                                   Quantity=quantity/N
                                 };
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
            if (bundleCount > 0) AnnotateProducts(lines, bundleCount);
            return bundleCount > 0 ?  CreateOutput(lines,bundleCount)
                                    : null;
        }

        private int CalculateBundleQuantity(Dictionary<string, OrderLine> lines){
            int minQuantity=ProductsBundle.Count > 0? HowManyBundlesCanOrderHoldByFirstItem(lines)
                                                     : 0; 
            foreach (BundleProduct item in ProductsBundle){
                minQuantity = lines.ContainsKey(item.ProductUID)? CalculateItemQuantity(lines[item.ProductUID], item, minQuantity)
                                                                 : minQuantity;
            }
            return minQuantity;
        }

        private int HowManyBundlesCanOrderHoldByFirstItem(Dictionary<string, OrderLine> lines){
            var firstItem = ProductsBundle[0];
            var orderQuantity = lines[firstItem.ProductUID].AvailableItems();
            return HowManyBundlesCanOrderHoldByItem(orderQuantity, firstItem.Quantity);
        }

        private int HowManyBundlesCanOrderHoldByItem(int orderQuantity, int bundleQuantity){
            return orderQuantity/bundleQuantity;
        }

        private int CalculateItemQuantity(OrderLine order, BundleProduct item, int currentMinQuantity){
            var quantityByItem = HowManyBundlesCanOrderHoldByItem(order.AvailableItems(), item.Quantity);
            return quantityByItem < currentMinQuantity? quantityByItem
                                                     : currentMinQuantity;
        }

        private decimal CalculateDiscount(Dictionary<string, OrderLine> lines, int bundleCount){
            decimal output=0;
            foreach (OrderLine order in FreeProductsBundle){
                output += lines.ContainsKey(order.Item.genUID())? CalculateDiscountItem(lines[order.Item.genUID()].AvailableItems(), bundleCount*order.Quantity, lines[order.Item.genUID()].Item.Price )
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
                    BuildDiscountItemDetails(lines[order.Item.genUID()].AvailableItems(), bundleCount*order.Quantity, lines[order.Item.genUID()], oLines);
                }else {
                    oLines.Add(createOrderLine(order.Item.Name, 0, bundleCount*order.Quantity));
                }
            }
        }
        private OrderLine createOrderLine(string name, decimal price, int quantity){
            return new OrderLine { Item= new Item { Name=name, Price=price}, Quantity= quantity };
        }
        private void BuildDiscountItemDetails(int linesQuantity, int itemQuantity, OrderLine order, List<OrderLine> oLines){
            if (linesQuantity > 0) DiscountOrderItems(linesQuantity, itemQuantity, order, oLines);
            if (linesQuantity < itemQuantity) AddFreeOrderItem(linesQuantity, itemQuantity, order, oLines);
        }

        private void DiscountOrderItems(int linesQuantity, int itemQuantity, OrderLine order, List<OrderLine> oLines){
            oLines.Add(createOrderLine(order.Item.Name, order.Item.Price, -(linesQuantity < itemQuantity? linesQuantity: itemQuantity) ));
            AnnotateOrder(order, linesQuantity < itemQuantity? linesQuantity: itemQuantity);
        }

        private void AddFreeOrderItem(int linesQuantity, int itemQuantity, OrderLine order, List<OrderLine> oLines){
            oLines.Add(createOrderLine(order.Item.Name, 0, itemQuantity-linesQuantity ));
        }

        private List<OrderLine> CreateOutput(Dictionary<string, OrderLine> lines, int bundleCount){
            List<OrderLine> output = new List<OrderLine>();
            output.Add( CreateBundleOrder(lines, bundleCount) );
            CalculateDiscountDetails(lines,bundleCount,output);
            return output;
        }

        private string BuildBundleDisplayName(Dictionary<string, OrderLine> lines, int bundleCount){
            return String.Format("{0} (Total: ${1})", Name , CalculateDiscount(lines,bundleCount).ToString("F4"));
        }

        private Item CreateBundleItem(Dictionary<string, OrderLine> lines, int bundleCount){
            return new Item { Name= BuildBundleDisplayName(lines, bundleCount),
                              Price= 0
                            };
        }

        private OrderLine CreateBundleOrder(Dictionary<string, OrderLine> lines, int bundleCount){
            return new OrderLine { Item= CreateBundleItem(lines, bundleCount),
                                   Quantity= bundleCount
                                 };
        }

        private void AnnotateOrder(OrderLine order, int quantity){ order.OnBundle += quantity; }

        private void AnnotateProducts(Dictionary<string, OrderLine> lines, int bundleCount){
            foreach (var item in ProductsBundle){
                AnnotateOrder(lines[item.ProductUID],bundleCount*item.Quantity);
            }
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
            bundle.AddProductsBundle(ProductsBundle)
                  .AddFreeProductsBundle(FreeProductsBundle);
            return bundle;
        }
        
    }
}