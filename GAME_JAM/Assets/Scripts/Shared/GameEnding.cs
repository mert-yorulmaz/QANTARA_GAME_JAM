namespace ZeroHour.Shared
{
    /// <summary>
    /// Gece vardiyası (Faz 2) bitince ofis sahnesi kapanış modunda açılır: oyuncu kanepeye yatar, jenerik gelir.
    /// Faz 2 bayrağı kaldırıp ofisi yükler; ofisteki NightEnding bayrağı okuyup indirir.
    /// </summary>
    public static class GameEnding
    {
        public static bool Pending;
    }
}
