using UnityEngine;

namespace Solo.MOST_IN_ONE
{
    // Dummy Script to control Enemies on Shoot Scenes
    public class SimpleEnemyControl : MonoBehaviour
    {
        public float Speed;
        public bool FollowCharacter;
        public GameObject DestroyPt;

        Transform _charFollow;
        void Start()
        {
            if (FollowCharacter)
            {
                _charFollow = GameObject.Find("Character_Shoot").transform;
                GetComponent<Animation>().Play("Running");
            }
        }
        void Update()
        {
            if (FollowCharacter)
            {
                transform.LookAt(_charFollow);
                transform.position += Speed * Time.deltaTime * transform.TransformDirection(Vector3.forward);
            }
        }
        public void DestroyObject()
        {
            Destroy(Instantiate(DestroyPt, transform.position + Vector3.up, Quaternion.identity), 5);
            Destroy(gameObject);
        }
    }
}
