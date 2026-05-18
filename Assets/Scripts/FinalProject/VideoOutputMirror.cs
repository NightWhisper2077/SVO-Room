using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace FinalProject
{
    public sealed class VideoOutputMirror : MonoBehaviour
    {
        [SerializeField] private VideoPlayer sourceVideoPlayer;
        [SerializeField] private Renderer targetScreenRenderer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstall()
        {
            if (SceneManager.GetActiveScene().name != "Start Scene")
                return;

            if (FindFirstObjectByType<VideoOutputMirror>() != null)
                return;

            var videoPlayer = FindFirstObjectByType<VideoPlayer>();
            var tvScreen = FindTvScreenRenderer();

            if (videoPlayer == null || tvScreen == null)
                return;

            var mirror = tvScreen.gameObject.AddComponent<VideoOutputMirror>();
            mirror.sourceVideoPlayer = videoPlayer;
            mirror.targetScreenRenderer = tvScreen;
            mirror.Apply();
        }

        private void Start()
        {
            Apply();
        }

        public void Apply()
        {
            if (sourceVideoPlayer == null)
                sourceVideoPlayer = FindFirstObjectByType<VideoPlayer>();

            if (targetScreenRenderer == null)
                targetScreenRenderer = FindTvScreenRenderer();

            if (sourceVideoPlayer == null || targetScreenRenderer == null || sourceVideoPlayer.targetTexture == null)
                return;

            var material = CreateVideoMaterial(sourceVideoPlayer.targetTexture);
            targetScreenRenderer.material = material;
        }

        private static Renderer FindTvScreenRenderer()
        {
            var tv = GameObject.Find("TV_FlatWallMounted");
            if (tv == null)
                return null;

            var renderers = tv.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].gameObject.name == "Screen")
                    return renderers[i];
            }

            return renderers.Length > 0 ? renderers[0] : null;
        }

        private static Material CreateVideoMaterial(Texture texture)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = "TV Video Mirror Material",
                mainTexture = texture,
                color = Color.white,
            };

            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);

            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);

            if (material.HasProperty("_EmissionMap"))
                material.SetTexture("_EmissionMap", texture);

            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", Color.white);

            return material;
        }
    }
}
