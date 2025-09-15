namespace ElitePiracyTracker.Models
{
    public class PiracyScoringConfig
    {
        public double EconomyScoreWeight { get; set; }
        public double NoRingsScoreWeight { get; set; }
        public double GovernmentScoreWeight { get; set; }
        public double FactionStateScoreWeight { get; set; }
        public double MarketDemandScoreWeight { get; set; }
        public double SecurityScoreWeight { get; set; }

        public Dictionary<string, double> EconomyMultipliers { get; set; }
        public Dictionary<string, double> GovernmentMultipliers { get; set; }
        public Dictionary<string, double> SecurityMultipliers { get; set; }
        public Dictionary<string, double> FactionStateMultipliers { get; set; }
        public Dictionary<string, double> DemandThresholds { get; set; }
        public Dictionary<string, double> ValuableCommodities { get; set; }
    }

    public class PiracyScoreResult
    {
        public string SystemName { get; set; }
        public double EconomyScore { get; set; }
        public double NoRingsScore { get; set; }
        public double GovernmentScore { get; set; }
        public double SecurityScore { get; set; }
        public double FactionStateScore { get; set; }
        public double MarketDemandScore { get; set; }
        public double FinalScore { get; set; }


        public override string ToString()
        {
            return $"{SystemName}: {FinalScore:F2}/100\n" +
                   $"  Economy: {EconomyScore:F2}\n" +
                   $"  No Rings/Belts: {NoRingsScore:F2}\n" +
                   $"  Government: {GovernmentScore:F2}\n" +
                   $"  Security: {SecurityScore:F2}\n" +
                   $"  Faction State: {FactionStateScore:F2}\n" +
                   $"  Market Demand: {MarketDemandScore:F2}";
        }
    }
}