//using System.Collections;
//using UnityEngine;

//public class AvatarAnimator : MonoBehaviour
//{
//    private Animator anim;

//    void Start()
//    {
//        anim = GetComponent<Animator>();
//        Debug.Log("Animator found: " + (anim != null));
//        StartCoroutine(RandomizeAnimation());
//    }

//    public IEnumerator RandomizeAnimation()
//    {
//        while (true)
//        {
//            yield return new WaitForSeconds(3);
//            int rand = Random.Range(0, 6);
//            Debug.Log("Changing to animation: " + rand);
//            anim.SetInteger("AnimationInteger", rand);
//            anim.SetTrigger("AnimationTrigger");
//        }
//    }
//}
