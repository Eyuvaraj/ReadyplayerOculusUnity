using System.Collections;
using UnityEngine;

public class RandomAnimator : MonoBehaviour
{
    [Header("Assign Animator via Inspector or leave blank to auto-find")]
    public Animator anim;

    [Header("Animation Timing (seconds)")]
    public float interval = 3f;

    void Awake()
    {
        // Fallback if not assigned in inspector
        if (anim == null)
        {
            anim = GetComponent<Animator>();
        }

        if (anim == null)
        {
            Debug.LogError("Animator not assigned and not found on the GameObject.");
        }
    }

    IEnumerator Start()
    {
        if (anim == null) yield break;

        while (true)
        {
            yield return new WaitForSeconds(interval);

            int index = Random.Range(0, 4);
            Debug.Log("Auto: Setting IdleIndex to " + index);

            anim.SetInteger("IdleIndex", index);
            anim.SetTrigger("Trigger");
        }
    }

    void Update()
    {
        // Optional manual trigger to test from keyboard
        if (Input.GetKeyDown(KeyCode.Space) && anim != null)
        {
            int index = Random.Range(0, 5);
            Debug.Log("Manual: Setting IdleIndex to " + index);

            anim.SetInteger("IdleIndex", index);
            anim.SetTrigger("Trigger");
        }
    }
}
