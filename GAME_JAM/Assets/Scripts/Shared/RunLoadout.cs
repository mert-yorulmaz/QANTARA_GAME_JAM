using UnityEngine;

namespace ZeroHour.Shared
{
    /// <summary>
    /// Bu turun gece vardiyası ekibi: Faz 1 sonunda bilgisayardaki satın alma ekranında oluşturulur,
    /// Faz 2 (AStarMovementTester) araç sayılarını buradan alır. Ayarlanmadıysa Faz 2 kendi varsayılan filosuyla başlar
    /// (ör. Faz 2 doğrudan test edilirken). Ana menüden yeni tur başlayınca temizlenir.
    /// </summary>
    public static class RunLoadout
    {
        public static bool IsSet { get; private set; }
        public static int Police { get; private set; }
        public static int Ambulance { get; private set; }
        public static int FireTruck { get; private set; }
        public static int Helicopter { get; private set; }

        public static void Set(int police, int ambulance, int fireTruck, int helicopter)
        {
            Police = Mathf.Max(1, police);
            Ambulance = Mathf.Max(1, ambulance);
            FireTruck = Mathf.Max(1, fireTruck);
            Helicopter = Mathf.Max(1, helicopter);
            IsSet = true;
            Debug.Log($"[RunLoadout] Gece ekibi: {Police} polis, {Ambulance} ambulans, {FireTruck} itfaiye, {Helicopter} helikopter");
        }

        public static void Clear()
        {
            IsSet = false;
        }
    }
}
