namespace FreshStock.API.Enums
{
    public enum EstadoStock
    {
        Critico = 1,    // Stock < StockMinimo
        Bajo = 2,       // Stock entre StockMinimo y StockIdeal * 0.5
        Normal = 3,     // Stock entre 50% y 100% del ideal
        Exceso = 4      // Stock > StockMaximo
    }
}
