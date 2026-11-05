using System;
using System.Collections;
using UnityEngine;

namespace CutiePie.Ads
{
    /// <summary>
    /// Geliştirme için sahte reklam: belirtilen süre bekler ve her zaman başarılı döner.
    /// </summary>
    public class FakeAdsService : IAdsService
    {
        private readonly MonoBehaviour _coroutineRunner;
        private readonly float _durationSeconds;
        private bool _isShowing;

        /// <param name="coroutineRunner">Bekleme coroutine'ini çalıştıracak kalıcı obje.</param>
        /// <param name="durationSeconds">Sahte reklamın süresi (gerçek zamanlı saniye).</param>
        public FakeAdsService(MonoBehaviour coroutineRunner, float durationSeconds)
        {
            _coroutineRunner = coroutineRunner;
            _durationSeconds = durationSeconds;
        }

        public bool IsRewardedReady => !_isShowing;

        public void ShowRewarded(Action<bool> onCompleted)
        {
            if (_isShowing)
            {
                onCompleted?.Invoke(false);
                return;
            }

            _coroutineRunner.StartCoroutine(ShowRoutine(onCompleted));
        }

        private IEnumerator ShowRoutine(Action<bool> onCompleted)
        {
            _isShowing = true;
            Debug.Log("[FakeAds] Ödüllü reklam başladı.");
            // Oyun duraklatılmış olsa da çalışsın diye gerçek zaman kullanılır.
            yield return new WaitForSecondsRealtime(_durationSeconds);
            _isShowing = false;
            Debug.Log("[FakeAds] Ödüllü reklam bitti.");
            onCompleted?.Invoke(true);
        }
    }
}
