using KiteCopilot.Domain.Enums;

namespace KiteCopilot.Infrastructure.Common;

public static class AssetClassifier
{
    private static readonly HashSet<string> KnownEtfs = new(StringComparer.OrdinalIgnoreCase)
    {
        "MON100", "GOLDBEES", "NIFTYBEES", "BANKBEES", "ITBEES", "SILVERBEES", "LIQUIDBEES", 
        "JUNIORBEES", "CPSEETF", "BHARAT22", "HDFCMFGETF", "SETFNIF50", "KOTAKNIFTY", "ICICINIFTY",
        "AUTOBEES", "PHARMABEES", "MID150BEES", "MAFANG", "HDFCLOWVOL", "MOM30IETFS"
    };

    private static readonly HashSet<string> KnownIpos = new(StringComparer.OrdinalIgnoreCase)
    {
        "SWIGGY", "HYUNDAI", "NTPCGREEN", "BAJAJHFL", "OLAELEC", "PREMIERENE", "KRN", "WAREE", 
        "ZOMATO", "PAYTM", "NYKAA", "POLICYBZR", "MAMAARTH"
    };

    private static readonly Dictionary<string, (string Name, string Sector)> StockMetadata = new(StringComparer.OrdinalIgnoreCase)
    {
        { "GOLDBEES", ("Nippon India ETF Gold BeES", "Commodities / Gold ETF") },
        { "MON100", ("Motilal Oswal Nasdaq 100 ETF", "International Tech Index ETF") },
        { "NIFTYBEES", ("Nippon India Nifty 50 BeES ETF", "Index ETF") },
        { "BANKBEES", ("Nippon India ETF Bank BeES", "Banking Sector ETF") },
        { "ITBEES", ("Nippon India ETF IT BeES", "Technology Sector ETF") },
        { "SILVERBEES", ("Nippon India ETF Silver BeES", "Commodities / Silver ETF") },
        { "HDFCBANK", ("HDFC Bank Limited", "Banking & Financial Services") },
        { "TCS", ("Tata Consultancy Services Ltd", "Information Technology Services") },
        { "INFY", ("Infosys Limited", "Information Technology Services") },
        { "HINDUNILVR", ("Hindustan Unilever Limited", "FMCG & Consumer Goods") },
        { "ITC", ("ITC Limited", "FMCG & Conglomerates") },
        { "RELIANCE", ("Reliance Industries Limited", "Energy / Telecom / Retail") },
        { "ICICIBANK", ("ICICI Bank Limited", "Banking & Financials") },
        { "SBIN", ("State Bank of India", "Public Sector Banking") },
        { "BHARTIARTL", ("Bharti Airtel Limited", "Telecommunications") },
        { "LT", ("Larsen & Toubro Limited", "Infrastructure & Capital Goods") },
        { "AXISBANK", ("Axis Bank Limited", "Banking & Financials") },
        { "KOTAKBANK", ("Kotak Mahindra Bank", "Banking & Financials") },
        { "WIPRO", ("Wipro Limited", "Information Technology Services") },
        { "HCLTECH", ("HCL Technologies Limited", "Information Technology Services") },
        { "MARUTI", ("Maruti Suzuki India Limited", "Automobiles") },
        { "TATAMOTORS", ("Tata Motors Limited", "Automobiles / EV") },
        { "TATASTEEL", ("Tata Steel Limited", "Metals & Mining") },
        { "BAJFINANCE", ("Bajaj Finance Limited", "NBFC / Financial Services") },
        { "SWIGGY", ("Swiggy Limited", "New-age Tech / Quick Commerce IPO") },
        { "HYUNDAI", ("Hyundai Motor India Limited", "Automobiles IPO") },
        { "NTPCGREEN", ("NTPC Green Energy Limited", "Renewable Energy IPO") }
    };

    public static AssetType DetectAssetType(string symbol)
    {
        var sym = symbol.Trim().ToUpperInvariant();
        if (KnownEtfs.Contains(sym) || sym.EndsWith("BEES", StringComparison.OrdinalIgnoreCase) || sym.EndsWith("ETF", StringComparison.OrdinalIgnoreCase))
        {
            return AssetType.ETF;
        }

        if (KnownIpos.Contains(sym))
        {
            return AssetType.IPO;
        }

        if (sym.Contains("NIFTY") || sym.Contains("SENSEX") || sym == "INDIAVIX")
        {
            return AssetType.INDEX;
        }

        return AssetType.STOCK;
    }

    public static string GetCompanyName(string symbol)
    {
        var sym = symbol.Trim().ToUpperInvariant();
        if (StockMetadata.TryGetValue(sym, out var meta))
        {
            return meta.Name;
        }
        return sym;
    }

    public static string GetSector(string symbol)
    {
        var sym = symbol.Trim().ToUpperInvariant();
        if (StockMetadata.TryGetValue(sym, out var meta))
        {
            return meta.Sector;
        }
        return DetectAssetType(sym).ToString();
    }
}
