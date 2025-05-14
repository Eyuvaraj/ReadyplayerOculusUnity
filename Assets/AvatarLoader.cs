using UnityEngine;
using UnityEngine.UI;
using ReadyPlayerMe.Core;

public class AvatarLoader : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private InputField avatarUrlInputField;
    [SerializeField] private Button loadButton;

    [Header("Avatar Settings")]
    [SerializeField] private RuntimeAnimatorController animatorController;
    [SerializeField] private string avatarConfigResourcePath = "Assets/Avatar Config.asset";

    private GameObject avatar;

    private void Start()
    {
        loadButton.onClick.AddListener(OnLoadAvatarClicked);
    }

    private void OnLoadAvatarClicked()
    {
        string avatarUrl = avatarUrlInputField.text;

        if (!string.IsNullOrEmpty(avatarUrl))
        {
            var avatarLoader = new AvatarObjectLoader();

            // Try to load configuration from Resources
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
                eyeHandler.BlinkInterval = 3f;   // Customize as desired
                eyeHandler.BlinkDuration = 0.1f; // Customize as desired

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
    }

    private void OnDestroy()
    {
        if (avatar != null)
        {
            Destroy(avatar);
        }
    }
}