namespace CutiePie.Core
{
    /// <summary>
    /// Yalnızca editörde: Play'e basılan sahneyi Boot'a taşır. SessionState domain reload'dan sonra da korunur.
    /// Değer sahne adıdır; Build Settings dışı sahneler (BootInEditorMarker) için dosya yoludur (Assets/... .unity).
    /// Build'de içi boştur; Consume her zaman null döner.
    /// </summary>
    public static class EditorPlayStartScene
    {
#if UNITY_EDITOR
        private const string SessionKey = "CutiePie.EditorPlayStartScene";
#endif

        /// <summary>Boot'tan sonra açılacak sahneyi (ad ya da yol) kaydeder (editör tarafı çağırır). null/boş: normal akış.</summary>
        public static void Set(string sceneName)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(sceneName))
            {
                UnityEditor.SessionState.EraseString(SessionKey);
            }
            else
            {
                UnityEditor.SessionState.SetString(SessionKey, sceneName);
            }
#endif
        }

        /// <summary>Değer bir sahne dosya yolu mu (Build Settings dışı sahne; adla yüklenemez).</summary>
        public static bool IsScenePath(string value) =>
            !string.IsNullOrEmpty(value) && value.StartsWith("Assets/") && value.EndsWith(".unity");

        /// <summary>Kayıtlı sahne adını ya da yolunu döner ve siler; yoksa null.</summary>
        public static string Consume()
        {
#if UNITY_EDITOR
            string sceneName = UnityEditor.SessionState.GetString(SessionKey, string.Empty);
            UnityEditor.SessionState.EraseString(SessionKey);
            return string.IsNullOrEmpty(sceneName) ? null : sceneName;
#else
            return null;
#endif
        }
    }
}
