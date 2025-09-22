using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDPA.Models.EDSM
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class EDSMSystemResponse
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("information")]
        public EDSMInformation Information { get; set; }

        [JsonProperty("traffic")]
        public EDSMTraffic Traffic { get; set; }

        [JsonProperty("bodies")]
        public List<EDSMBody> Bodies { get; set; }
    }

    public class EDSMInformation
    {
        [JsonProperty("security")]
        public string Security { get; set; }

        [JsonProperty("population")]
        public long Population { get; set; }

        [JsonProperty("economy")]
        public string Economy { get; set; }

        [JsonProperty("secondEconomy")]
        public string SecondEconomy { get; set; }

        [JsonProperty("government")]
        public string Government { get; set; }

        [JsonProperty("allegiance")]
        public string Allegiance { get; set; }

        [JsonProperty("controllingFaction")]
        public string ControllingFaction { get; set; }

        [JsonProperty("factionState")]
        public string FactionState { get; set; }
    }

    public class EDSMTraffic
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("week")]
        public int Week { get; set; }

        [JsonProperty("day")]
        public int Day { get; set; }
    }

    public class EDSMBody
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("subType")]
        public string SubType { get; set; }

        [JsonProperty("distanceToArrival")]
        public double DistanceToArrival { get; set; }

        [JsonProperty("isLandable")]
        public bool IsLandable { get; set; }

        [JsonProperty("gravity")]
        public double Gravity { get; set; }

        [JsonProperty("earthMasses")]
        public double EarthMasses { get; set; }

        [JsonProperty("radius")]
        public double Radius { get; set; }

        [JsonProperty("surfaceTemperature")]
        public int SurfaceTemperature { get; set; }

        [JsonProperty("volcanismType")]
        public string VolcanismType { get; set; }

        [JsonProperty("atmosphereType")]
        public string AtmosphereType { get; set; }

        [JsonProperty("rings")]
        public List<EDSMRing> Rings { get; set; }
    }

    public class EDSMRing
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("mass")]
        public long Mass { get; set; }

        [JsonProperty("innerRadius")]
        public long InnerRadius { get; set; }

        [JsonProperty("outerRadius")]
        public long OuterRadius { get; set; }
    }

    public class EDSMSstationResponse
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("stations")]
        public List<EDSMSstation> Stations { get; set; }
    }

    public class EDSMSstation
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("marketId")]
        public long MarketId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("distanceToArrival")]
        public double DistanceToArrival { get; set; }

        [JsonProperty("allegiance")]
        public string Allegiance { get; set; }

        [JsonProperty("government")]
        public string Government { get; set; }

        [JsonProperty("economy")]
        public string Economy { get; set; }

        [JsonProperty("haveMarket")]
        public bool HaveMarket { get; set; }

        [JsonProperty("haveShipyard")]
        public bool HaveShipyard { get; set; }

        [JsonProperty("controllingFaction")]
        public EDSMControllingFaction ControllingFaction { get; set; }
    }

    public class EDSMControllingFaction
    {
        [JsonProperty("id")]
        public long? Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class EDSMMarketData
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("commodities")]
        public List<EDSMCommodity> Commodities { get; set; }
    }

    public class EDSMCommodity
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("buyPrice")]
        public int BuyPrice { get; set; }

        [JsonProperty("sellPrice")]
        public int SellPrice { get; set; }

        [JsonProperty("demand")]
        public int Demand { get; set; }

        [JsonProperty("stock")]
        public int Stock { get; set; }

        [JsonProperty("demandBracket")]
        public int DemandBracket { get; set; }

        [JsonProperty("stockBracket")]
        public int StockBracket { get; set; }
    }

}
