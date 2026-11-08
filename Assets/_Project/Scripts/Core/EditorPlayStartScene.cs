namespace CutiePie.Core
{
    /// <summary>
    /// Yalnızca editörde: Play'e basılan sahnenin adını Boot'a taşır. SessionState domain reload'dan sonra da korunur.
    /// Build'de içi boştur; Consume her zaman null döner.
    /// </summary>
    public static class EditorPlayStartScene
    {
#if UNITY_EDITOR
        private const string SessionKey = "CutiePie.EditorPlayStartScene";
#endif

        /// <summary>Boot'tan sonra açılacak sahneyi kaydeder (editör tarafı çağırır). null/boş: normal akış.</summary>
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

        /// <summary>Kayıtlı sahne adını döner ve siler; yoksa null.</summary>
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
