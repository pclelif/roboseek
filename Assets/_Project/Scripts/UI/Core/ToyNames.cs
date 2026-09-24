using System;

namespace Robot.UI.Production
{
    public static class ToyNames
    {
        public static string Display(string value)
        {
            if (string.IsNullOrEmpty(value)) return "TARGET";
            string compact = value.Replace(" ", string.Empty).Replace("_", string.Empty).ToUpperInvariant();
            if (compact.StartsWith("BALL", StringComparison.Ordinal)) return UILocalization.IsTurkish ? "TOP" : "BALL";
            if (compact.StartsWith("TEDDYBEAR", StringComparison.Ordinal)) return UILocalization.IsTurkish ? "OYUNCAK AYI" : "TEDDY BEAR";
            if (compact.StartsWith("TOYCAR", StringComparison.Ordinal)) return UILocalization.IsTurkish ? "OYUNCAK ARABA" : "TOY CAR";
            return value.ToUpperInvariant();
        }
    }
}
