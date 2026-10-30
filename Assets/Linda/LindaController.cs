using UnityEngine;

// Linda in the lab: A/D walk (flips to face the way she walks), J whip, H take a hit. Idle otherwise.
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class LindaController : MonoBehaviour
{
    public float speed = 1.5f;        // units per second
    public float knockSpeed = 4f;     // thrown forward this fast while hit_back starts
    public float knockTime = 0.35f;   // seconds of the throw

    Animator anim;
    SpriteRenderer sr;
    float knockLeft;

    public int Facing => sr.flipX ? -1 : 1;
    public bool Free => State.IsName("idle") || State.IsName("walk");
    // the whip is out around the middle of the strike
    public bool WhipOut => State.IsName("hit_whip") && State.normalizedTime > 0.25f && State.normalizedTime < 0.6f;
    AnimatorStateInfo State => anim.GetCurrentAnimatorStateInfo(0);

    // Hit in the back: she turns away from the blow and is thrown forward (dir = +1 right, -1 left).
    public void Knock(float dir)
    {
        sr.flipX = dir < 0;
        knockLeft = knockTime;
        anim.SetTrigger("hit");
    }

    void Start()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    void OnGUI()   // lab readout
    {
        GUI.Label(new Rect(8, 8, 600, 24), string.Format("x {0:F2}  {1}  flipX {2}  knock {3:F2}   A/D walk  J whip  H hit",
            transform.position.x, StateName, sr.flipX, knockLeft));
    }

    string StateName => Free ? (State.IsName("idle") ? "idle" : "walk") : State.IsName("hit_whip") ? "hit_whip" : State.IsName("hit_back") ? "hit_back" : "?";

    void Update()
    {
        if (knockLeft > 0)
        {
            knockLeft -= Time.deltaTime;
            transform.position += Vector3.right * Facing * knockSpeed * Time.deltaTime;
            return;
        }
        var free = Free;
        if (free && Input.GetKeyDown(KeyCode.J)) anim.SetTrigger("whip");
        if (free && Input.GetKeyDown(KeyCode.H)) Knock(Facing);
        var dx = free ? Input.GetAxisRaw("Horizontal") : 0;
        anim.SetBool("walk", dx != 0);
        if (dx != 0)
        {
            sr.flipX = dx < 0;
            transform.position += Vector3.right * dx * speed * Time.deltaTime;
        }
    }
}
