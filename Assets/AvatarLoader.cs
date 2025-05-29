using UnityEngine;
using UnityEngine.UI;
using ReadyPlayerMe.Core;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking; // For UnityWebRequest
using NativeGalleryNamespace; // For NativeGallery
using System.Collections;

public class AvatarLoader : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button pickImageButton;

    [Header("Avatar Settings")]
    [SerializeField] private RuntimeAnimatorController animatorController;
    [SerializeField] private AvatarConfig avatarConfig;

    private GameObject avatar;
    private AvatarObjectLoader avatarLoader;

    private void Start()
    {
        pickImageButton.onClick.AddListener(OnPickImageClicked);
    }

    private void OnPickImageClicked()
    {
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (!string.IsNullOrEmpty(path))
            {
                Debug.Log("Image path: " + path);
                StartCoroutine(UploadAndLoadAvatar(path));
            }
            else
            {
                Debug.LogWarning("No image selected or permission denied.");
            }
        }, "Pick an image to generate your avatar", "image/*");
    }

    IEnumerator UploadAndLoadAvatar(string imagePath)
    {
        // UPLOAD to endpoint
        WWWForm form = new WWWForm();
        byte[] imageBytes = File.ReadAllBytes(imagePath);
        string filename = Path.GetFileName(imagePath);

        // Optional: gender could be taken from user input/UI, but hard-coded for demo:
        form.AddField("gender", "female");
        form.AddBinaryData("image", imageBytes, filename, "image/jpeg");

        string url = "https://eyuvaraj-avatarcreator.hf.space/generate-avatar/";

        using (UnityWebRequest uwr = UnityWebRequest.Post(url, form))
        {
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("accept", "application/json");

            yield return uwr.SendWebRequest();

#if UNITY_2023_1_OR_NEWER
            if (uwr.result != UnityWebRequest.Result.Success)
#else
            if (uwr.isNetworkError || uwr.isHttpError)
#endif
            {
                Debug.LogError("Error uploading image: " + uwr.error + " / " + uwr.downloadHandler.text);
                yield break;
            }

            // Parse response JSON
            string jsonResponse = uwr.downloadHandler.text;
            Debug.Log("Server response: " + jsonResponse);

            // Response is {"avatar_url": "..."}
            string avatarUrl = "";
            try
            {
                AvatarApiResponse response = JsonUtility.FromJson<AvatarApiResponse>(jsonResponse);
                avatarUrl = response.avatar_url;
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to parse avatar URL: " + ex);
                yield break;
            }

            if (!string.IsNullOrEmpty(avatarUrl))
            {
                LoadAvatarFromUrl(avatarUrl);
            }
            else
            {
                Debug.LogError("Received empty avatar URL from API.");
            }
        }
    }

    [Serializable]
    public class AvatarApiResponse
    {
        public string avatar_url;
    }

    private void LoadAvatarFromUrl(string avatarUrl)
    {
        avatarLoader = new AvatarObjectLoader();

        if (avatarConfig != null)
        {
            avatarLoader.AvatarConfig = avatarConfig;
        }
        else
        {
            Debug.LogWarning("AvatarConfig not assigned in inspector.");
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

            RandomAnimator randomAnimator = avatar.AddComponent<RandomAnimator>();
            randomAnimator.anim = animator;
            randomAnimator.interval = 3f;

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

    private void OnDestroy()
    {
        if (avatar != null)
        {
            Destroy(avatar);
        }
    }
}