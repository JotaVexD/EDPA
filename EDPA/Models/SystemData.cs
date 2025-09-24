using System.Text.Json.Serialization;

namespace EDPA.Models
{
    public class SystemData
    {
        // Essential properties for scoring
        public string Name { get; set; }
        public string Security { get; set; }
        public long Population { get; set; }
        public string Economy { get; set; }
        public string SecondEconomy { get; set; }
        public string Government { get; set; }
        public string FactionState { get; set; }

        // Collections we actually use
        public List<Station> Stations { get; set; } = new List<Station>();
        public List<CommodityMarket> BestCommodities { get; set; } = new List<CommodityMarket>();
        public List<PiracyScoreResult> SystemScore { get; set; } = new List<PiracyScoreResult>();
        public List<MinorFactionPresences> MinorFactionPresences { get; set; } = new List<MinorFactionPresences>();

        // Remove rings and planets entirely since API doesn't provide this data
        // We'll handle rings/belts differently (see below)

        // Properties not used in scoring
        [JsonIgnore]
        public string Allegiance { get; set; }

        [JsonIgnore]
        public string ControllingFaction { get; set; }

        [JsonIgnore]
        public TrafficData TrafficData { get; set; }

        [JsonIgnore]
        public List<CommodityMarket> CommodityMarkets { get; set; } = new List<CommodityMarket>();

        [JsonIgnore]
        public int TotalDemand { get; set; }

        [JsonIgnore]
        public int ValuableCommodityDemand { get; set; }
    }

    public class Station
    {
        public long Id { get; set; }
        public long MarketId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public double DistanceToArrival { get; set; }
        public string Allegiance { get; set; }
        public string Government { get; set; }
        public string Economy { get; set; }
        public bool HaveMarket { get; set; }
        public bool HaveShipyard { get; set; }
        public string ControllingFaction { get; set; }
    }

    public class MinorFactionPresences
    {
        public double Influence {  get; set; }
        public string Name { get; set; }
        public string State { get; set; }
    }

    public class CommodityMarket
    {
        public string Name { get; set; }
        public int BuyPrice { get; set; }
        public int SellPrice { get; set; }
        public int Demand { get; set; }
        public int Stock { get; set; }
        public int DemandBracket { get; set; }
        public int StockBracket { get; set; }
    }

    public class TrafficData
    {
        public int Total { get; set; }
        public int Week { get; set; }
        public int Day { get; set; }
    }

    public class Ring
    {
        public string Type { get; set; }
        public string Composition { get; set; }
        public bool IsPristine { get; set; }
        public double DistanceFromSystemEntry { get; set; }
    }

    public class Planet
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool HasRings { get; set; }
        public double DistanceFromArrivalLS { get; set; }
    }

}