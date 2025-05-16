using UnityEngine;
using UnityEngine.UI;
using ReadyPlayerMe.Core;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class AvatarLoader : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private InputField avatarUrlInputField;
    [SerializeField] private Button loadButton;

    [Header("Avatar Settings")]
    [SerializeField] private RuntimeAnimatorController animatorController;
    [SerializeField] private string avatarConfigResourcePath = "Avatar Config"; // Don't include "Assets/Resources/"

    private GameObject avatar;
    private string lastAvatarUrlKey = "LastAvatarURL";
    private AvatarObjectLoader avatarLoader;

    private void Start()
    {
        loadButton.onClick.AddListener(OnLoadAvatarClicked);

        // Optional: Load last avatar at start
        if (PlayerPrefs.HasKey(lastAvatarUrlKey))
        {
            avatarUrlInputField.text = PlayerPrefs.GetString(lastAvatarUrlKey);
            LoadAvatarFromUrl(avatarUrlInputField.text);
        }
    }

    private void OnLoadAvatarClicked()
    {
        string newUrl = avatarUrlInputField.text;

        if (!string.IsNullOrEmpty(newUrl))
        {
            string previousUrl = PlayerPrefs.GetString(lastAvatarUrlKey, string.Empty);

            // If URL changed, delete previous avatar files
            if (!string.IsNullOrEmpty(previousUrl) && previousUrl != newUrl)
            {
                DeleteCachedAvatar(previousUrl);
            }

            PlayerPrefs.SetString(lastAvatarUrlKey, newUrl);
            LoadAvatarFromUrl(newUrl);
        }
    }

    private void LoadAvatarFromUrl(string avatarUrl)
    {
        avatarLoader = new AvatarObjectLoader();

        AvatarConfig avatarConfig = Resources.Load<AvatarConfig>(avatarConfigResourcePath);
        if (avatarConfig != null)
        {
            avatarLoader.AvatarConfig = avatarConfig;
        }
        else
        {
            Debug.LogWarning("AvatarConfig not found at path: " + avatarConfigResourcePath);
        }

        avatarLoader.OnCompleted += (_, args) =>
        {
            if (avatar != null)
                Destroy(avatar);

            avatar = args.Avatar;

            Animator animator = avatar.GetComponent<Animator>() ?? avatar.AddComponent<Animator>();
            if (animatorController != null)
            {
                animator.runtimeAnimatorController = animatorController;
            }

            AvatarAnimationHelper.SetupAnimator(args.Metadata, avatar);

            EyeAnimationHandler eyeHandler = avatar.AddComponent<EyeAnimationHandler>();
            eyeHandler.BlinkInterval = 3f;
            eyeHandler.BlinkDuration = 0.1f;

            var lipSyncTarget = FindObjectOfType<OVRLipSyncContextMorphTarget>();
            if (lipSyncTarget != null)
            {
                var headMesh = avatar.GetMeshRenderer(MeshType.HeadMesh, true);
                if (headMesh != null)
                {
                    lipSyncTarget.skinnedMeshRenderer = headMesh;
                    lipSyncTarget.enabled = true;
                    Debug.Log("Lip Sync renderer assigned.");
                }
                else
                {
                    Debug.LogWarning("Head mesh not found. Lip Sync may not work.");
                }
            }
        };

        avatarLoader.OnFailed += (_, failureArgs) =>
        {
            Debug.LogError($"Avatar loading failed: {failureArgs.Message}");
        };

        avatarLoader.LoadAvatar(avatarUrl);
    }

    private async void DeleteCachedAvatar(string avatarUrl)
    {
        var avatarConfig = Resources.Load<AvatarConfig>(avatarConfigResourcePath);
        if (avatarConfig == null)
        {
            Debug.LogWarning("Cannot delete avatar cache: AvatarConfig not found.");
            return;
        }

        // Prepare context
        var context = new AvatarContext
        {
            Url = avatarUrl,
            AvatarConfig = avatarConfig,
            ParametersHash = AvatarCache.GetAvatarConfigurationHash(avatarConfig)
        };

        // Process the URL to extract GUID
        var urlProcessor = new UrlProcessor();
        context = await urlProcessor.Execute(context, CancellationToken.None);

        // Now that we have the GUID, delete cached avatar
        AvatarCache.DeleteAvatarModel(context.AvatarUri.Guid, context.ParametersHash);
        Debug.Log("Deleted previous cached avatar.");
    }


    private void OnDestroy()
    {
        if (avatar != null)
        {
            Destroy(avatar);
        }
    }
}
