using UnityEngine;

namespace ZeroHour.Phase2.Calls
{
    /// <summary>
    /// Afet efektlerinin materyalleri. Sahnedeki bir objede referanslı oldukları için build'e dahil olurlar
    /// (Shader.Find ile çalışma anında aranan shader'lar build'de eksik kalabilir).
    /// </summary>
    public class DisasterFxLibrary : MonoBehaviour
    {
        public static DisasterFxLibrary Instance { get; private set; }

        [Tooltip("Alev, kıvılcım, patlama, şok dalgası (toplamalı / additive)")]
        public Material additive;
        [Tooltip("Duman, toz, su sıçraması (alfa karışımlı)")]
        public Material alphaBlended;
        [Tooltip("Moloz parçaları (dokusuz kare parçacık)")]
        public Material debris;
        [Tooltip("Sel suyu (yarı saydam)")]
        public Material water;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
