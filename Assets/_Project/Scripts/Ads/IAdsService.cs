using System;

namespace CutiePie.Ads
{
    /// <summary>
    /// Reklam servisi arayüzü. Geliştirmede FakeAdsService, yayında AdMobAdsService.
    /// Erişim noktası: GameManager.Instance.Ads (somut sınıfa doğrudan bağlanmayın).
    /// </summary>
    public interface IAdsService
    {
        /// <summary>Ödüllü reklam gösterilebilir mi.</summary>
        bool IsRewardedReady { get; }

        /// <summary>Ödüllü reklamı gösterir. onCompleted(true): izlendi, ödül verilmeli.</summary>
        void ShowRewarded(Action<bool> onCompleted);
    }
}
